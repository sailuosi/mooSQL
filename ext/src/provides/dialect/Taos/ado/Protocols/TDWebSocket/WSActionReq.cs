using Newtonsoft.Json;

namespace mooSQL.data.taos.Protocols.TDWebSocket
{
    /// <summary>
    /// WSActionReq type.
    /// </summary>
 
    public class WSActionReq<T>
    {
        /// <summary>
        /// Gets or sets Action.
        /// </summary>
        [JsonProperty("action")]
        public string Action { get; set; }
        /// <summary>
        /// Gets or sets Args.
        /// </summary>

        [JsonProperty("args")]
        public T Args { get; set; }
    }

}