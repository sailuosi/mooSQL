using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class SingleOrDefaultCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitSingleOrDefault(this);
        }
        public SingleOrDefaultCall() : base("SingleOrDefault", null)
        {

        }
    }
}
