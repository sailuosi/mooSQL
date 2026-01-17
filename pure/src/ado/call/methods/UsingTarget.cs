using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class UsingTargetCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitUsingTarget(this);
        }
        public UsingTargetCall() : base("UsingTarget", null)
        {

        }
    }
}
