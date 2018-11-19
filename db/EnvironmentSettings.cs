using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using NLog.Fluent;

namespace db
{
    public class EnvironmentSettings : ISimpleSettings
    {
        private string variable { get; set; }
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        private Dictionary<String, String> values;

        public EnvironmentSettings(string variable)
        {
            this.variable = Environment.GetEnvironmentVariable(variable);

            var linesEncoded = Encoding.UTF8.GetString(Convert.FromBase64String(variable));
            
            var lines = linesEncoded.Split('\n');

            values = SimpleSettings.LoadFrom(lines);
         
            log.Info($"Settings loaded from environment variable: {variable}.");
        }

        public string GetValue(string key, string def = null)
        {
            string ret;
            if (!values.TryGetValue(key, out ret))
            {
                if (def == null)
                {
                    log.Error($"Attempt to access nonexistant settings '{key}'.");
                    throw new ArgumentException(string.Format("'{0}' does not exist in settings.", key));
                }
                ret = values[key] = def;
            }
            return ret;
        }

        public T GetValue<T>(string key, string def = null)
        {
            string ret;
            if (!values.TryGetValue(key, out ret))
            {
                if (def == null)
                {
                    log.Error($"Attempt to access nonexistant settings '{key}'.");
                    throw new ArgumentException(string.Format("'{0}' does not exist in settings.", key));
                }
                ret = values[key] = def;
            }
            return (T) Convert.ChangeType(ret, typeof (T));
        }

        public void SetValue(string key, string val)
        {
            values[key] = val;
        }

        public void Reload()
        {
            
        }
    }
}