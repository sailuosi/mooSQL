using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    /// <summary>
    /// LINQ / 查询扩展方法 InlineParameters 的调用节点（InlineParametersCall）。
    /// </summary>
    public class InlineParametersCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitInlineParameters(this);
        }
        /// <summary>
        /// 创建 InlineParameters 方法调用节点。
        /// </summary>
        public InlineParametersCall() : base("InlineParameters", null)
        {

        }
    }
}