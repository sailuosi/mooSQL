using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace mooSQL.data.call
{

    public class InjectSQLCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitInjectSQL(this);
        }
        public InjectSQLCall() : base("InjectSQL", null)
        {

        }
    }
}
