using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{


    public class ConstantCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return this;
        }
        public ConstantCall() : base("Constant", null)
        {

        }

        public object Value { get; set; }
    }
}
