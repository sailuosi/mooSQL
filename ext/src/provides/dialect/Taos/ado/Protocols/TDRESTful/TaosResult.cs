using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace mooSQL.data.taos.Protocols.TDRESTful
{
    /// <summary>
    /// TaosResult type.
    /// </summary>
    /// <summary>
    /// TaosResult type.
    /// </summary>
    public class TaosResult
    {
        /// <summary>
        /// code
        /// </summary>
        /// <summary>
        /// Gets or sets code.
        /// </summary>
        public int code { get; set; }
        /// <summary>
        /// desc
        /// </summary>
        /// <summary>
        /// Gets or sets desc.
        /// </summary>
        public string desc { get; set; }
        /// <summary>
        /// Gets or sets column_meta.
        /// </summary>

        public List<List<string>> column_meta { get; set; }
        /// <summary>
        /// Scalar
        /// </summary>

        public object Scalar => (data?.First?.First as JValue)?.Value;
        /// <summary>
        /// data
        /// </summary>
        /// <summary>
        /// Gets or sets data.
        /// </summary>
        public JArray data { get; set; }
        /// <summary>
        /// Gets or sets rows.
        /// </summary>

        public int rows { get; set; }
    }
}