using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public class SqlCTE
    {
        public SqlCTE() { 
            this.cteList = new List<SqlCTEItem> ();
        }

        public List<SqlCTEItem> cteList;


        public bool Empty
        {
            get {

                if (cteList == null|| cteList.Count==0) {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 添加一个表达式
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public SqlCTE add(SqlCTEItem item) { 
            cteList.Add(item); return this;
        }


        public void Clear() { 
            cteList.Clear();
        }

    }

    public class SqlCTEItem {

        public SQLBuilder builder;

        public SqlCTEType type;

        public string asName;

        public string solidSQL;

        public string getSQL() {
            if (type == SqlCTEType.Select)
            {
                var cmd = builder.toSelect();
                if (cmd != null && !string.IsNullOrWhiteSpace(cmd.sql))
                {
                    return cmd.sql;
                }
            }
            else if (type == SqlCTEType.SolidSQL)
            {
                var cmd = solidSQL;
                if (!string.IsNullOrWhiteSpace(cmd))
                {
                    return cmd;
                }
            }
            return null;
        }
    }

    public enum SqlCTEType { 
        Select=0, 
        Update=1, 
        Delete=2,
        SolidSQL=9
    }
}
