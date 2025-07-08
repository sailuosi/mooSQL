using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class MergeIntoCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitMergeInto(this);
        }
        public MergeIntoCall() : base("MergeInto", null)
        {

        }
    }
}
