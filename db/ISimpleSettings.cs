using System;

namespace db
{
    public interface ISimpleSettings
    {
        string GetValue(string key, string def = null);
        T GetValue<T>(string key, string def = null);
        void SetValue(string key, string val);
        void Reload();
    }
}