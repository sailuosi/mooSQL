using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class LoadWithAsTableCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitLoadWithAsTable(this);
        }
        public LoadWithAsTableCall() : base("LoadWithAsTable", null)
        {

        }
    }
}
