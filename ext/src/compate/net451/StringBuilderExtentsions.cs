#if NETFRAMEWORK



using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    public static class StringBuilderExtentsions
    {

        public static StringBuilder AppendLine(this StringBuilder builder, CultureInfo InvariantCulture, string val)
        {
            return builder.AppendLine(val);
        }

        public static StringBuilder Append(this StringBuilder builder, CultureInfo InvariantCulture, string val)
        {
            return builder.AppendLine(val);
        }
    }

}


    #endif
