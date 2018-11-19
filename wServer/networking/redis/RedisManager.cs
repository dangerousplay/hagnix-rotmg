using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web.WebPages;
using db.data;
//using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using ServiceStack.Redis;
using ServiceStack.Text;
using wServer.networking.redis.processors;
using wServer.realm;

namespace wServer.networking.redis
{
    internal class RedisManager
    {
        private static readonly RedisManager instance = new RedisManager();

        private readonly Logger log = NLog.LogManager.GetCurrentClassLogger();
        private const string REQUEST_CHANNEL = "requests";
        private const string RESPONSE_CHANNEL = "response";
        private readonly RedisManagerPool redis;
        private bool debug = false;

        private static List<IProcess> Commands = new List<IProcess>();
        
        private RedisManager()
        {
            try
            {
                var password = Environment.GetEnvironmentVariable("REDIS_PASSWORD");
                var host = Environment.GetEnvironmentVariable("REDIS_HOST");
                var port = Environment.GetEnvironmentVariable("REDIS_PORT") != null
                    ? Environment.GetEnvironmentVariable("REDIS_PORT").AsInt()
                    : 0;

                var debugVer = Environment.GetEnvironmentVariable("DEBUG");

                this.debug = !string.IsNullOrEmpty(debugVer) && bool.Parse(debugVer);

                var nPort = port != 0 ? port : 6379;

                var config = new RedisEndpoint()
                {
                    Host = host ?? "localhost",
                    Password = password ?? "",
                    Port = nPort
                }.ToString();
                
                log.Info($"Redis connection: {host ?? "localhost"}:{nPort}");

                while (true)
                    try
                    {
                        redis = new RedisManagerPool(config);
                        Start();
                        
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
                log.Error(e);
                log.Error(e, "Can't load configuration Redis");
            }
        }

        public RealmManager Realm { private get; set; }

        public RedisManagerPool GetRedis()
        {
            return redis;
        }

        public static RedisManager GetInstance()
        {
            return instance;
        }

        public void PopulateCommands()
        {
            Commands.Add(new ProcessAuthorize(Realm));
            Commands.Add(new ProcessBan(Realm));
            Commands.Add(new ProcessChangePlayer(Realm));
            Commands.Add(new ProcessCreatePlayer(Realm));
            Commands.Add(new ProcessDeletePlayer(Realm));
            Commands.Add(new ProcessGetPlayer(Realm));
            Commands.Add(new ProcessKick(Realm));
            Commands.Add(new ProcessList(Realm));
            Commands.Add(new ProcessPardon(Realm));
            Commands.Add(new ProcessLogged(Realm));
            Commands.Add(new ProcessServerInfo(Realm));
        }

        private void Start()
        {
            log.Info("Connecting in to Redis");

            SetEvents();
        }

        public void RespondRequest(object content)
        {
            redis.GetClient().PublishMessage(RESPONSE_CHANNEL, JsonConvert.SerializeObject(content));
        }

        private void SetEvents()
        {
            var sub = (RedisPubSubServer)redis.CreatePubSubServer(REQUEST_CHANNEL, (channel, message) =>
            {                    
                if(debug)
                    log.Debug($"Received request: {message}");
                
                try
                {
                    var request = JsonConvert.DeserializeObject<Request<object>>(message);
                    
                    try
                    {
                        
                        if (!Commands.Any(p => p.CheckCommand(request)))
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

            sub.OnStart = () =>
            {
                log.Info($"Redis starting Listening on {REQUEST_CHANNEL}");

                if (debug)
                    log.Info("Debugging redis traffic.");
            };
            
            sub.Start();
        }
 
    }
}