using System.Collections.Generic;

namespace mooSQL.data.taos.Protocols.TDWebSocket
{
    /// <summary>
    /// WSQueryRsp type.
    /// </summary>
    /// <summary>
    /// WSQueryRsp type.
    /// </summary>
    public class WSQueryRsp : WSActionRsp
    {
        /// <summary>
        /// Gets or sets req_id.
        /// </summary>

        public int req_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public long timing { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool is_update { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int affected_rows { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int fields_count { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<string> fields_names { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<int> fields_types { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<int> fields_lengths { get; set; }
        /// <summary>
        /// precision
        /// </summary>
        /// <summary>
        /// Gets or sets precision.
        /// </summary>
        public int precision { get; set; }
    }


}