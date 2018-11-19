using Newtonsoft.Json.Linq;
using wServer.realm;

namespace wServer.networking.redis.processors
{
    public class ProcessDeletePlayer : IProcess
    {
        private RealmManager Realm;
        
        public ProcessDeletePlayer(RealmManager realm) : base(Command.DELETE_PLAYER.nome)
        {
            this.Realm = realm;
        }

        protected override void Process(Request<object> request)
        {
            var jarr = (JArray) request.args;
            var id = jarr[0]["id"].ToString();
                            
            Realm.Database.DoActionAsync(db =>
            {
                RespondRequest(db.DeletePlayer(id) ? Response<string>.Ok(request.id) : Response<string>.BadRequest(request.id));
            });
        }
    }
}