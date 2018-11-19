using System.Collections;
using Newtonsoft.Json;

namespace wServer.networking.redis
{
    public class Response<T>
    {
        public Response()
        {
        }

        public Response(uint status, string id, T content)
        {
            this.status = status;
            this.id = id;
            this.content = content;
        }

        [JsonProperty]
        private uint status { get; }
        [JsonProperty]
        private T content { get; }
        [JsonProperty]
        private string id;

        public static Response<string> BadRequest(string id)
        {
            return new Response<string>(400, id, "Bad request");
        }

        public static Response<object> BadRequest(string id, IEnumerable response)
        {
            return new Response<object>(400, id, response);
        }

        public static Response<string> NotFound(string id)
        {
            return new Response<string>(404, id, "Not Found");
        }

        public static Response<string> Id(string id, uint httpid)
        {
            return new Response<string>(httpid, id, "");
        }

        public static Response<T> Ok(string id, T body)
        {
            return new Response<T>(200, id, body);
        }

        public static Response<string> Ok(string id)
        {
            return new Response<string>(200, id, "Ok");
        }
    }
}