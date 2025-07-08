using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class DeleteWithOutputCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitDeleteWithOutput(this);
        }
        public DeleteWithOutputCall() : base("DeleteWithOutput", null)
        {

        }
    }
}
