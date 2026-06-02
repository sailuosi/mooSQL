using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    /// <summary>
    /// LINQ / 查询扩展方法 LEFT JOIN 的调用节点（LeftJoinCall）。
    /// </summary>
    public class LeftJoinCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitLeftJoin(this);
        }
        /// <summary>
        /// 创建 LEFT JOIN 方法调用节点。
        /// </summary>
        public LeftJoinCall() : base("LeftJoin", null)
        {

        }
    }
}