
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

        /// <summary>
        /// 当前快速编译上下文（分层 Builder、实体昵称等）。
        /// </summary>
        public FastCompileContext Context { get; set; }


        /// <summary>
        /// 使用指定的编译上下文创建访问器。
        /// </summary>
        /// <param name="context">快速编译上下文。</param>
        public BaseExpressionSQLBuildVisitor(FastCompileContext context)
        {
            this.Context = context;
        }
    }
}
