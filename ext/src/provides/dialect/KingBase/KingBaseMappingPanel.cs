#if !NET451
using mooSQL.data.mapping;
using mooSQL.data.model;

namespace mooSQL.data
{
    /// <summary>
    /// 金仓类型映射：PG 兼容字面量，不依赖 NpgsqlTypes。
    /// </summary>
    public class KingBaseMappingPanel : DefaultMappingPanel
    {
        public override string DbDataTypeToSQL(DbDataType type)
        {
            switch (type.DataType)
            {
                case DataFam.VarChar:
                case DataFam.NVarChar:
                    if (type.Length > 0)
                        return $"varchar({type.Length})";
                    return "text";
                case DataFam.Char:
                case DataFam.NChar:
                    if (type.Length > 0)
                        return $"character({type.Length})";
                    return "character(1)";
                case DataFam.Text:
                case DataFam.LongText:
                case DataFam.NText:
                case DataFam.Xml:
                case DataFam.Json:
                    return "text";
                case DataFam.Guid:
                    return "uuid";
                case DataFam.DateTime:
                case DataFam.DateTime2:
                case DataFam.SmallDateTime:
                    return "timestamp";
                case DataFam.DateTimeOffset:
                    return "timestamptz";
                case DataFam.Date:
                    return "date";
                case DataFam.Time:
                    return "time";
                case DataFam.Boolean:
                    return "boolean";
                case DataFam.Byte:
                case DataFam.Int16:
                    return "smallint";
                case DataFam.Int32:
                    return "integer";
                case DataFam.Int64:
                case DataFam.Long:
                    return "bigint";
                case DataFam.Decimal:
                case DataFam.VarNumeric:
                case DataFam.Money:
                    if (type.Precision > 0)
                        return $"numeric({type.Precision},{type.Scale})";
                    return "numeric";
                case DataFam.Double:
                    return "double precision";
                case DataFam.Single:
                    return "real";
                case DataFam.Binary:
                case DataFam.VarBinary:
                case DataFam.Blob:
                case DataFam.Image:
                    return "bytea";
            }

            return base.DbDataTypeToSQL(type);
        }
    }
}
#endif
