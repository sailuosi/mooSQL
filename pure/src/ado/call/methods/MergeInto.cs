using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    /// <summary>
    /// LINQ / 查询扩展方法 MergeInto 的调用节点（MergeIntoCall）。
    /// </summary>
    public class MergeIntoCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitMergeInto(this);
        }
        /// <summary>
        /// 创建 MergeInto 方法调用节点。
        /// </summary>
        public MergeIntoCall() : base("MergeInto", null)
        {

        }
    }
}