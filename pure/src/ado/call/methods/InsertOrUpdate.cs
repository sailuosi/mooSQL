using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    /// <summary>
    /// LINQ / 查询扩展方法 InsertOrUpdate 的调用节点（InsertOrUpdateCall）。
    /// </summary>
    public class InsertOrUpdateCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitInsertOrUpdate(this);
        }
        /// <summary>
        /// 创建 InsertOrUpdate 方法调用节点。
        /// </summary>
        public InsertOrUpdateCall() : base("InsertOrUpdate", null)
        {

        }
    }
}