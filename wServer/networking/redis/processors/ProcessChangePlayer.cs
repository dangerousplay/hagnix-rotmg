using db.data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using wServer.networking.redis.models;
using wServer.realm;

namespace wServer.networking.redis.processors
{
    public class ProcessChangePlayer : IProcess
    {
        private RealmManager Realm;
        
        public ProcessChangePlayer(RealmManager realm) : base(Command.CHANGE_PLAYER.nome)
        {
            this.Realm = realm;
        }

        protected override void Process(Request<object> request)
        {
            var playerch = JsonConvert.DeserializeObject<Player>(((JArray) request.args)[0].ToString());

            Realm.Database.DoActionAsync(e =>
            {
                var account = e.GetAccountByUUID(playerch.email, new XmlData());
                if (account == null)
                {
                    RespondRequest(Response<string>.NotFound(request.id));
                    return;
                }

                account.Rank = playerch.admin ? 3 : account.Rank;
                account.Name = !string.IsNullOrEmpty(playerch.name) ? playerch.name : account.Name;
                account.FortuneTokens = playerch.token > 0 ? playerch.token : account.FortuneTokens;
                account.Credits = playerch.gold > 0 ? playerch.gold : account.Credits;
                account.Password = !string.IsNullOrEmpty(playerch.password)
                    ? playerch.password
                    : null;
                                
                e.SaveAccount(account);

                RespondRequest(Response<Player>.Ok(request.id, playerch));
            });
        }
    }
}