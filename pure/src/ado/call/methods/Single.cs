using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    /// <summary>
    /// LINQ / 查询扩展方法 SINGLE 的调用节点（SingleCall）。
    /// </summary>
    public class SingleCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitSingle(this);
        }
        /// <summary>
        /// 创建 SINGLE 方法调用节点。
        /// </summary>
        public SingleCall() : base("Single", null)
        {

        }
    }
}