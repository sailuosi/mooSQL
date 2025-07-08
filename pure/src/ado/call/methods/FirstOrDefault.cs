using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class FirstOrDefaultCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitFirstOrDefault(this);
        }
        public FirstOrDefaultCall() : base("FirstOrDefault", null)
        {

        }
    }
}
