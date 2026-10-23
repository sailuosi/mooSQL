using mooSQL.data.mapping;
using mooSQL.data.model;
using System;

namespace mooSQL.data
{
    /// <summary>
    /// SQLite 数据类型映射。实体 DDL 依赖此方法生成列类型文本；
    /// <see cref="GetDataType"/> 按声明类型 + 亲和规则解析读库结果。
    /// </summary>
    public class SQLiteMappingPanel : DefaultMappingPanel
    {
 /// <summary>
 /// Maps a DbDataType to a dialect-specific SQL type name.
 /// </summary>
 
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
                case DataFam.BinaryJson:
                case DataFam.Vector:
                case DataFam.Guid:
                    return "TEXT";
                case DataFam.DateTime:
                case DataFam.DateTime2:
                case DataFam.SmallDateTime:
                case DataFam.Date:
                case DataFam.Time:
                    return "TEXT";
                case DataFam.Boolean:
                case DataFam.SByte:
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

        /// <summary>
        /// 读库声明类型 → <see cref="DataFam"/>。
        /// 优先精确声明名，再按 SQLite 亲和（含 INT / CHAR|CLOB|TEXT / BLOB / REAL|FLOA|DOUB）。
        /// </summary>
        public override DataFam GetDataType(string? dataType, string? columnType = null)
        {
            var raw = (columnType ?? dataType ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(raw))
                return DataFam.Undefined;

            var open = raw.IndexOf('(');
            var baseName = (open > 0 ? raw.Substring(0, open) : raw).Trim().ToLowerInvariant();

            switch (baseName)
            {
                case "bool":
                case "boolean":
                    return DataFam.Boolean;
                case "tinyint":
                    return DataFam.SByte;
                case "smallint":
                case "int2":
                    return DataFam.Int16;
                case "int":
                case "integer":
                case "mediumint":
                case "int4":
                    return DataFam.Int32;
                case "bigint":
                case "int8":
                    return DataFam.Int64;
                case "real":
                case "float":
                case "double":
                case "double precision":
                    return DataFam.Double;
                case "numeric":
                case "decimal":
                case "number":
                    return DataFam.Decimal;
                case "money":
                    return DataFam.Money;
                case "char":
                case "character":
                case "nchar":
                    return DataFam.NChar;
                case "varchar":
                case "nvarchar":
                case "varying character":
                case "native character":
                    return DataFam.NVarChar;
                case "text":
                case "clob":
                case "longtext":
                case "mediumtext":
                case "tinytext":
                    return DataFam.Text;
                case "blob":
                case "binary":
                case "varbinary":
                    return DataFam.Blob;
                case "date":
                    return DataFam.Date;
                case "time":
                    return DataFam.Time;
                case "datetime":
                case "timestamp":
                    return DataFam.DateTime;
                case "guid":
                case "uuid":
                    return DataFam.Guid;
                case "json":
                    return DataFam.Json;
                case "xml":
                    return DataFam.Xml;
            }

            // SQLite 类型亲和（文档规则）
            if (raw.IndexOf("INT", StringComparison.OrdinalIgnoreCase) >= 0)
                return DataFam.Int64;
            if (raw.IndexOf("CHAR", StringComparison.OrdinalIgnoreCase) >= 0
                || raw.IndexOf("CLOB", StringComparison.OrdinalIgnoreCase) >= 0
                || raw.IndexOf("TEXT", StringComparison.OrdinalIgnoreCase) >= 0)
                return DataFam.Text;
            if (raw.IndexOf("BLOB", StringComparison.OrdinalIgnoreCase) >= 0)
                return DataFam.Blob;
            if (raw.IndexOf("REAL", StringComparison.OrdinalIgnoreCase) >= 0
                || raw.IndexOf("FLOA", StringComparison.OrdinalIgnoreCase) >= 0
                || raw.IndexOf("DOUB", StringComparison.OrdinalIgnoreCase) >= 0)
                return DataFam.Double;

            // NUMERIC 亲和（含 DECIMAL 等未命中精确名时）
            return DataFam.Decimal;
        }
    }
}
