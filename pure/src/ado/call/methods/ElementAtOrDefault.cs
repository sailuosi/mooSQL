using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class ElementAtOrDefaultCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitElementAtOrDefault(this);
        }
        public ElementAtOrDefaultCall() : base("ElementAtOrDefault", null)
        {

        }
    }
}
