using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class ElementAtCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitElementAt(this);
        }
        public ElementAtCall() : base("ElementAt", null)
        {

        }
    }
}
