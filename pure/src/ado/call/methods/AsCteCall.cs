using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class AsCteCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitAsCte(this);
        }
        public AsCteCall() : base("AsCte", null)
        {

        }
    }
}
