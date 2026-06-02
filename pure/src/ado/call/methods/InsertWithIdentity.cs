using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    /// <summary>
    /// LINQ / 查询扩展方法 InsertWithIdentity 的调用节点（InsertWithIdentityCall）。
    /// </summary>
    public class InsertWithIdentityCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitInsertWithIdentity(this);
        }
        /// <summary>
        /// 创建 InsertWithIdentity 方法调用节点。
        /// </summary>
        public InsertWithIdentityCall() : base("InsertWithIdentity", null)
        {

        }
    }
}