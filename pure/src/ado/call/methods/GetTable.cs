using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    /// <summary>
    /// LINQ / 查询扩展方法 GetTable 的调用节点（GetTableCall）。
    /// </summary>
    public class GetTableCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitGetTable(this);
        }
        /// <summary>
        /// 创建 GetTable 方法调用节点。
        /// </summary>
        public GetTableCall() : base("GetTable", null)
        {

        }
    }
}