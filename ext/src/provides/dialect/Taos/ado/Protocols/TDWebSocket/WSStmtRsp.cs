namespace mooSQL.data.taos.Protocols.TDWebSocket
{
    /// <summary>
    /// WSStmtRsp type.
    /// </summary>
    /// <summary>
    /// WSStmtRsp type.
    /// </summary>
    public class WSStmtRsp : WSActionRsp
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
        /// <summary>
        /// stmt_id
        /// </summary>
        /// <summary>
        /// Gets or sets stmt_id.
        /// </summary>
        public int stmt_id { get; set; }
    }
    /// <summary>
    /// WSStmtRsp type.
    /// </summary>
    /// <summary>
    /// WSStmtRsp type.
    /// </summary>
    public class WSStmtExecRsp : WSStmtRsp
    {
        /// <summary>
        /// affected
        /// </summary>
        /// <summary>
        /// Gets or sets affected.
        /// </summary>
        public int affected { get; set; }
    }
}