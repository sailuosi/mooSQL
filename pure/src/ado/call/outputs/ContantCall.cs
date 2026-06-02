using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{


    /// <summary>
    /// 类型 ConstantCall。
    /// </summary>
    public class ConstantCall : MethodCall
    {
        /// <summary>
        /// 接受访问者并翻译为 SQL 片段。
        /// </summary>
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return this;
        }
        /// <summary>
        /// 创建 Constant 方法调用节点。
        /// </summary>
        public ConstantCall() : base("Constant", null)
        {

        }

        /// <summary>
        /// 属性 Value（object）。
        /// </summary>
        public object Value { get; set; }
    }
}