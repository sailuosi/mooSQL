using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class AsSubQueryCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitAsSubQuery(this);
        }
        public AsSubQueryCall() : base("AsSubQuery", null)
        {

        }
    }
}
