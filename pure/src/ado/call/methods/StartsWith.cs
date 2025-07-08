using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace mooSQL.data.call
{

    public class StartsWithCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitStartsWith(this);
        }
        public StartsWithCall() : base("StartsWith", null)
        {

        }
    }
}