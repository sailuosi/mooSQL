using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    /// <summary>
    /// LINQ / 查询扩展方法 ORDER BY DESC 的调用节点（OrderByDescendingCall）。
    /// </summary>
    public class OrderByDescendingCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitOrderByDescending(this);
        }
        /// <summary>
        /// 创建 ORDER BY DESC 方法调用节点。
        /// </summary>
        public OrderByDescendingCall() : base("OrderByDescending", null)
        {

        }
    }
}