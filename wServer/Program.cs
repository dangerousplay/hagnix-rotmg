#region

using System;
using System.Globalization;
using System.IO;
using System.Threading;
using db;
//using log4net;
//using log4net.Config;
using wServer.networking;
using wServer.realm;
using System.Net.Mail;
using System.Net;
using NLog;
using NLog.Config;
using NLog.Targets;
using wServer.networking.redis;

#endregion

namespace wServer
{
    internal static class Program
    {
        public static bool WhiteList { get; private set; }
        public static bool Verify { get; private set; }
        internal static SimpleSettings Settings;
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
//        private static readonly ILog log = LogManager.GetLogger("Server");
        private static RealmManager manager;

        public static DateTime WhiteListTurnOff { get; private set; }

        private static void Main(string[] args)
        {
            Console.Title = "Fabiano Swagger of Doom - World Server";
            try
            {
//                XmlConfigurator.ConfigureAndWatch(new FileInfo("log4net_wServer.config"));
                setLog();

                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.Name = "Entry";

                Settings = new SimpleSettings("wServer");

                var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
                var dbDatabase = Environment.GetEnvironmentVariable("DB_DATABASE");
                var dbUser = Environment.GetEnvironmentVariable("DB_USER");
                var dbAuth = Environment.GetEnvironmentVariable("DB_PASSWORD");

                new Database(
                    dbHost ?? Settings.GetValue<string>("db_host", "127.0.0.1"),
                    dbDatabase ?? Settings.GetValue<string>("db_database", "rotmgprod"),
                    dbUser ?? Settings.GetValue<string>("db_user", "root"),
                    dbAuth ?? Settings.GetValue<string>("db_auth", ""));

                manager = new RealmManager(
                    Settings.GetValue<int>("maxClients", "100"),
                    Settings.GetValue<int>("tps", "20"));

                WhiteList = Settings.GetValue<bool>("whiteList", "false");
                Verify = Settings.GetValue<bool>("verifyEmail", "false");
                WhiteListTurnOff = Settings.GetValue<DateTime>("whitelistTurnOff");

                manager.Initialize();
                manager.Run();

                RedisManager.GetInstance().Realm = manager;
                RedisManager.GetInstance().PopulateCommands();

                Server server = new Server(manager);
                PolicyServer policy = new PolicyServer();

                Console.CancelKeyPress += (sender, e) => e.Cancel = true;

                policy.Start();
                server.Start();
                if(Settings.GetValue<bool>("broadcastNews", "false") && File.Exists("news.txt"))
                    new Thread(autoBroadcastNews).Start();
                log.Info("Server initialized.");

                uint key = 0;
                while ((key = (uint)Console.ReadKey(true).Key) != (uint)ConsoleKey.Escape)
                {
                    if (key == (2 | 80))
                        Settings.Reload();
                }

                log.Info("Terminating...");
                server.Stop();
                policy.Stop();
                manager.Stop();
                log.Info("Server terminated.");
            }
            catch (Exception e)
            {
                log.Fatal(e);

                if(manager?.Clients != null)
                foreach (var c in manager.Clients)
                {
                    c.Value.Disconnect();
                }
                Console.ReadLine();
            }
        }

        public static void SendEmail(MailMessage message, bool enableSsl = true)
        {
            SmtpClient client = new SmtpClient
            {
                Host = Settings.GetValue<string>("smtpHost", "smtp.gmail.com"),
                Port = Settings.GetValue<int>("smtpPort", "587"),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                EnableSsl = true,
                Credentials =
                    new NetworkCredential(Settings.GetValue<string>("serverEmail"),
                        Settings.GetValue<string>("serverEmailPassword"))
            };

            client.Send(message);
        }

        private static void setLog()
        {
            var config = new LoggingConfiguration();
            var consoleTarget = new ColoredConsoleTarget("target1")
            {
                Layout = @"${longdate} ${level} ${message} ${exception}"
            };
            config.AddTarget(consoleTarget);


            config.AddRuleForAllLevels(consoleTarget); 

            LogManager.Configuration = config;
        }

        private static void autoBroadcastNews()
        {
                var news = File.ReadAllLines("news.txt");
                do
                {
                    ChatManager cm = new ChatManager(manager);
                    cm.News(news[new Random().Next(news.Length)]);
                    Thread.Sleep(300000); //5 min
                }
                while (true);
            }

        
    }
}