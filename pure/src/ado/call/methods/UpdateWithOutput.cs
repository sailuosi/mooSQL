using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class UpdateWithOutputCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitUpdateWithOutput(this);
        }
        public UpdateWithOutputCall() : base("UpdateWithOutput", null)
        {

        }
    }
}
