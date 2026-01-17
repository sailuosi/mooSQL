using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class TagQueryCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitTagQuery(this);
        }
        public TagQueryCall() : base("TagQuery", null)
        {

        }
    }
}
