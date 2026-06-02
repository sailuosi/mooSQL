using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    /// <summary>
    /// CASE WHEN 表达式的方法调用节点。
    /// </summary>
    public class WhenCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitWhen(this);
        }
        /// <summary>
        /// 创建 When 调用节点。
        /// </summary>
        public WhenCall() : base("When", null)
        {

        }
    }
}