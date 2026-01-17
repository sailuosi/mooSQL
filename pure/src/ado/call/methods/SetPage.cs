using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace mooSQL.data.call
{

    public class SetPageCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitSetPage(this);
        }
        public SetPageCall() : base("SetPage", null)
        {

        }
    }
}