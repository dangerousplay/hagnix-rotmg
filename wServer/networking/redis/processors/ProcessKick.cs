using Newtonsoft.Json.Linq;
using wServer.realm;

namespace wServer.networking.redis.processors
{
    public class ProcessKick : IProcess
    {
        private RealmManager Realm;
        
        public ProcessKick(RealmManager realm) : base(Command.KICK.nome)
        {
            this.Realm = realm;
        }

        protected override void Process(Request<object> request)
        {
            var array = (JArray) request.args;

            var email = array[0]["email"].ToString();
            var reason = array[0]["reason"].ToString();

            var player = Realm.FindPlayerByEmail(email);

            player.SendInfo("Player Disconnected: " + reason);
            player.Client.Disconnect();

            RespondRequest(Realm.FindPlayerByEmail(email) != null
                ? Response<string>.Ok(request.id)
                : Response<string>.NotFound(request.id));
        }
    }
}