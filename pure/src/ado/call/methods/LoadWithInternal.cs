using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class LoadWithInternalCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitLoadWithInternal(this);
        }
        public LoadWithInternalCall() : base("LoadWithInternal", null)
        {

        }
    }
}
