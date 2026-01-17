using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public static class MethodCallExtensions
    {

        public static ExpressionCall Expression(this MethodCall method,Expression expression) {
            var t = new ExpressionCall();
            t.Value = expression;
            return t;
        }
    }
}
