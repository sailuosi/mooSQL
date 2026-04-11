using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.utils
{
    /// <summary>
    /// 二元值容器，用于临时承载两个强类型值。
    /// </summary>
    /// <typeparam name="T1">第一项类型。</typeparam>
    /// <typeparam name="T2">第二项类型。</typeparam>
    public class TupleValue<T1,T2>
    {
        /// <summary>
        /// 使用两个值构造实例。
        /// </summary>
        public TupleValue(T1 val1, T2 Val2) { 
            this.Value1 = val1; this.Value2 = Val2;
        }

        /// <summary>
        /// 第一项。
        /// </summary>
        public T1 Value1 { get; set; }
        /// <summary>
        /// 第二项。
        /// </summary>
        public T2 Value2 { get; set; }
    }
}
