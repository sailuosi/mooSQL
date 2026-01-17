using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class IsTemporaryCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitIsTemporary(this);
        }
        public IsTemporaryCall() : base("IsTemporary", null)
        {

        }
    }
}
