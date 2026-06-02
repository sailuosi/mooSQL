using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    /// <summary>
    /// LINQ / 查询扩展方法 DELETE 的调用节点（DeleteCall）。
    /// </summary>
    public class DeleteCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitDelete(this);
        }
        /// <summary>
        /// 创建 DELETE 方法调用节点。
        /// </summary>
        public DeleteCall() : base("Delete", null)
        {

        }
    }

    /// <summary>
    /// LINQ / 查询扩展方法 DoDelete 的调用节点（DoDeleteCall）。
    /// </summary>
    public class DoDeleteCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitDoDelete(this);
        }
        /// <summary>
        /// 创建 DoDelete 方法调用节点。
        /// </summary>
        public DoDeleteCall() : base("DoDelete", null)
        {

        }
    }
}