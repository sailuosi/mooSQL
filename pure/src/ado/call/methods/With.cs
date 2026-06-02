using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    /// <summary>
    /// LINQ / 查询扩展方法 With 的调用节点（WithCall）。
    /// </summary>
    public class WithCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitWith(this);
        }
        /// <summary>
        /// 创建 With 方法调用节点。
        /// </summary>
        public WithCall() : base("With", null)
        {

        }
    }
}