using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    /// <summary>
    /// LINQ / 查询扩展方法 InsertWithOutput 的调用节点（InsertWithOutputCall）。
    /// </summary>
    public class InsertWithOutputCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitInsertWithOutput(this);
        }
        /// <summary>
        /// 创建 InsertWithOutput 方法调用节点。
        /// </summary>
        public InsertWithOutputCall() : base("InsertWithOutput", null)
        {

        }
    }
}