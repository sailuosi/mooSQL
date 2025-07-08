using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class InsertWithOutputCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitInsertWithOutput(this);
        }
        public InsertWithOutputCall() : base("InsertWithOutput", null)
        {

        }
    }
}
