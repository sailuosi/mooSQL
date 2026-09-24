#if NET10_0_OR_GREATER
using mooSQL.data.mapping;
using mooSQL.data.model;

namespace mooSQL.data
{
    /// <summary>
    /// SonnetDB CLR → SQL 类型映射（关系表：INT/FLOAT/BOOL/STRING/DATETIME/BLOB/JSON）。
    /// </summary>
    public class SonnetDBMappingPanel : DefaultMappingPanel
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
                case DataFam.Guid:
                    return "STRING";
                case DataFam.Json:
                    return "JSON";
                case DataFam.DateTime:
                case DataFam.DateTime2:
                case DataFam.SmallDateTime:
                case DataFam.Date:
                case DataFam.Time:
                    return "DATETIME";
                case DataFam.Boolean:
                    return "BOOL";
                case DataFam.Byte:
                case DataFam.Int16:
                case DataFam.Int32:
                case DataFam.Int64:
                case DataFam.Long:
                case DataFam.UInt16:
                case DataFam.UInt32:
                case DataFam.UInt64:
                    return "INT";
                case DataFam.Decimal:
                case DataFam.VarNumeric:
                case DataFam.Money:
                case DataFam.Double:
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
