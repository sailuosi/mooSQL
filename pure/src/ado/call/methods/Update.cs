using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    /// <summary>
    /// LINQ / 查询扩展方法 UPDATE 的调用节点（UpdateCall）。
    /// </summary>
    public class UpdateCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitUpdate(this);
        }
        /// <summary>
        /// 创建 UPDATE 方法调用节点。
        /// </summary>
        public UpdateCall() : base("Update", null)
        {

        }
    }
    /// <summary>
    /// LINQ / 查询扩展方法 DoUpdate 的调用节点（DoUpdateCall）。
    /// </summary>
    public class DoUpdateCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitDoUpdate(this);
        }
        /// <summary>
        /// 创建 DoUpdate 方法调用节点。
        /// </summary>
        public DoUpdateCall() : base("DoUpdateCall", null)
        {

        }
    }
}