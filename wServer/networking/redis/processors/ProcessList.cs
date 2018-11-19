using System.Collections.Generic;
using System.Linq;
using wServer.networking.redis.models;
using wServer.realm;

namespace wServer.networking.redis.processors
{
    public class ProcessList : IProcess
    {
        private RealmManager Realm;
        
        public ProcessList( RealmManager realm) : base(Command.LIST.nome)
        {
            this.Realm = realm;
        }

        protected override void Process(Request<object> request)
        {
            var clientes = Realm.Clients.Select(R => R.Value.Account).Select(R =>
                new Player(R.Name, R.Admin, R.FortuneTokens, R.Credits, R.Email, null));

            RespondRequest(Response<IEnumerable<Player>>.Ok(request.id, clientes));
        }
    }
}