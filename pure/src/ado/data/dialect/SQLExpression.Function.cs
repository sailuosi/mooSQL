// 基础功能说明：SQL方言之 函数部分

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public abstract partial class SQLExpression
    {
        /// <summary>
        /// 拼接字符串
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public virtual string stringConcat(string left, string right)
        {

            return string.Format("concat({0}, {1})", left, right);
        }
        /// <summary>
        /// 三个字符串的拼接
        /// </summary>
        /// <param name="left"></param>
        /// <param name="mid"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public string stringConcat(string left, string mid, string right)
        {
            return stringConcat(stringConcat(left, mid), right);
        }
    }
}