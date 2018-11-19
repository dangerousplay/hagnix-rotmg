namespace wServer.networking.redis
{
    public class Command
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
        public static readonly Command SERVER_INFO = new Command("SERVER_INFO");

        public Command(string nome)
        {
            this.nome = nome;
        }

        public string nome { get; }
    }
}