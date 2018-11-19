using Newtonsoft.Json;

namespace wServer.networking.redis
{
    public class Request<T>
    {
        [JsonProperty]
        public T args;
        [JsonProperty]
        public string command;
        [JsonProperty]
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