using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Web.Mvc;
using System.Web.WebPages;
using db;
using db.data;
//using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Fluent;
using StackExchange.Redis;
using wServer.realm;

namespace wServer.networking.redis
{
    internal class RedisManager
    {
        private static readonly RedisManager instance = new RedisManager();

        private readonly Logger log = NLog.LogManager.GetCurrentClassLogger();
        private const string REQUEST_CHANNEL = "requests";
        private const string RESPONSE_CHANNEL = "response";
        private readonly ConnectionMultiplexer redis;
        
        private RedisManager()
        {
            try
            {
                var password = Environment.GetEnvironmentVariable("REDIS_PASSWORD");
                var host = Environment.GetEnvironmentVariable("REDIS_HOST");
                var port = Environment.GetEnvironmentVariable("REDIS_PORT") != null
                    ? Environment.GetEnvironmentVariable("REDIS_PORT").AsInt()
                    : 0;
                
                var config = new ConfigurationOptions()
                {
                    EndPoints =
                    {
                        {
                            host ?? "localhost", port != 0 ? port : 6379
                        }
                    },
                    Password = password ?? "",
                    UseSsl = true,
                    Ssl = true,
                    AbortOnConnectFail = false
                };
                
                log.Info($"Redis connection: {JsonConvert.SerializeObject(config)}");

                while (true)
                    try
                    {
                        redis = ConnectionMultiplexer.Connect(config);
                        
                        break;
                    }
                    catch (Exception e)
                    {
                        Thread.Sleep(1000);
                        log.Warn($"Can't connect to Redis: {e}");
                    }
            }
            catch (Exception e)
            {   
                log.Error(e, "Can't load configuration Redis");
            }
        }

        public RealmManager Realm { private get; set; }

        public ConnectionMultiplexer GetRedis()
        {
            return redis;
        }

        public static RedisManager GetInstance()
        {
            return instance;
        }

        public void Start()
        {
            log.Info("Connecting in to Redis");

            SetEvents();
        }

        private void RespondRequest(object content)
        {
            redis.GetSubscriber().Publish(RESPONSE_CHANNEL, JsonConvert.SerializeObject(content));
        }

        private void SetEvents()
        {
            redis.GetSubscriber().Subscribe(REQUEST_CHANNEL, (channel, message) =>
            {
                try
                {
                    var request = JsonConvert.DeserializeObject<Request<object>>(message);

                    try
                    {
                        if (request.command.EqualsIgnoreCase(Command.AUTHORIZE.nome))
                        {
                            var array = (JArray) request.args;
                            var email = array[0]["email"].ToString();
                            var time = Convert.ToInt64(array[0]["expiration"].ToString());

                            RespondRequest(
                                Realm.AuthorizedList.TryAdd(email, DateTimeOffset.Now.ToUnixTimeMilliseconds() + time)
                                    ? Response<string>.Ok(request.id)
                                    : Response<string>.Id(request.id, 500));
                        }
                        else if (request.command.EqualsIgnoreCase(Command.BAN.nome))
                        {
                            var array = (JArray) request.args;

                            var email = array[0]["email"].ToString();

                            var player = Realm.FindPlayerByEmail(email);

                            if (player == null)
                            {
                                RespondRequest(Response<string>.NotFound(request.id));
                                return;
                            }
                                
                            player?.Client.Disconnect();

                            Realm.Database.DoActionAsync(db =>
                            {
                                var cmd = db.CreateQuery();
                                cmd.CommandText = "UPDATE accounts SET banned=1 WHERE uuid=@accId;";
                                cmd.Parameters.AddWithValue("@accId", email);
                                var rtn = cmd.ExecuteNonQuery();

                                RespondRequest(rtn > 0
                                    ? Response<string>.Ok(request.id)
                                    : Response<string>.NotFound(request.id));
                            });
                        }
                        else if (request.command.EqualsIgnoreCase(Command.GETPLAYER.nome))
                        {
                            var array = (JArray) request.args;

                            var email = array[0]["email"].ToString();

                            var player = Realm.FindPlayerByEmail(email).Client.Account;

                            if (player != null)
                                RespondRequest(Response<Player>.Ok(request.id,
                                    new Player(player.Name, player.Admin, player.FortuneTokens, player.Credits, player.Email, null)));
                            else
                                RespondRequest(Response<string>.NotFound(request.id));
                        }
                        else if (request.command.EqualsIgnoreCase(Command.KICK.nome))
                        {
                            var array = (JArray) request.args;

                            var email = array[0]["email"].ToString();
                            var reason = array[0]["reason"].ToString();

                            var player = Realm.FindPlayerByEmail(email);

                            player.SendInfo("Player Disconnected: " + reason);
                            player.Client.Disconnect();

                            RespondRequest(Realm.FindPlayerByEmail(email) != null
                                ? Response<string>.Ok(request.id)
                                : Response<string>.NotFound(request.id));
                        }
                        else if (request.command.EqualsIgnoreCase(Command.LOGGED.nome))
                        {
                            var jarr = (JArray) request.args;

                            var email = jarr[0]["email"].ToString();

                            RespondRequest(Realm.FindPlayerByEmail(email) != null
                                ? Response<string>.Ok(request.id)
                                : Response<string>.NotFound(request.id));
                        }
                        else if (request.command.EqualsIgnoreCase(Command.PARDON.nome))
                        {
                            var array = (JArray) request.args;

                            var email = array[0]["email"].ToString();

                            Realm.Database.DoActionAsync(db =>
                            {
                                var cmd = db.CreateQuery();
                                cmd.CommandText = "UPDATE accounts SET banned=0 WHERE uuid=@accId;";
                                cmd.Parameters.AddWithValue("@accId", email);
                                var rtn = cmd.ExecuteNonQuery();

                                RespondRequest(rtn > 0
                                    ? Response<string>.Ok(request.id)
                                    : Response<string>.NotFound(request.id));
                            });
                        }
                        else if (request.command.EqualsIgnoreCase(Command.LIST.nome))
                        {
                            var clientes = Realm.Clients.Select(R => R.Value.Account).Select(R =>
                                new Player(R.Name, R.Admin, R.FortuneTokens, R.Credits, R.Email, null));

                            RespondRequest(Response<IEnumerable<Player>>.Ok(request.id, clientes));
                        } 
                        else if (request.command.EqualsIgnoreCase(Command.CREATE_PLAYER.nome))
                        {
                            var jarr = (JArray) request.args;

                            var email = jarr[0]["email"].ToString();
                            var password = jarr[0]["password"].ToString();
                            var objectId = jarr[0]["object_id"].ToString();
                            
                            Realm.Database.DoActionAsync(db =>
                            {
                                var cmd = db.Register(email, password, objectId, false, new XmlData());
                                
                                if (cmd != null)
                                    RespondRequest(Response<string>.Ok(request.id));
                                else
                                    RespondRequest(Response<string>.BadRequest(request.id, "Email already in use."));
                            });
                         
                        } else if (request.command.EqualsIgnoreCase(Command.DELETE_PLAYER.nome))
                        {
                            var jarr = (JArray) request.args;
                            var id = jarr[0]["id"].ToString();
                            
                            Realm.Database.DoActionAsync(db =>
                            {
                                RespondRequest(db.DeletePlayer(id) ? Response<string>.Ok(request.id) : Response<string>.BadRequest(request.id));
                            });
                            
                        } else if (request.command.EqualsIgnoreCase(Command.CHANGE_PLAYER.nome))
                        {
                            var playerch = JsonConvert.DeserializeObject<Player>(((JArray) request.args)[0].ToString());

                            Realm.Database.DoActionAsync(e =>
                            {
                                var account = e.GetAccountByUUID(playerch.email, new XmlData());
                                if (account == null)
                                {
                                    RespondRequest(Response<string>.NotFound(request.id));
                                    return;
                                }

                                account.Rank = playerch.admin ? 3 : account.Rank;
                                account.Name = !string.IsNullOrEmpty(playerch.name) ? playerch.name : account.Name;
                                account.FortuneTokens = playerch.token > 0 ? playerch.token : account.FortuneTokens;
                                account.Credits = playerch.gold > 0 ? playerch.gold : account.Credits;
                                account.Password = !string.IsNullOrEmpty(playerch.password)
                                    ? playerch.password
                                    : null;
                                
                                e.SaveAccount(account);

                                RespondRequest(Response<Player>.Ok(request.id, playerch));
                            });
                        }
                        else
                        {
                            RespondRequest(Response<string>.BadRequest(request.id));
                        }
                    }
                    catch (Exception e)
                    {
                        RespondRequest(Response<string>.Id(request.id, 500));
                        log.Error(e);
                    }
                }
                catch (Exception e)
                {
                    log.Error(e);
                }
            });
        }


        private class Command
        {
            public static readonly Command KICK = new Command("KICK");
            public static readonly Command BAN = new Command("BAN");
            public static readonly Command AUTHORIZE = new Command("AUTHORIZE");
            public static readonly Command GETPLAYER = new Command("GETPLAYER");
            public static readonly Command LOGGED = new Command("LOGGED");
            public static readonly Command PARDON = new Command("PARDON");
            public static readonly Command LIST = new Command("LIST");
            public static readonly Command CREATE_PLAYER = new Command("CREATE_PLAYER");
            public static readonly Command DELETE_PLAYER = new Command("DELETE_PLAYER");
            public static readonly Command CHANGE_PLAYER = new Command("CHANGE_PLAYER");

            public Command(string nome)
            {
                this.nome = nome;
            }

            public string nome { get; }
        }

        private class Response<T>
        {
            public static Response<string> BAD_REQUEST = new Response<string>(400, "RND-ID", "Bad request");
            public static Response<string> OK = new Response<string>(200, "RND-ID", "OK");
            private string id;

            public Response()
            {
            }

            public Response(uint status, string id, T content)
            {
                this.status = status;
                this.id = id;
                this.content = content;
            }

            private uint status { get; }
            private T content { get; }

            public static Response<string> BadRequest(string id)
            {
                return new Response<string>(400, id, "Bad request");
            }

            public static Response<object> BadRequest(string id, IEnumerable response)
            {
                return new Response<object>(400, id, response);
            }

            public static Response<string> NotFound(string id)
            {
                return new Response<string>(404, id, "Not Found");
            }

            public static Response<string> Id(string id, uint httpid)
            {
                return new Response<string>(httpid, id, "");
            }

            public static Response<T> Ok(string id, T body)
            {
                return new Response<T>(200, id, body);
            }

            public static Response<string> Ok(string id)
            {
                return new Response<string>(200, id, "Ok");
            }
        }

        private class Player
        {
            public Player(string name, bool admin, int token, int gold, string email, string password)
            {
                this.name = name;
                this.admin = admin;
                this.token = token;
                this.gold = gold;
                this.email = email;
                this.password = password;
            }

            public Player()
            {
            }

            public string name;
            public bool admin;
            public int token;
            public int gold;
            public string email;
            public string password;
        }

        private class Request<T>
        {
            public readonly T args;
            public readonly string command;
            public string id;

            public Request()
            {
            }

            public Request(string command, T args)
            {
                this.command = command;
                this.args = args;
            }

            public override string ToString()
            {
                return JsonConvert.SerializeObject(this);
            }
        }
    }
}