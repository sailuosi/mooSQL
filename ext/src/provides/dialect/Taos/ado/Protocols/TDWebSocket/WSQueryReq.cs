namespace mooSQL.data.taos.Protocols.TDWebSocket
{
    /// <summary>
    /// WSQueryReq type.
    /// </summary>

    public class WSQueryReq
    {
        /// <summary>
        /// req_id
        /// </summary>
        /// <summary>
        /// Gets or sets req_id.
        /// </summary>
        public long req_id { get; set; }
        /// <summary>
        /// sql
        /// </summary>
        /// <summary>
        /// Gets or sets sql.
        /// </summary>
        public string sql { get; set; }
    }


}