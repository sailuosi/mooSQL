#if NET6_0_OR_GREATER
using mooSQL.data.mapping;
using mooSQL.data.model;

namespace mooSQL.data
{
    /// <summary>
    /// ClickHouse CLR → SQL 类型映射。
    /// </summary>
    public class ClickHouseMappingPanel : DefaultMappingPanel
    {
        public override string DbDataTypeToSQL(DbDataType type)
        {
            switch (type.DataType)
            {
                case DataFam.VarChar:
                case DataFam.NVarChar:
                case DataFam.Char:
                case DataFam.Text:
                case DataFam.LongText:
                case DataFam.NText:
                case DataFam.Xml:
                case DataFam.Json:
                    return "String";
                case DataFam.Guid:
                    return "UUID";
                case DataFam.DateTime:
                case DataFam.DateTime2:
                case DataFam.SmallDateTime:
                    return "DateTime64(3)";
                case DataFam.Date:
                    return "Date";
                case DataFam.Time:
                    return "String";
                case DataFam.Boolean:
                    return "UInt8";
                case DataFam.Byte:
                    return "UInt8";
                case DataFam.Int16:
                    return "Int16";
                case DataFam.Int32:
                    return "Int32";
                case DataFam.Int64:
                case DataFam.Long:
                    return "Int64";
                case DataFam.UInt16:
                    return "UInt16";
                case DataFam.UInt32:
                    return "UInt32";
                case DataFam.UInt64:
                    return "UInt64";
                case DataFam.Decimal:
                case DataFam.VarNumeric:
                case DataFam.Money:
                    if (type.Precision > 0)
                        return $"Decimal({type.Precision},{type.Scale})";
                    return "Decimal(38, 18)";
                case DataFam.Double:
                    return "Float64";
                case DataFam.Single:
                    return "Float32";
                case DataFam.Binary:
                case DataFam.VarBinary:
                case DataFam.Blob:
                case DataFam.Image:
                    return "String";
            }

            return base.DbDataTypeToSQL(type);
        }
    }
}
#endif
