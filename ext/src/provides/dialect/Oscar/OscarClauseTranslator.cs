using mooSQL.data.model;
using mooSQL.linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public class OscarClauseTranslator:ClauseTranslateVisitor
    {
        public OscarClauseTranslator(Dialect dialect):base(dialect) { 
        
        }

        public override IExpWord? GetIdentityExpression(FieldWord field)
        {
            if (field.ColumnDescriptor !=null)
            {
                var col = field.ColumnDescriptor;
                var name = !string.IsNullOrWhiteSpace(col.SequenceName);

                if (name != null)
                    return new ExpressionWord(
                            (col.SequenceSchema != null ? TranslateValue(col.SequenceSchema, ConvertType.NameToSchema) + "." : null) +
                            TranslateValue(col.SequenceName, ConvertType.SequenceName) +
                            ".nextval",
                        PrecedenceLv.Primary);
            }

            return base.GetIdentityExpression(field);
        }
    }
}
