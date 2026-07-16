using mooSQL.data.mapping;
using mooSQL.data.model;
using System;

namespace mooSQL.data
{
    /// <summary>
    /// SQLite 数据类型映射。实体 DDL 依赖此方法生成列类型文本。
    /// </summary>
    public class SQLiteMappingPanel : DefaultMappingPanel
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
                case DataFam.Guid:
                    return "TEXT";
                case DataFam.DateTime:
                case DataFam.DateTime2:
                case DataFam.SmallDateTime:
                case DataFam.Date:
                case DataFam.Time:
                    return "TEXT";
                case DataFam.Boolean:
                case DataFam.Byte:
                case DataFam.Int16:
                case DataFam.Int32:
                case DataFam.Int64:
                case DataFam.Long:
                case DataFam.UInt16:
                case DataFam.UInt32:
                case DataFam.UInt64:
                    return "INTEGER";
                case DataFam.Decimal:
                case DataFam.VarNumeric:
                case DataFam.Double:
                case DataFam.Single:
                case DataFam.Money:
                    return "REAL";
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
