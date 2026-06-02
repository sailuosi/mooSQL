using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    /// <summary>
    /// LINQ / 查询扩展方法 CONTAINS 的调用节点（ContainsCall）。
    /// </summary>
    public class ContainsCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitContains(this);
        }
        /// <summary>
        /// 创建 CONTAINS 方法调用节点。
        /// </summary>
        public ContainsCall() : base("Contains", null)
        {

        }
    }
}