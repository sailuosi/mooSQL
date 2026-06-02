using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    /// <summary>
    /// LINQ / 查询扩展方法 EXCEPT 的调用节点（ExceptCall）。
    /// </summary>
    public class ExceptCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitExcept(this);
        }
        /// <summary>
        /// 创建 EXCEPT 方法调用节点。
        /// </summary>
        public ExceptCall() : base("Except", null)
        {

        }
    }
}