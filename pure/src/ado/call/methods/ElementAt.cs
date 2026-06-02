using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    /// <summary>
    /// LINQ / 查询扩展方法 ElementAt 的调用节点（ElementAtCall）。
    /// </summary>
    public class ElementAtCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitElementAt(this);
        }
        /// <summary>
        /// 创建 ElementAt 方法调用节点。
        /// </summary>
        public ElementAtCall() : base("ElementAt", null)
        {

        }
    }
}