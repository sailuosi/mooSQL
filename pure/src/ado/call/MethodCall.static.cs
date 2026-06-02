using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    /// <summary>
    /// 类型 MethodCallExtensions。
    /// </summary>
    public static class MethodCallExtensions
    {

        /// <summary>
        /// Expression 方法（返回 ExpressionCall）。
        /// </summary>
        public static ExpressionCall Expression(this MethodCall method,Expression expression) {
            var t = new ExpressionCall();
            t.Value = expression;
            return t;
        }
    }
}