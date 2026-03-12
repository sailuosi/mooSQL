
using mooSQL.linq.Common.Internal;
using mooSQL.linq.Mapping;
using mooSQL.linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data.model;

namespace mooSQL.data
{
    public class NpgClauseTranslator: ClauseTranslateVisitor
    {
        public NpgClauseTranslator(Dialect dia) : base(dia) { 
        
        }

        public override IExpWord? GetIdentityExpression(FieldWord field)
        {
            if (field.ColumnDescriptor != null)
            {
                var col = field.ColumnDescriptor;
                var name = (col.SequenceName);

                if (!string.IsNullOrWhiteSpace(name)) { 
                    var tb=col.belongTable;
                    var sequenceName = new SqlObjectName(col.SequenceName, Server: tb.ServerName, Database: tb.DatabaseName, Schema: col.SequenceSchema ?? tb.SchemaName);

                    using var sb = Pools.StringBuilder.Allocate();
                    sb.Value.Append("nextval(");
                    var val = TranslateObjectName(sequenceName, ConvertType.SequenceName, true, TableOptions.NotSet);
                    //TODO 重构时此处引用不可抵达，待后续需使用时再行优化。
                    //MappingSchema.ConvertToSqlValue(sb.Value, null, DataOptions, val);
                    sb.Value.Append(val);
                    sb.Value.Append(')');
                    return new ExpressionWord(sb.Value.ToString(), PrecedenceLv.Primary);

            
                }

            }

            return base.GetIdentityExpression(field);
        }
    }
}
