using System;
using System.Collections.Generic;
using System.Linq;

using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{
    public class AliasCall:MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitAlias(this);
        }
        public AliasCall() : base("Alias", null)
        {
            
        }
    }
}
