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

            this.SetDataType(typeof(string), DataFam.NVarChar);

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
                case DataFam.VarChar:
                case DataFam.NVarChar:
                    return string.Format("VARCHAR({0})", type.Length);
                case DataFam.DateTime:
                case DataFam.DateTime2:
                    return "DATETIME";
                case DataFam.Time:
                    return "TIME";
                case DataFam.Date:
                    return "DATE";
                case DataFam.Boolean:
                    return "bit(1)";
                case DataFam.Decimal:
                case DataFam.VarNumeric:
                    if (type.Precision == null || type.Precision == 0) {
                        type.Precision = 10;
                    }
                    return string.Format("DECIMAL({0},{1})", type.Precision, type.Scale);
                case DataFam.Double:
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
                    
                case DataFam.Guid:
                    return "varchar(36)";
                case DataFam.Char:
                    return string.Format("CHAR({0})", type.Length);
                case DataFam.Int32:
                    return "INT";
                case DataFam.Int64:
                case DataFam.Long:
                    return "BIGINT";
                case DataFam.Text:
                    return "TEXT";
            }
            return base.DbDataTypeToSQL(type);
        }

        public override DataFam GetDataType(string? dataType, string? columnType=null)
        {
            return dataType?.ToLowerInvariant() switch
            {
                "tinyint unsigned" => DataFam.Byte,
                "smallint unsigned" => DataFam.UInt16,
                "mediumint unsigned" => DataFam.UInt32,
                "int unsigned" => DataFam.UInt32,
                "bigint unsigned" => DataFam.UInt64,
                "bool" => DataFam.SByte, // tinyint(1) alias
                "bit" => DataFam.BitArray,
                "blob" => DataFam.Blob,
                "tinyblob" => DataFam.Blob,
                "mediumblob" => DataFam.Blob,
                "longblob" => DataFam.Blob,
                "binary" => DataFam.Binary,
                "varbinary" => DataFam.VarBinary,
                "date" => DataFam.Date,
                "datetime" => DataFam.DateTime,
                "timestamp" => DataFam.DateTime,
                "time" => DataFam.Time,
                "char" => DataFam.Char,
                "varchar" => DataFam.VarChar,
                "set" => DataFam.VarChar,
                "enum" => DataFam.VarChar,
                "tinytext" => DataFam.Text,
                "text" => DataFam.Text,
                "mediumtext" => DataFam.Text,
                "longtext" => DataFam.Text,
                "double" => DataFam.Double,
                "float" => DataFam.Single,
                "tinyint" => columnType != null && columnType.Contains("unsigned") ? DataFam.Byte : DataFam.SByte,
                "smallint" => columnType != null && columnType.Contains("unsigned") ? DataFam.UInt16 : DataFam.Int16,
                "int" => columnType != null && columnType.Contains("unsigned") ? DataFam.UInt32 : DataFam.Int32,
                "year" => DataFam.Int32,
                "mediumint" => columnType != null && columnType.Contains("unsigned") ? DataFam.UInt32 : DataFam.Int32,
                "bigint" => columnType != null && columnType.Contains("unsigned") ? DataFam.UInt64 : DataFam.Int64,
                "decimal" => DataFam.Decimal,
                "json" => DataFam.Json,
                _ => DataFam.Undefined,
            };
        }

    }
}
