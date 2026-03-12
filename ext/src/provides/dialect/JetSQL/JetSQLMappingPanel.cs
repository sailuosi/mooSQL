
using mooSQL.data.mapping;
using mooSQL.data.model;
using System;

namespace mooSQL.data
{
    /// <summary>
    /// Jet / Access / Excel 数据类型映射：Integer, Long, Single, Double, Currency, DateTime, Bit, VarChar, LongText 等。
    /// </summary>
    public class JetSQLMappingPanel : DefaultMappingPanel
    {
        public JetSQLMappingPanel() { }

        public override string DbDataTypeToSQL(DbDataType type)
        {
            switch (type.DataType)
            {
                case DataFam.VarChar:
                    return type.Length > 0 ? string.Format("VARCHAR({0})", type.Length) : "VARCHAR(255)";
                case DataFam.NVarChar:
                    return type.Length > 0 ? string.Format("NVARCHAR({0})", type.Length) : "NVARCHAR(255)";
                case DataFam.Char:
                    return type.Length > 0 ? string.Format("CHAR({0})", type.Length) : "CHAR(1)";
                case DataFam.DateTime:
                case DataFam.DateTime2:
                case DataFam.SmallDateTime:
                    return "DATETIME";
                case DataFam.Date:
                    return "DATETIME";
                case DataFam.Time:
                    return "DATETIME";
                case DataFam.Boolean:
                    return "BIT";
                case DataFam.Decimal:
                case DataFam.VarNumeric:
                    var p = type.Precision ?? 19;
                    var s = type.Scale ?? 4;
                    return string.Format("DECIMAL({0},{1})", p, s);
                case DataFam.Double:
                    return "DOUBLE";
                case DataFam.Single:
                    return "SINGLE";
                case DataFam.Guid:
                    return "GUID";
                case DataFam.Int32:
                    return "INTEGER";
                case DataFam.Int64:
                case DataFam.Long:
                    return "BIGINT";
                case DataFam.Int16:
                    return "SMALLINT";
                case DataFam.Byte:
                    return "TINYINT";
                case DataFam.Money:
                case DataFam.SmallMoney:
                    return "CURRENCY";
                case DataFam.Text:
                case DataFam.LongText:
                case DataFam.NText:
                case DataFam.Json:
                    return "LONGTEXT";
                case DataFam.VarBinary:
                case DataFam.Binary:
                    return type.Length > 0 ? string.Format("VARBINARY({0})", type.Length) : "VARBINARY(255)";
                case DataFam.Image:
                    return "LONGTEXT";
            }
            return base.DbDataTypeToSQL(type);
        }

        public override DataFam GetDataType(string dataType, string columnType = null)
        {
            if (string.IsNullOrWhiteSpace(dataType)) return DataFam.Undefined;
            var t = dataType.Trim();
            switch (t.ToUpperInvariant())
            {
                case "BIT":
                case "BOOLEAN":
                    return DataFam.Boolean;
                case "TINYINT":
                case "BYTE":
                    return DataFam.Byte;
                case "SMALLINT":
                case "INTEGER":  // Jet INTEGER = 2 bytes
                    return DataFam.Int16;
                case "INT":
                case "LONG":    // Jet LONG = 4 bytes, AutoNumber
                    return DataFam.Int32;
                case "BIGINT":
                    return DataFam.Int64;
                case "SINGLE":
                case "REAL":
                    return DataFam.Single;
                case "DOUBLE":
                case "FLOAT":
                    return DataFam.Double;
                case "CURRENCY":
                    return DataFam.Money;
                case "DECIMAL":
                case "NUMERIC":
                case "VARNUMERIC":
                    return DataFam.Decimal;
                case "DATETIME":
                case "DATE":
                    return DataFam.DateTime;
                case "CHAR":
                    return DataFam.Char;
                case "VARCHAR":
                case "TEXT":
                case "VARWCHAR":
                    return DataFam.VarChar;
                case "LONGTEXT":
                case "MEMO":
                case "LONGVARCHAR":
                case "LONGVARWCHAR":
                    return DataFam.LongText;
                case "BINARY":
                    return DataFam.Binary;
                case "VARBINARY":
                    return DataFam.VarBinary;
                case "GUID":
                    return DataFam.Guid;
            }
            return base.GetDataType(dataType, columnType);
        }
    }
}
