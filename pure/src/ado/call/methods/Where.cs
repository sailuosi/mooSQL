using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    /// <summary>
    /// LINQ / 查询扩展方法 WHERE 条件 的调用节点（WhereCall）。
    /// </summary>
    public class WhereCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitWhere(this);
        }
        /// <summary>
        /// 创建 WHERE 条件 方法调用节点。
        /// </summary>
        public WhereCall() : base("Where", null)
        {

        }
    }
}