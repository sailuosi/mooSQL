using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class DeleteCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitDelete(this);
        }
        public DeleteCall() : base("Delete", null)
        {

        }
    }

    public class DoDeleteCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitDoDelete(this);
        }
        public DoDeleteCall() : base("DoDelete", null)
        {

        }
    }
}
