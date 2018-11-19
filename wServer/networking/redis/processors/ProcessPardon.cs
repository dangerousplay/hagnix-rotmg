using Newtonsoft.Json.Linq;
using wServer.realm;

namespace wServer.networking.redis.processors
{
    public class ProcessPardon : IProcess
    {
        private RealmManager Realm;
        
        public ProcessPardon( RealmManager realm) : base(Command.PARDON.nome)
        {
            this.Realm = realm;
        }

        protected override void Process(Request<object> request)
        {
            var array = (JArray) request.args;

            var email = array[0]["email"].ToString();

            Realm.Database.DoActionAsync(db =>
            {
                var cmd = db.CreateQuery();
                cmd.CommandText = "UPDATE accounts SET banned=0 WHERE uuid=@accId;";
                cmd.Parameters.AddWithValue("@accId", email);
                var rtn = cmd.ExecuteNonQuery();

                RespondRequest(rtn > 0
                    ? Response<string>.Ok(request.id)
                    : Response<string>.NotFound(request.id));
            });
        }
    }
}