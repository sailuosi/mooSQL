using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class MergeWithOutputCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitMergeWithOutput(this);
        }
        public MergeWithOutputCall() : base("MergeWithOutput", null)
        {

        }
    }
}
