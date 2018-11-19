using System;
using Newtonsoft.Json.Linq;
using wServer.realm;

namespace wServer.networking.redis.processors
{
    public class ProcessAuthorize : IProcess
    {
        private RealmManager Realm;
        public ProcessAuthorize(RealmManager realm) : base(Command.AUTHORIZE.nome)
        {
            this.Realm = realm;
        }

        protected override void Process(Request<object> request)
        {
            var array = (JArray) request.args;
            var email = array[0]["email"].ToString();
            var time = Convert.ToInt64(array[0]["expiration"].ToString());

            while (!Realm.AuthorizedList.TryAdd(email,
                DateTimeOffset.Now.ToUnixTimeMilliseconds() + time)) {}
                            
            if(debug)
                log.Debug($"Authorized user {email} for {time}ms.");
                            
            RespondRequest(
                Response<string>.Ok(request.id)
            );
        }
    }
}