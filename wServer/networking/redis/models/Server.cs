using Newtonsoft.Json;

namespace wServer.networking.redis.models
{
    public class Server
    {
        [JsonProperty]
        private string name;
        [JsonProperty]
        private int players;
        [JsonProperty]
        private int capacity;

        public Server(string name, int players, int capacity)
        {
            this.name = name;
            this.players = players;
            this.capacity = capacity;
        }
    }
}