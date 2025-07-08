using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class FromSqlScalarCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitFromSqlScalar(this);
        }
        public FromSqlScalarCall() : base("FromSqlScalar", null)
        {

        }
    }
}
