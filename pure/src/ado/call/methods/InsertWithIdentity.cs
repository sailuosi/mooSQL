using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class InsertWithIdentityCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitInsertWithIdentity(this);
        }
        public InsertWithIdentityCall() : base("InsertWithIdentity", null)
        {

        }
    }
}
