using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class DefaultIfEmptyCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitDefaultIfEmpty(this);
        }
        public DefaultIfEmptyCall() : base("DefaultIfEmpty", null)
        {

        }
    }
}
