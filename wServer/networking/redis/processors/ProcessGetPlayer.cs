using Newtonsoft.Json.Linq;
using wServer.networking.redis.models;
using wServer.realm;

namespace wServer.networking.redis.processors
{
    public class ProcessGetPlayer : IProcess
    {
        private RealmManager Realm;
        public ProcessGetPlayer( RealmManager realm) : base(Command.GETPLAYER.nome)
        {
            this.Realm = realm;
        }

        protected override void Process(Request<object> request)
        {
            var array = (JArray) request.args;

            var email = array[0]["email"].ToString();

            var player = Realm.FindPlayerByEmail(email).Client.Account;

            if (player != null)
                RespondRequest(Response<Player>.Ok(request.id,
                    new Player(player.Name, player.Admin, player.FortuneTokens, player.Credits, player.Email, null)));
            else
                RespondRequest(Response<string>.NotFound(request.id));
        }
    }
}