using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    internal class OriginColumn
    {
        public OriginColumn() { }

        public string DbColumnName;

        public string NickName;
        /// <summary>
        /// 读取时的列类型
        /// </summary>
        public Type ValueType;
    }
}
