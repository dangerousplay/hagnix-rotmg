#region

using System;
using System.Collections.Generic;
using System.IO;
//using log4net;

#endregion

namespace db
{
    public class SimpleSettings : ISimpleSettings, IDisposable
    {
//        private static readonly ILog log = LogManager.GetLogger(typeof (SimpleSettings));
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        
        private readonly string cfgFile;
        private readonly string id;
        private readonly string path;
        private readonly Dictionary<string, string> values;

        public SimpleSettings(string id)
        {
            log.Info($"Loading settings for '{id}'...");

            values = new Dictionary<string, string>();
            this.id = id;
            this.path = Environment.CurrentDirectory;
            cfgFile = Path.Combine(Environment.CurrentDirectory, id + ".cfg");
            if (File.Exists(cfgFile))
                using (StreamReader rdr = new StreamReader(File.OpenRead(cfgFile)))
                {
                    string line;
                    int lineNum = 1;
                    while ((line = rdr.ReadLine()) != null)
                    {
                        if (line.StartsWith("#")) continue;
                        int i = line.IndexOf(":");
                        if (i == -1)
                        {
                            log.Info($"Invalid settings at line {lineNum}.");
                            throw new ArgumentException("Invalid settings.");
                        }
                        string val = line.Substring(i + 1);

                        values.Add(line.Substring(0, i),
                            val.Equals("null", StringComparison.InvariantCultureIgnoreCase) ? null : val);
                        lineNum++;
                    }
                    log.Info("Settings loaded.");
                }
            else
                log.Info("Settings not found.");
            
            InitWatcher();
        }

        private void InitWatcher()
        {
            var watcher = new FileSystemWatcher();
            watcher.Path = this.path;
            /* Watch for changes in LastAccess and LastWrite times, and
               the renaming of files or directories. */
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                                            | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            // Only watch text files.
            watcher.Filter = "*.cfg";

            // Add event handlers.
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);

            // Begin watching.
            watcher.EnableRaisingEvents = true;
        }

        private void OnChanged(object o, FileSystemEventArgs e)
        {
            Reload();
        }

        public void Reload()
        {
            log.Info($"Reloading settings for '{id}'...");
            values.Clear();
            if (File.Exists(cfgFile))
                using (StreamReader rdr = new StreamReader(File.OpenRead(cfgFile)))
                {
                    string line;
                    int lineNum = 1;
                    while ((line = rdr.ReadLine()) != null)
                    {
                        if (line.StartsWith("#")) continue;
                        int i = line.IndexOf(":");
                        if (i == -1)
                        {
                            log.Info($"Invalid settings at line {lineNum}.");
                            throw new ArgumentException("Invalid settings.");
                        }
                        string val = line.Substring(i + 1);

                        values.Add(line.Substring(0, i),
                            val.Equals("null", StringComparison.InvariantCultureIgnoreCase) ? null : val);
                        lineNum++;
                    }
                    log.Info("Settings loaded.");
                }
            else
                log.Info("Settings not found.");
        }

        public static Dictionary<string,string> LoadFrom(IEnumerable<string> strings)
        {
            var values = new Dictionary<string,string>();

            foreach (var s in strings)
            {
                if (s.StartsWith("#")) continue;
                var i = s.IndexOf(":");
                if (i == -1)
                {
                    log.Info($"Invalid settings at line {s}.");
                    throw new ArgumentException("Invalid settings.");
                }
                var val = s.Substring(i + 1);

                values.Add(s.Substring(0, i),
                    val.Equals("null", StringComparison.InvariantCultureIgnoreCase) ? null : val);
            }
            
         
            return values;
        }

        public void Dispose()
        {
            try
            {
                log.Info("Saving settings for '{0}'...", id);
                using (StreamWriter writer = new StreamWriter(File.OpenWrite(cfgFile)))
                    foreach (KeyValuePair<string, string> i in values)
                        writer.WriteLine("{0}:{1}", i.Key, i.Value == null ? "null" : i.Value);
            }
            catch (Exception e)
            {
                log.Error("Error when saving settings.", e);
            }
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
    }
}