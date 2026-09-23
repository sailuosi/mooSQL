using mooSQL.data.mapping;
using mooSQL.data.model;
using mooSQL.linq.utils;
using mooSQL.utils;
using System;
using System.Data;
using System.Data.Common;
using System.Data.Linq;
using System.Globalization;
using System.Text;

namespace mooSQL.data
{
    /// <summary>
    /// 达梦类型映射：Oracle 兼容字面量（HEXTORAW / TO_DATE / TIMESTAMP），不依赖 ODP.NET 专有类型。
    /// </summary>
    public class DMMappingPanel : DefaultMappingPanel
    {
        public DMMappingPanel()
        {
            SetDataType<Guid>(DataFam.Guid);
            SetDataType<string>(DataFam.VarChar);

            SetValueToSql<Guid>((v) => ConvertBinaryToSql(v.ToByteArray()));
            SetValueToSql<string>((v) => ConvertStringToSql(v));
            SetValueToSql<char>((v) => ConvertCharToSql(v));
            SetValueToSql<byte[]>((v) => ConvertBinaryToSql(v));
            SetValueToSql<Binary>((v) => ConvertBinaryToSql(v.ToArray()));
            SetValueToSql<ValueWord>((v) => ConvertDataTypeWordToSQL(v));
            SetValueConverter<decimal, TimeSpan>((v) => new TimeSpan((long)v));
        }

        public override Type ConvertParameterType(Type type, DbDataType dataType)
        {
            if (type.IsNullable())
                type = type.UnwrapNullable();

            switch (dataType.DataType)
            {
                case DataFam.Boolean when type == typeof(bool):
                    return typeof(byte);
                case DataFam.Guid when type == typeof(Guid):
                    return typeof(byte[]);
                case DataFam.Int16 when type == typeof(bool):
                    return typeof(short);
            }

            return base.ConvertParameterType(type, dataType);
        }

        public override void SetParameter(DbCommand dataConnection, DbParameter parameter, string name, DbDataType dataType, object? value)
        {
            switch (dataType.DataType)
            {
                case DataFam.Boolean:
                    dataType = dataType.WithDataType(DataFam.Byte);
                    if (value is bool boolValue)
                        value = boolValue ? (byte)1 : (byte)0;
                    break;
                case DataFam.Guid:
                case DataFam.Binary:
                case DataFam.VarBinary:
                case DataFam.Blob:
                case DataFam.Image:
                    if (value is Guid guid) value = guid.ToByteArray();
                    break;
                case DataFam.Time:
                    if (value is TimeSpan)
                        dataType = dataType.WithDataType(DataFam.Undefined);
                    break;
                case DataFam.DateTime:
                    if (value is DateTime dt)
                        value = dt.WithPrecision(0);
                    break;
                case DataFam.DateTime2:
                    if (value is DateTime dt2)
                        value = dt2.WithPrecision(dataType.Precision ?? 6);
                    break;
#if NET6_0_OR_GREATER
                case DataFam.Date:
                    if (value is DateOnly d)
                        value = d.ToDateTime(TimeOnly.MinValue);
                    break;
#endif
            }

            if (dataType.DataType == DataFam.Undefined && value is string @string && @string.Length >= 4000)
                dataType = dataType.WithDataType(DataFam.NText);

            base.SetParameter(dataConnection, parameter, name, dataType, value);
        }

        protected override void SetParameterType(DbCommand cmd, DbParameter parameter, DbDataType dataType)
        {
            switch (dataType.DataType)
            {
                case DataFam.Byte:
                case DataFam.SByte:
                    parameter.DbType = DbType.Int16;
                    break;
                case DataFam.UInt16:
                    parameter.DbType = DbType.Int32;
                    break;
                case DataFam.UInt32:
                    parameter.DbType = DbType.Int64;
                    break;
                case DataFam.UInt64:
                case DataFam.VarNumeric:
                    parameter.DbType = DbType.Decimal;
                    break;
                case DataFam.SmallDateTime:
                    parameter.DbType = DbType.Date;
                    break;
                case DataFam.DateTime2:
                    parameter.DbType = DbType.DateTime;
                    break;
                case DataFam.Guid:
                    parameter.DbType = DbType.Binary;
                    break;
                case DataFam.VarChar:
                case DataFam.NVarChar:
                case DataFam.Text:
                case DataFam.NText:
                case DataFam.Xml:
                    parameter.DbType = DbType.String;
                    break;
                case DataFam.Long:
                case DataFam.LongRaw:
                case DataFam.Image:
                case DataFam.Binary:
                case DataFam.Cursor:
                case DataFam.BFile:
                    parameter.DbType = DbType.Binary;
                    break;
                default:
                    base.SetParameterType(cmd, parameter, dataType);
                    break;
            }
        }

        public override DataFam GetDataType(string? dataType, string? columnType)
        {
            switch (dataType)
            {
                case "OBJECT": return DataFam.Variant;
                case "BFILE": return DataFam.VarBinary;
                case "BINARY_DOUBLE": return DataFam.Double;
                case "BINARY_FLOAT": return DataFam.Single;
                case "BINARY_INTEGER":
                case "INT":
                case "INTEGER": return DataFam.Int32;
                case "BIGINT": return DataFam.Int64;
                case "BLOB": return DataFam.Blob;
                case "CHAR": return DataFam.Char;
                case "CLOB":
                case "TEXT": return DataFam.Text;
                case "DATE": return DataFam.DateTime;
                case "FLOAT": return DataFam.Decimal;
                case "LONG": return DataFam.Long;
                case "LONG RAW": return DataFam.LongRaw;
                case "NCHAR": return DataFam.NChar;
                case "NCLOB": return DataFam.NText;
                case "NUMBER":
                case "DECIMAL":
                case "NUMERIC": return DataFam.Decimal;
                case "NVARCHAR2": return DataFam.NVarChar;
                case "RAW": return DataFam.Binary;
                case "VARCHAR2":
                case "VARCHAR": return DataFam.VarChar;
                case "XMLTYPE": return DataFam.Xml;
                case "ROWID": return DataFam.VarChar;
                case "REF CURSOR": return DataFam.Cursor;
                default:
                    if (dataType?.StartsWith("TIMESTAMP") == true)
                        return dataType.EndsWith("TIME ZONE") ? DataFam.DateTimeOffset : DataFam.DateTime2;
                    if (dataType?.StartsWith("INTERVAL DAY") == true)
                        return DataFam.Time;
                    if (dataType?.StartsWith("INTERVAL YEAR") == true)
                        return DataFam.Int64;
                    break;
            }

            return DataFam.Undefined;
        }

        public override string DbDataTypeToSQL(DbDataType type)
        {
            switch (type.DataType)
            {
                case DataFam.VarChar:
                case DataFam.NVarChar:
                    if (type.Length > 0)
                        return $"VARCHAR({type.Length})";
                    return "VARCHAR(4000)";
                case DataFam.Char:
                case DataFam.NChar:
                    if (type.Length > 0)
                        return $"CHAR({type.Length})";
                    return "CHAR(1)";
                case DataFam.Text:
                case DataFam.LongText:
                case DataFam.NText:
                    return "CLOB";
                case DataFam.Guid:
                    return "RAW(16)";
                case DataFam.DateTime:
                case DataFam.SmallDateTime:
                    return "DATE";
                case DataFam.DateTime2:
                case DataFam.DateTimeOffset:
                    return "TIMESTAMP";
                case DataFam.Date:
                    return "DATE";
                case DataFam.Time:
                    return "TIME";
                case DataFam.Boolean:
                    return "BIT";
                case DataFam.Byte:
                case DataFam.Int16:
                    return "SMALLINT";
                case DataFam.Int32:
                    return "INT";
                case DataFam.Int64:
                case DataFam.Long:
                    return "BIGINT";
                case DataFam.Decimal:
                case DataFam.VarNumeric:
                case DataFam.Money:
                    if (type.Precision > 0)
                        return $"NUMBER({type.Precision},{type.Scale})";
                    return "NUMBER";
                case DataFam.Double:
                    return "BINARY_DOUBLE";
                case DataFam.Single:
                    return "BINARY_FLOAT";
                case DataFam.Binary:
                case DataFam.VarBinary:
                case DataFam.Blob:
                case DataFam.Image:
                    return "BLOB";
            }

            return base.DbDataTypeToSQL(type);
        }

        static string ConvertBinaryToSql(byte[] value)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("HEXTORAW('").AppendByteArrayAsHexViaLookup32(value);
            stringBuilder.Append("')");
            return stringBuilder.ToString();
        }

        internal static string ConvertStringToSql(string value)
            => DataTools.ConvertStringToSql("||", null, (sb, v) => sb.Append($"chr({v})"), value, null);

        static string ConvertCharToSql(char value)
            => DataTools.ConvertCharToSql("'", (sb, v) => sb.Append($"chr({v})"), value);

        static string ConvertDataTypeWordToSQL(ValueWord dt)
        {
            if (dt.Value is DateTime v)
                return ConvertDateTimeToSql(dt.ValueType, v);
            if (dt.Value is DateTimeOffset dfov)
                return ConvertDateTimeToSql(dt.ValueType, dfov.UtcDateTime);
#if NET6_0_OR_GREATER
            if (dt.Value is DateOnly doV)
                return string.Format(DATE_FORMAT, doV);
#endif
            throw new InvalidOperationException("尚未支持");
        }

        static string ConvertDateTimeToSql(DbDataType dataType, DateTime value)
        {
            string format;
            switch (dataType.DataType)
            {
                case DataFam.Date:
                    format = DATE_FORMAT;
                    break;
                case DataFam.DateTime2:
                    format = TIMESTAMP6_FORMAT;
                    break;
                case DataFam.DateTimeOffset:
                    value = value.ToUniversalTime();
                    format = TIMESTAMPTZ6_FORMAT;
                    break;
                default:
                    format = DATETIME_FORMAT;
                    break;
            }

            return string.Format(CultureInfo.InvariantCulture, format, value);
        }

        private const string DATE_FORMAT = "DATE '{0:yyyy-MM-dd}'";
        private const string DATETIME_FORMAT = "TO_DATE('{0:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')";
        private const string TIMESTAMP6_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ffffff}'";
        private const string TIMESTAMPTZ6_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ffffff} +00:00'";
    }
}
