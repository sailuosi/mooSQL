using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    /// <summary>
    /// LINQ / 查询扩展方法 INTO 的调用节点（IntoCall）。
    /// </summary>
    public class IntoCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitInto(this);
        }
        /// <summary>
        /// 创建 INTO 方法调用节点。
        /// </summary>
        public IntoCall() : base("Into", null)
        {

        }
    }
}