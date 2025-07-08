using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class IgnoreFiltersCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitIgnoreFilters(this);
        }
        public IgnoreFiltersCall() : base("IgnoreFilters", null)
        {

        }
    }
}
