using Newtonsoft.Json.Linq;
using wServer.realm;

namespace wServer.networking.redis.processors
{
    public class ProcessLogged : IProcess
    {
        private RealmManager Realm;
        
        public ProcessLogged( RealmManager realm) : base(Command.LOGGED.nome)
        {
            this.Realm = realm;
        }

        protected override void Process(Request<object> request)
        {
            var jarr = (JArray) request.args;

            var email = jarr[0]["email"].ToString();

            RespondRequest(Realm.FindPlayerByEmail(email) != null
                ? Response<string>.Ok(request.id)
                : Response<string>.NotFound(request.id));
        }
    }
}