using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    /// <summary>
    /// LINQ / 查询扩展方法 InsertWhenNotMatchedAnd 的调用节点（InsertWhenNotMatchedAndCall）。
    /// </summary>
    public class InsertWhenNotMatchedAndCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitInsertWhenNotMatchedAnd(this);
        }
        /// <summary>
        /// 创建 InsertWhenNotMatchedAnd 方法调用节点。
        /// </summary>
        public InsertWhenNotMatchedAndCall() : base("InsertWhenNotMatchedAnd", null)
        {

        }
    }
}