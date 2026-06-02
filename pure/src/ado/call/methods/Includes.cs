using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace mooSQL.data.call
{
    /// <summary>
    /// 导航查询包含方法
    /// </summary>
    public class IncludesCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitIncludes(this);
        }
        /// <summary>
        /// 创建 Includes 方法调用节点。
        /// </summary>
        public IncludesCall() : base("Includes", null)
        {

        }
    }
}