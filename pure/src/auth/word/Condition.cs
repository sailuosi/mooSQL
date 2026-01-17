using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.auth
{
    /*
     * 
{"Operation":"or","Filters":[{"Key":"{loginRole}","Value":"09ee2ffa-7463-4938-ae0b-1cb4e80c7c13,77e6d0c3-f9e1-4933-92c3-c1c6eef75593","Contrast":"contains","names":"","Text":"管理员,神"}],"Children":[{"Operation":"and","Filters":[{"Key":"{loginRole}","Value":"0a7ebd0c-78d6-4fbc-8fbe-6fc25c3a932d","Contrast":"contains","Text":"测试"},{"Key":"CreateUserId","Value":"{loginUser}","Contrast":"==","Text":""}]}]}
     */

    /// <summary>
    /// 条件
    /// </summary>
    public class Condition
    {
        public string Key { get; set; }
        public string Value { get; set; }

        public Object parsedValue { get; set; }
        /// <summary>
        /// 操作符，如 = like > < 等
        /// </summary>
        public string Contrast { get; set; }
        /// <summary>
        /// 操作的值
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// 是否参数化
        /// </summary>
        public bool Paramed=true;
        /// <summary>
        /// 获取值
        /// </summary>
        /// <returns></returns>
        public object getValue() {
            if (parsedValue != null) { 
                return parsedValue;
            }
            if(!string.IsNullOrWhiteSpace(Value))
            {
                return Value;
            }
            return Text;
        }
        /// <summary>
        /// 获取列表性质的值
        /// </summary>
        /// <returns></returns>
        public IEnumerable getListValue()
        {
            var listV = parsedValue as IEnumerable;
            if (listV != null)
            {
                return listV;
            }
            if (!string.IsNullOrWhiteSpace(Value))
            {
                var t = Value.Split(';').ToList();
                return t;
            }
            return Text.Split(';').ToList();
        }
    }

}
