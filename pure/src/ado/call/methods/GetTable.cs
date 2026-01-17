using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class GetTableCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitGetTable(this);
        }
        public GetTableCall() : base("GetTable", null)
        {

        }
    }
}
