using Newtonsoft.Json;

namespace wServer.networking.redis.models
{
    public class Player
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
            
        [JsonProperty]
        public string name;
        [JsonProperty]
        public bool admin;
        [JsonProperty]
        public int token;
        [JsonProperty]
        public int gold;
        [JsonProperty]
        public string email;
        [JsonProperty]
        public string password;
    }
}