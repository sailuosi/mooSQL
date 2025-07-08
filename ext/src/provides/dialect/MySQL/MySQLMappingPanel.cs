using mooSQL.data.mapping;
using mooSQL.data.model;
using mooSQL.linq.Common;
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public class MySQLMappingPanel:DefaultMappingPanel
    {
        public MySQLMappingPanel()
        {

            this.SetDataType(typeof(string), DataType.NVarChar);

            this.SetValueToSql<string>( (v) => ConvertStringToSql(v));
            this.SetValueToSql<char>( ( v) => ConvertCharToSql(v));
            this.SetValueToSql<byte[]>( ( v) => ConvertBinaryToSql(v));
            this.SetValueToSql<Binary>( (v) => ConvertBinaryToSql(v.ToArray()));

        }

        static string ConvertStringToSql(string value)
        {
            var res = "'";
            res += (value.Replace("\\", "\\\\").Replace("'", "''"));
            res +="'";
            return res;
        }

        static string ConvertCharToSql(char value)
        {
            var stringBuilder = new StringBuilder();
            if (value == '\\')
            {
                stringBuilder.Append("\\\\");
            }
            else
            {
                stringBuilder.Append('\'');

                if (value == '\'') stringBuilder.Append("''");
                else stringBuilder.Append(value);

                stringBuilder.Append('\'');
            }
            return stringBuilder.ToString();
        }

        static string ConvertBinaryToSql( byte[] value)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("0x");

            stringBuilder.AppendByteArrayAsHexViaLookup32(value);
            return stringBuilder.ToString();
        }

        public override string DbDataTypeToSQL(DbDataType type)
        {
            switch (type.DataType) {
                case DataType.VarChar:
                case DataType.NVarChar:
                    return string.Format("VARCHAR({0})", type.Length);
                case DataType.DateTime:
                case DataType.DateTime2:
                    return "DATETIME";
                case DataType.Time:
                    return "TIME";
                case DataType.Date:
                    return "DATE";
                case DataType.Boolean:
                    return "bit(1)";
                case DataType.Decimal:
                case DataType.VarNumeric:
                    if (type.Precision == null || type.Precision == 0) {
                        type.Precision = 10;
                    }
                    return string.Format("DECIMAL({0},{1})", type.Precision, type.Scale);
                case DataType.Double:
                    if (type.Precision == null || type.Precision == 0)
                    {
                        return "DOUBLE";
                    }
                    else if (type.Scale == null)
                    {
                        return string.Format("DECIMAL({0})", type.Precision);
                    }
                    else { 
                        return string.Format("DECIMAL({0},{1})", type.Precision, type.Scale);
                    }
                    
                case DataType.Guid:
                    return "varchar(36)";
                case DataType.Char:
                    return string.Format("CHAR({0})", type.Length);
                case DataType.Int32:
                    return "INT";
                case DataType.Int64:
                case DataType.Long:
                    return "BIGINT";
                case DataType.Text:
                    return "TEXT";
            }
            return base.DbDataTypeToSQL(type);
        }

        public override DataType GetDataType(string? dataType, string? columnType, int? length, int? precision, int? scale)
        {
            return dataType?.ToLowerInvariant() switch
            {
                "tinyint unsigned" => DataType.Byte,
                "smallint unsigned" => DataType.UInt16,
                "mediumint unsigned" => DataType.UInt32,
                "int unsigned" => DataType.UInt32,
                "bigint unsigned" => DataType.UInt64,
                "bool" => DataType.SByte, // tinyint(1) alias
                "bit" => DataType.BitArray,
                "blob" => DataType.Blob,
                "tinyblob" => DataType.Blob,
                "mediumblob" => DataType.Blob,
                "longblob" => DataType.Blob,
                "binary" => DataType.Binary,
                "varbinary" => DataType.VarBinary,
                "date" => DataType.Date,
                "datetime" => DataType.DateTime,
                "timestamp" => DataType.DateTime,
                "time" => DataType.Time,
                "char" => DataType.Char,
                "varchar" => DataType.VarChar,
                "set" => DataType.VarChar,
                "enum" => DataType.VarChar,
                "tinytext" => DataType.Text,
                "text" => DataType.Text,
                "mediumtext" => DataType.Text,
                "longtext" => DataType.Text,
                "double" => DataType.Double,
                "float" => DataType.Single,
                "tinyint" => columnType != null && columnType.Contains("unsigned") ? DataType.Byte : DataType.SByte,
                "smallint" => columnType != null && columnType.Contains("unsigned") ? DataType.UInt16 : DataType.Int16,
                "int" => columnType != null && columnType.Contains("unsigned") ? DataType.UInt32 : DataType.Int32,
                "year" => DataType.Int32,
                "mediumint" => columnType != null && columnType.Contains("unsigned") ? DataType.UInt32 : DataType.Int32,
                "bigint" => columnType != null && columnType.Contains("unsigned") ? DataType.UInt64 : DataType.Int64,
                "decimal" => DataType.Decimal,
                "json" => DataType.Json,
                _ => DataType.Undefined,
            };
        }

    }
}
