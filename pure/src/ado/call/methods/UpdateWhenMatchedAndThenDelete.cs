using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class UpdateWhenMatchedAndThenDeleteCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitUpdateWhenMatchedAndThenDelete(this);
        }
        public UpdateWhenMatchedAndThenDeleteCall() : base("UpdateWhenMatchedAndThenDelete", null)
        {

        }
    }
}
