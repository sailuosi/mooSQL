using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    /// <summary>
    /// LINQ / 查询扩展方法 Sink 的调用节点（SinkCall）。
    /// </summary>
    public class SinkCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitSink(this);
        }
        /// <summary>
        /// 创建 Sink 方法调用节点。
        /// </summary>
        public SinkCall() : base("Sink", null)
        {

        }
    }

    /// <summary>
    /// LINQ / 查询扩展方法 Rise 的调用节点（RiseCall）。
    /// </summary>
    public class RiseCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitRise(this);
        }
        /// <summary>
        /// 创建 Rise 方法调用节点。
        /// </summary>
        public RiseCall() : base("Rise", null)
        {

        }
    }

    /// <summary>
    /// LINQ / 查询扩展方法 SinkOR 的调用节点（SinkORCall）。
    /// </summary>
    public class SinkORCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitSinkOR(this);
        }
        /// <summary>
        /// 创建 SinkOR 方法调用节点。
        /// </summary>
        public SinkORCall() : base("SinkOR", null)
        {

        }
    }
}