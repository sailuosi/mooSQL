using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    /// <summary>
    /// LINQ / 查询扩展方法 LONG COUNT 的调用节点（LongCountCall）。
    /// </summary>
    public class LongCountCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitLongCount(this);
        }
        /// <summary>
        /// 创建 LONG COUNT 方法调用节点。
        /// </summary>
        public LongCountCall() : base("LongCount", null)
        {

        }
    }
}