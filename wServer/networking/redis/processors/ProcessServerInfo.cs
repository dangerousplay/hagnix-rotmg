using wServer.realm;
using wServer.networking.redis.models;

namespace wServer.networking.redis.processors
{
    public class ProcessServerInfo : IProcess
    {
        private RealmManager Realm;
        public ProcessServerInfo( RealmManager realm) : base(Command.SERVER_INFO.nome)
        {
            this.Realm = realm;
        }

        protected override void Process(Request<object> request)
        {
            RespondRequest(Response<models.Server>.Ok(request.id, new models.Server("Server", Realm.Clients.Count, Realm.MaxClients)));
        }
    }
}