using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    /// <summary>
    /// LINQ / 查询扩展方法 SELECT 投影 的调用节点（SelectCall）。
    /// </summary>
    public class SelectCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitSelect(this);
        }
        /// <summary>
        /// 创建 SELECT 投影 方法调用节点。
        /// </summary>
        public SelectCall() : base("Select", null)
        {

        }
    }
}