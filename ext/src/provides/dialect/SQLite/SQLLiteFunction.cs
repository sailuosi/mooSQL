using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <inheritdoc/>
    public class SQLLiteFunction:SooSQLFunction
    {
        /// <inheritdoc/>
        public override string Len(string FieldSQL)
        {
            return "LENGTH(" + FieldSQL + ")";
        }
        /// <inheritdoc/>
        public override string SubStr(string FieldSQL, int start, int length)
        {
            return string.Concat("SUBSTR(", FieldSQL, ",", start.ToString(), ",", length.ToString(), ")");
        }
        /// <inheritdoc/>
        public override string CharIndex(string subString, string str)
        {
            return string.Concat("INSTR(", str, ",", subString, ")");
        }
        /// <inheritdoc/>
        public override string Now() {
            return "DATETIME('now')";
        }
        /// <inheritdoc/>
        public override string Year(string FieldSQL)
        {
            return string.Concat("STRFTIME('%Y',", FieldSQL, ")");
        }
        /// <inheritdoc/>
        public override string Month(string FieldSQL)
        {
            return string.Concat("STRFTIME('%m',", FieldSQL, ")");
        }
        /// <inheritdoc/>
        public override string Day(string FieldSQL)
        {
            return string.Concat("STRFTIME('%d',", FieldSQL, ")");
        }
    }
}
