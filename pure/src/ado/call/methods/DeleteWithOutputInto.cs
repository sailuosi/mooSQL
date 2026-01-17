using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class DeleteWithOutputIntoCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitDeleteWithOutputInto(this);
        }
        public DeleteWithOutputIntoCall() : base("DeleteWithOutputInto", null)
        {

        }
    }
}
