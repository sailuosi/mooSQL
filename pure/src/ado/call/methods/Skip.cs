using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    /// <summary>
    /// LINQ / 查询扩展方法 SKIP 的调用节点（SkipCall）。
    /// </summary>
    public class SkipCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitSkip(this);
        }
        /// <summary>
        /// 创建 SKIP 方法调用节点。
        /// </summary>
        public SkipCall() : base("Skip", null)
        {

        }
    }
}