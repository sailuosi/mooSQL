using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace mooSQL.data.call
{

    /// <summary>
    /// LINQ / 查询扩展方法 InjectSQL 的调用节点（InjectSQLCall）。
    /// </summary>
    public class InjectSQLCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitInjectSQL(this);
        }
        /// <summary>
        /// 创建 InjectSQL 方法调用节点。
        /// </summary>
        public InjectSQLCall() : base("InjectSQL", null)
        {

        }
    }
}