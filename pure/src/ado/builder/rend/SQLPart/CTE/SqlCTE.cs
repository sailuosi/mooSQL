using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 类型 SqlCTE。
    /// </summary>
    public class SqlCTE
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public SqlCTE() { 
            this.cteList = new List<SqlCTEItem> ();
        }

        /// <summary>
        /// 字段 cteList（List<SqlCTEItem>）。
        /// </summary>
        public List<SqlCTEItem> cteList;


        /// <summary>
        /// 属性 Empty（bool）。
        /// </summary>
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


        /// <summary>
        /// Clear 方法。
        /// </summary>
        public void Clear() { 
            cteList.Clear();
        }

    }

    /// <summary>
    /// 类型 SqlCTEItem。
    /// </summary>
    public class SqlCTEItem {

        /// <summary>
        /// 字段 builder（SQLBuilder）。
        /// </summary>
        public SQLBuilder builder;

        /// <summary>
        /// 字段 type（SqlCTEType）。
        /// </summary>
        public SqlCTEType type;

        /// <summary>
        /// 字段 asName（string）。
        /// </summary>
        public string asName;

        /// <summary>
        /// 字段 solidSQL（string）。
        /// </summary>
        public string solidSQL;

        /// <summary>
        /// getSQL 方法（返回 string）。
        /// </summary>
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

    /// <summary>
    /// 枚举 SqlCTEType。
    /// </summary>
    public enum SqlCTEType { 
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        Select=0, 
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        Update=1, 
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        Delete=2,
        /// <summary>
        /// 内部成员说明。
        /// </summary>
        SolidSQL=9
    }
}