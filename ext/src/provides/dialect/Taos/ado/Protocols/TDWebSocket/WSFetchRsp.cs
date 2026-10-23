using System.Collections.Generic;

namespace mooSQL.data.taos.Protocols.TDWebSocket
{
    /// <summary>
    /// WSFetchRsp type.
    /// </summary>
    /// <summary>
    /// WSFetchRsp type.
    /// </summary>
    public class WSFetchRsp : WSActionRsp
    {
        /// <summary>
        /// req_id
        /// </summary>
        /// <summary>
        /// Gets or sets req_id.
        /// </summary>
        public int req_id { get; set; }
        /// <summary>
        /// Gets or sets timing.
        /// </summary>

        public int timing { get; set; }
        /// <summary>
        /// Gets or sets id.
        /// </summary>

        public int id { get; set; }
        /// <summary>
        /// Gets or sets completed.
        /// </summary>

        public bool completed { get; set; }
        /// <summary>
        /// Gets or sets lengths.
        /// </summary>

        public List<int> lengths { get; set; }
        /// <summary>
        /// Gets or sets rows.
        /// </summary>

        public int rows { get; set; }
    }


}