using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class InsertWithOutputIntoCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitInsertWithOutputInto(this);
        }
        public InsertWithOutputIntoCall() : base("InsertWithOutputInto", null)
        {

        }
    }
}
