namespace mooSQL.data.taos.Protocols.TDWebSocket
{
    /// <summary>
    /// TaosWSResult type.
    /// </summary>

    public class TaosWSResult
    {
        /// <summary>
        /// data
        /// </summary>
        /// <summary>
        /// Gets or sets data.
        /// </summary>
        public byte[] data { get; set; }
        /// <summary>
        /// meta
        /// </summary>
        /// <summary>
        /// Gets or sets meta.
        /// </summary>
        public WSQueryRsp meta { get; set; }
        /// <summary>
        /// rows
        /// </summary>
        /// <summary>
        /// Gets or sets rows.
        /// </summary>
        public int rows { get;  set; }
        /// <summary>
        /// StmtExec
        /// </summary>
        /// <summary>
        /// Gets or sets StmtExec.
        /// </summary>
        public WSStmtExecRsp StmtExec { get;  set; }
    }


}