namespace mooSQL.data.taos.Protocols.TDWebSocket
{
    /// <summary>
    /// WSSchemalessRsp type.
    /// </summary>
    /// <summary>
    /// WSSchemalessRsp type.
    /// </summary>
    public class WSSchemalessRsp: WSActionRsp
    {
        /// <summary>
        /// req_id
        /// </summary>
        /// <summary>
        /// Gets or sets req_id.
        /// </summary>
        public int req_id { get; set; }
        /// <summary>
        /// timing
        /// </summary>
        /// <summary>
        /// Gets or sets timing.
        /// </summary>
        public int timing { get; set; }

    }
}