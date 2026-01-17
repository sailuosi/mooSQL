using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class InsertFirstCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitInsertFirst(this);
        }
        public InsertFirstCall() : base("InsertFirst", null)
        {

        }
    }
}
