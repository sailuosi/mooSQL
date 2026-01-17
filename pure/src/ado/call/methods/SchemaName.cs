using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class SchemaNameCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitSchemaName(this);
        }
        public SchemaNameCall() : base("SchemaName", null)
        {

        }
    }
}
