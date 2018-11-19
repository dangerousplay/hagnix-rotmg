using Newtonsoft.Json.Linq;
using wServer.realm;

namespace wServer.networking.redis.processors
{
    public class ProcessBan : IProcess
    {
        private RealmManager Realm;
        public ProcessBan(RealmManager realm) : base(Command.BAN.nome)
        {
            this.Realm = realm;
        }

        protected override void Process(Request<object> request)
        {
            var array = (JArray) request.args;

            var email = array[0]["email"].ToString();

            var player = Realm.FindPlayerByEmail(email);

            if (player == null)
            {
                RespondRequest(Response<string>.NotFound(request.id));
                return;
            }
                                
            player?.Client.Disconnect();

            Realm.Database.DoActionAsync(db =>
            {
                var cmd = db.CreateQuery();
                cmd.CommandText = "UPDATE accounts SET banned=1 WHERE uuid=@accId;";
                cmd.Parameters.AddWithValue("@accId", email);
                var rtn = cmd.ExecuteNonQuery();

                RespondRequest(rtn > 0
                    ? Response<string>.Ok(request.id)
                    : Response<string>.NotFound(request.id));
            });
        }
    }
}