using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    /// <summary>
    /// LINQ / 查询扩展方法 UpdateWithOutput 的调用节点（UpdateWithOutputCall）。
    /// </summary>
    public class UpdateWithOutputCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitUpdateWithOutput(this);
        }
        /// <summary>
        /// 创建 UpdateWithOutput 方法调用节点。
        /// </summary>
        public UpdateWithOutputCall() : base("UpdateWithOutput", null)
        {

        }
    }
}