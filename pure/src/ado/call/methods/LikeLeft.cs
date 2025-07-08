using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace mooSQL.data.call
{

    public class LikeLeftCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitLikeLeft(this);
        }
        public LikeLeftCall() : base("LikeLeft", null)
        {

        }
    }
}