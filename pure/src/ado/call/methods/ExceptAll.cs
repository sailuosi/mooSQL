using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class ExceptAllCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitExceptAll(this);
        }
        public ExceptAllCall() : base("ExceptAll", null)
        {

        }
    }
}
