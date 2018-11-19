using db.data;
using Newtonsoft.Json.Linq;
using wServer.realm;

namespace wServer.networking.redis.processors
{
    public class ProcessCreatePlayer : IProcess
    {
        private RealmManager Realm;
        
        public ProcessCreatePlayer(RealmManager realm) : base(Command.CREATE_PLAYER.nome)
        {
            this.Realm = realm;
        }

        protected override void Process(Request<object> request)
        {
            var jarr = (JArray) request.args;

            var email = jarr[0]["email"].ToString();
            var password = jarr[0]["password"].ToString();
            var objectId = jarr[0]["object_id"].ToString();
                            
            Realm.Database.DoActionAsync(db =>
            {
                var cmd = db.Register(email, password, objectId, false, new XmlData());
                                
                if (cmd != null)
                    RespondRequest(Response<string>.Ok(request.id));
                else
                    RespondRequest(Response<string>.BadRequest(request.id, "Email already in use."));
            });
        }
    }
}