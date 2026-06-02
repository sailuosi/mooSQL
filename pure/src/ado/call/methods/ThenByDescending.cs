using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    /// <summary>
    /// LINQ / 查询扩展方法 THEN BY DESC 的调用节点（ThenByDescendingCall）。
    /// </summary>
    public class ThenByDescendingCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitThenByDescending(this);
        }
        /// <summary>
        /// 创建 THEN BY DESC 方法调用节点。
        /// </summary>
        public ThenByDescendingCall() : base("ThenByDescending", null)
        {

        }
    }
}