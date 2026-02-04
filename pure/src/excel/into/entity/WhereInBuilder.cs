// 基础功能说明：

using mooSQL.data;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.excel.context
{
    public class WhereInBuilder:WhereListBag
    {
        /// <summary>
        /// 来源的列集合的名字key
        /// </summary>
        public HashSet<string> srcFields;

        /// <summary>
        /// 添加一个来源列
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public bool addSrcField(string fieldName) { 
            if(string.IsNullOrEmpty(fieldName)) return false;
            return srcFields.Add(fieldName);
        }

        /// <summary>
        /// 输出普通的无参数化SQL，不安全部分使用正则过滤处理
        /// </summary>
        /// <returns></returns>
        public string toPlainSQL() {

            var vals= new List<string>();
            if (this.unSafeValues.Count > 0) { 
                foreach (var val in this.unSafeValues) { 
                    var t= RegxUntils.SqlFilter(val,false);
                    vals.Add(t); 
                }
            }

            var resInPart = this.toWhereIn(vals);
            if (string.IsNullOrWhiteSpace(op)) {
                op = "IN";
            }
            return string.Format(" {0} {1} ( {2}) ", field,op, resInPart);
        }

    }
}
