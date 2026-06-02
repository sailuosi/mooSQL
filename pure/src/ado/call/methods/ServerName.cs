using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    /// <summary>
    /// LINQ / 查询扩展方法 ServerName 的调用节点（ServerNameCall）。
    /// </summary>
    public class ServerNameCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitServerName(this);
        }
        /// <summary>
        /// 创建 ServerName 方法调用节点。
        /// </summary>
        public ServerNameCall() : base("ServerName", null)
        {

        }
    }
}