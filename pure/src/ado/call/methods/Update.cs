using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class UpdateCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitUpdate(this);
        }
        public UpdateCall() : base("Update", null)
        {

        }
    }
    public class DoUpdateCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitDoUpdate(this);
        }
        public DoUpdateCall() : base("DoUpdateCall", null)
        {

        }
    }
}
