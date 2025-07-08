
using mooSQL.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    /// <summary>
    /// 将表达式实时转换为SQLBuilder调用的访问器
    /// </summary>
    public class BaseExpressionSQLBuildVisitor:ExpressionVisitor
    {
        /// <summary>
        /// 持有的构建器
        /// </summary>
        public SQLBuilder Builder { get {
                return Context.CurrentLayer.Current;    
            } 
        }

        public FastCompileContext Context { get; set; }


        public BaseExpressionSQLBuildVisitor(FastCompileContext context)
        {
            this.Context = context;
        }
    }
}
