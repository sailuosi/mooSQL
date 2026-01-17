using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.utils
{
    public class TupleValue<T1,T2>
    {
        public TupleValue(T1 val1, T2 Val2) { 
            this.Value1 = val1; this.Value2 = Val2;
        }

        public T1 Value1 { get; set; }
        public T2 Value2 { get; set; }
    }
}
