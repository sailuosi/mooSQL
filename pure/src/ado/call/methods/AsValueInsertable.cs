using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class AsValueInsertableCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitAsValueInsertable(this);
        }
        public AsValueInsertableCall() : base("AsValueInsertable", null)
        {

        }
    }
}
