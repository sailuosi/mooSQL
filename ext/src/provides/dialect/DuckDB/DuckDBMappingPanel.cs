#if NET6_0_OR_GREATER
using mooSQL.data.mapping;
using mooSQL.data.model;

namespace mooSQL.data
{
    /// <summary>
    /// DuckDB CLR → SQL 类型映射。
    /// </summary>
    public class DuckDBMappingPanel : DefaultMappingPanel
    {
        public override string DbDataTypeToSQL(DbDataType type)
        {
            switch (type.DataType)
            {
                case DataFam.VarChar:
                case DataFam.NVarChar:
                case DataFam.Char:
                    if (type.Length > 0)
                        return $"VARCHAR({type.Length})";
                    return "VARCHAR";
                case DataFam.Text:
                case DataFam.LongText:
                case DataFam.NText:
                case DataFam.Xml:
                case DataFam.Json:
                    return "VARCHAR";
                case DataFam.Guid:
                    return "UUID";
                case DataFam.DateTime:
                case DataFam.DateTime2:
                case DataFam.SmallDateTime:
                    return "TIMESTAMP";
                case DataFam.Date:
                    return "DATE";
                case DataFam.Time:
                    return "TIME";
                case DataFam.Boolean:
                    return "BOOLEAN";
                case DataFam.Byte:
                case DataFam.Int16:
                    return "SMALLINT";
                case DataFam.Int32:
                    return "INTEGER";
                case DataFam.Int64:
                case DataFam.Long:
                    return "BIGINT";
                case DataFam.UInt16:
                case DataFam.UInt32:
                case DataFam.UInt64:
                    return "UBIGINT";
                case DataFam.Decimal:
                case DataFam.VarNumeric:
                case DataFam.Money:
                    if (type.Precision > 0)
                        return $"DECIMAL({type.Precision},{type.Scale})";
                    return "DECIMAL";
                case DataFam.Double:
                    return "DOUBLE";
                case DataFam.Single:
                    return "FLOAT";
                case DataFam.Binary:
                case DataFam.VarBinary:
                case DataFam.Blob:
                case DataFam.Image:
                    return "BLOB";
            }

            return base.DbDataTypeToSQL(type);
        }
    }
}
#endif
