using System;
using NLog;

namespace wServer.networking.redis.processors
{
    public abstract class IProcess
    {
        protected readonly Logger log = NLog.LogManager.GetCurrentClassLogger();
        protected readonly string command;
        protected bool debug;
        
        protected IProcess(string command)
        {
            this.command = command;
            this.debug = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DEBUG")) && bool.Parse(Environment.GetEnvironmentVariable("DEBUG"));
        }

        protected static void RespondRequest(object content)
        {
            RedisManager.GetInstance().RespondRequest(content);
        }

        public bool CheckCommand(Request<object> request)
        {
            if (command.EqualsIgnoreCase(request.command))
            {
                Process(request);
                return true;
            }

            return false;
        }
        
        protected abstract void Process(Request<object> request);
    }
}