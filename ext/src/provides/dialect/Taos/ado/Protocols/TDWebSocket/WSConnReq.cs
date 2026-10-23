namespace mooSQL.data.taos.Protocols.TDWebSocket
{
    /// <summary>
    /// WSConnReq type.
    /// </summary>

    public class WSConnReq
    {
        /// <summary>
        /// req_id
        /// </summary>
        /// <summary>
        /// Gets or sets req_id.
        /// </summary>
        public int req_id { get; set; }
        /// <summary>
        /// user
        /// </summary>
        /// <summary>
        /// Gets or sets user.
        /// </summary>
        public string user { get; set; }
        /// <summary>
        /// password
        /// </summary>
        /// <summary>
        /// Gets or sets password.
        /// </summary>
        public string password { get; set; }
        /// <summary>
        /// db
        /// </summary>
        /// <summary>
        /// Gets or sets db.
        /// </summary>
        public string db { get; set; }
    }


}