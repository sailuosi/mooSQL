using mooSQL.data.model;
using mooSQL.linq;

namespace mooSQL.data
{
    /// <summary>
    /// 达梦子句翻译：序列 nextval；无序列名时走基类 IDENTITY 路径。
    /// </summary>
    public class DMClauseTranslator : ClauseTranslateVisitor
    {
        public DMClauseTranslator(Dialect dialect) : base(dialect)
        {
        }

        public override IExpWord? GetIdentityExpression(FieldWord field)
        {
            if (field.ColumnDescriptor != null)
            {
                var col = field.ColumnDescriptor;
                if (!string.IsNullOrWhiteSpace(col.SequenceName))
                {
                    return new ExpressionWord(
                        (col.SequenceSchema != null
                            ? TranslateValue(col.SequenceSchema, ConvertType.NameToSchema) + "."
                            : null)
                        + TranslateValue(col.SequenceName, ConvertType.SequenceName)
                        + ".nextval",
                        PrecedenceLv.Primary);
                }
            }

            return base.GetIdentityExpression(field);
        }
    }
}
