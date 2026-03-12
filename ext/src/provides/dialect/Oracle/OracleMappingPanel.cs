using mooSQL.data.mapping;
using mooSQL.data.model;
using mooSQL.linq.Common;
using mooSQL.linq.Extensions;
using mooSQL.utils;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Linq;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.Oracle
{
    /// <summary>
    /// Oracle数据库映射面板类。
    /// </summary>
    public class OracleMappingPanel:DefaultMappingPanel
    {
        /// <summary>
        /// 初始化Oracle数据库映射面板类。
        /// </summary>
        public OracleMappingPanel() {

            this.SetDataType<Guid>(DataFam.Guid);
            this.SetDataType<string>(DataFam.VarChar);

            SetValueToSql<Guid>((v) => ConvertBinaryToSql(v.ToByteArray()));
            SetValueToSql<string>( ( v) => ConvertStringToSql(v));
            SetValueToSql<char>( ( v) => ConvertCharToSql(v));
            SetValueToSql<byte[]>(( v) => ConvertBinaryToSql(v));
            SetValueToSql<Binary>( (v) => ConvertBinaryToSql(v.ToArray()));

            // adds floating point special values support
            SetValueToSql<float>( ( v) =>
            {
                var sb = new StringBuilder();
                var f = (float)v;
                if (float.IsNaN(f))
                    sb.Append("BINARY_FLOAT_NAN");
                else if (float.IsNegativeInfinity(f))
                    sb.Append("-BINARY_FLOAT_INFINITY");
                else if (float.IsPositiveInfinity(f))
                    sb.Append("BINARY_FLOAT_INFINITY");
                else
                    sb.AppendFormat( "{0:G9}", f);
                return sb.ToString();
            });
            SetValueToSql<double>( (v) =>
            {
                var sb = new StringBuilder();
                var d = (double)v;
                if (double.IsNaN(d))
                    sb.Append("BINARY_DOUBLE_NAN");
                else if (double.IsNegativeInfinity(d))
                    sb.Append("-BINARY_DOUBLE_INFINITY");
                else if (double.IsPositiveInfinity(d))
                    sb.Append("BINARY_DOUBLE_INFINITY");
                else
                    sb.AppendFormat("{0:G17}D", d);
                return sb.ToString();
            });

            //对于一个c#类型可能对应多个数据库类型，比如int在数据库中可以是smallint, int, bigint等的情况，需要使用ValueWord类来处理
            SetValueToSql<ValueWord>((v) => ConvertDataTypeWordToSQL(v));

            SetValueConverter<decimal, TimeSpan>((v) => new TimeSpan((long)v));
        }

        public override Type ConvertParameterType(Type type, DbDataType dataType)
        {
            if (type.IsNullable())
                type = type.UnwrapNullable();

            switch (dataType.DataType)
            {
                case DataFam.DateTimeOffset: if (type == typeof(DateTimeOffset)) return typeof( OracleTimeStampTZ); break;
                case DataFam.Boolean: if (type == typeof(bool)) return typeof(byte); break;
                case DataFam.Guid: if (type == typeof(Guid)) return typeof(byte[]); break;
                case DataFam.Int16: if (type == typeof(bool)) return typeof(short); break;
            }

            return base.ConvertParameterType(type, dataType);
        }
        public override void SetParameter(DbCommand dataConnection, DbParameter parameter, string name, DbDataType dataType, object? value)
        {
            switch (dataType.DataType)
            {
                case DataFam.DateTimeOffset:
                    if (value is DateTimeOffset dto)
                    {
                        dto = dto.WithPrecision(dataType.Precision ?? 6);
                        var zone = (dto.Offset < TimeSpan.Zero ? "-" : "+") + dto.Offset.ToString("hh\\:mm", DateTimeFormatInfo.InvariantInfo);
                        value =new OracleTimeStampTZ(dto.UtcDateTime, zone);
                    }
                    break;

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
                    // https://github.com/linq2db/linq2db/issues/3207
                    if (value is Guid guid) value = guid.ToByteArray();
                    break;

                case DataFam.Time:
                    // According to http://docs.oracle.com/cd/E16655_01/win.121/e17732/featOraCommand.htm#ODPNT258
                    // Inference of DbType and OracleDbType from Value: TimeSpan - Object - IntervalDS
                    if (value is TimeSpan)
                        dataType = dataType.WithDataType(DataFam.Undefined);
                    break;

                case DataFam.BFile:
                    // TODO: BFile we do not support setting parameter value
                    value = null;
                    break;

                case DataFam.DateTime:
                    {
                        if (value is DateTime dt)
                            value = dt.WithPrecision(0);
                        break;
                    }

                case DataFam.DateTime2:
                    {
                        if (value is DateTime dt)
                            value = dt.WithPrecision(dataType.Precision ?? 6);
                        break;
                    }

#if NET6_0_OR_GREATER
				case DataFam.Date     :
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
                case DataFam.SByte: parameter.DbType = DbType.Int16; break;
                case DataFam.UInt16: parameter.DbType = DbType.Int32; break;
                case DataFam.UInt32: parameter.DbType = DbType.Int64; break;
                case DataFam.UInt64:
                case DataFam.VarNumeric: parameter.DbType = DbType.Decimal; break;
                case DataFam.SmallDateTime: parameter.DbType = DbType.Date; break;
                case DataFam.DateTime2: parameter.DbType = DbType.DateTime; break;
                case DataFam.Guid: parameter.DbType = DbType.Binary; break;
                case DataFam.VarChar: parameter.DbType = DbType.String; break;

                // fallback (probably)
                case DataFam.NVarChar:
                case DataFam.Text:
                case DataFam.NText: parameter.DbType = DbType.String; break;
                case DataFam.Long:
                case DataFam.LongRaw:
                case DataFam.Image:
                case DataFam.Binary:
                case DataFam.Cursor: parameter.DbType = DbType.Binary; break;
                case DataFam.BFile: parameter.DbType = DbType.Binary; break;
                case DataFam.Xml: parameter.DbType = DbType.String; break;

                default: base.SetParameterType(cmd, parameter, dataType); break;
            }
        }

        public override Type GetProviderSpecificType(string dataType)
        {
            switch (dataType)
            {
                //case "BFILE": return OracleBFileType.Name;
                //case "RAW":
                //case "LONG RAW": return OracleBinaryType.Name;
                //case "BLOB": return _provider.Adapter.OracleBlobType.Name;
                //case "CLOB": return _provider.Adapter.OracleClobType.Name;
                //case "DATE": return _provider.Adapter.OracleDateType.Name;
                //case "BINARY_DOUBLE":
                //case "BINARY_FLOAT":
                //case "NUMBER": return _provider.Adapter.OracleDecimalType.Name;
                //case "INTERVAL DAY TO SECOND": return _provider.Adapter.OracleIntervalDSType.Name;
                //case "INTERVAL YEAR TO MONTH": return _provider.Adapter.OracleIntervalYMType.Name;
                //case "NCHAR":
                //case "LONG":
                //case "ROWID":
                //case "CHAR": return OracleString OracleStringType.Name;
                //case "TIMESTAMP": return _provider.Adapter.OracleTimeStampType.Name;
                //case "TIMESTAMP WITH LOCAL TIME ZONE": return _provider.Adapter.OracleTimeStampLTZType?.Name ?? _provider.Adapter.OracleTimeStampType.Name;
                //case "TIMESTAMP WITH TIME ZONE": return _provider.Adapter.OracleTimeStampTZType?.Name ?? _provider.Adapter.OracleTimeStampType.Name;
                //case "XMLTYPE": return _provider.Adapter.OracleXmlTypeType.Name;
                //case "REF CURSOR": return _provider.Adapter.OracleRefCursorType.Name;
            }
            
            return base.GetProviderSpecificType(dataType);
        }

        public override DataFam GetDataType(string? dataType, string? columnType)
        {
            switch (dataType)
            {
                case "OBJECT": return DataFam.Variant;
                case "BFILE": return DataFam.VarBinary;
                case "BINARY_DOUBLE": return DataFam.Double;
                case "BINARY_FLOAT": return DataFam.Single;
                case "BINARY_INTEGER": return DataFam.Int32;
                case "BLOB": return DataFam.Blob;
                case "CHAR": return DataFam.Char;
                case "CLOB": return DataFam.Text;
                case "DATE": return DataFam.DateTime;
                case "FLOAT": return DataFam.Decimal;
                case "LONG": return DataFam.Long;
                case "LONG RAW": return DataFam.LongRaw;
                case "NCHAR": return DataFam.NChar;
                case "NCLOB": return DataFam.NText;
                case "NUMBER": return DataFam.Decimal;
                case "NVARCHAR2": return DataFam.NVarChar;
                case "RAW": return DataFam.Binary;
                case "VARCHAR2": return DataFam.VarChar;
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







        static string ConvertBinaryToSql( byte[] value)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder
                .Append("HEXTORAW('")
                .AppendByteArrayAsHexViaLookup32(value);

            stringBuilder.Append("')");
            return stringBuilder.ToString();
        }
        internal static string ConvertStringToSql(string value)
        {
            return DataTools.ConvertStringToSql( "||", null
                , (sb,v)=>sb.Append($"chr({v})"), value, null);
        }
        static string ConvertCharToSql(char value)
        {
            return DataTools.ConvertCharToSql( "'", (sb, v) => sb.Append($"chr({v})"), value);
        }

        static string ConvertDataTypeWordToSQL(ValueWord dt)
        {
            if (dt.Value is DateTime v)
            {
                return ConvertDateTimeToSql(dt.ValueType, v);
            }
            else if (dt.Value is DateTimeOffset dfov) { 
                return ConvertDateTimeToSql(dt.ValueType, dfov.UtcDateTime);
            }
#if NET6_0_OR_GREATER
            else if (dt.Value is DateOnly doV)
            {
                return ConvertDateOnlyToSql(dt.ValueType, doV);
            }
#endif
            else
            {
                throw new InvalidOperationException("尚未支持");
            }
        }

        static string ConvertDateTimeToSql( DbDataType dataType, DateTime value)
        {
            StringBuilder stringBuilder = new StringBuilder();
#if SUPPORTS_COMPOSITE_FORMAT
			CompositeFormat format;
#else
            string format;
#endif
            switch (dataType.DataType)
            {
                case DataFam.Date:
                    format = DATE_FORMAT;
                    break;
                case DataFam.DateTime2:
                    switch (dataType.Precision)
                    {
                        case 0: format = TIMESTAMP0_FORMAT; break;
                        case 1: format = TIMESTAMP1_FORMAT; break;
                        case 2: format = TIMESTAMP2_FORMAT; break;
                        case 3: format = TIMESTAMP3_FORMAT; break;
                        case 4: format = TIMESTAMP4_FORMAT; break;
                        case 5: format = TIMESTAMP5_FORMAT; break;
                        // .net types doesn't support more than 7 digits, so it doesn't make sense to generate 8/9
                        case >= 7: format = TIMESTAMP7_FORMAT; break;
                        default: format = TIMESTAMP6_FORMAT; break;
                    }
                    break;
                case DataFam.DateTimeOffset:
                    // just use UTC literal
                    value = value.ToUniversalTime();
                    switch (dataType.Precision)
                    {
                        case 0: format = TIMESTAMPTZ0_FORMAT; break;
                        case 1: format = TIMESTAMPTZ1_FORMAT; break;
                        case 2: format = TIMESTAMPTZ2_FORMAT; break;
                        case 3: format = TIMESTAMPTZ3_FORMAT; break;
                        case 4: format = TIMESTAMPTZ4_FORMAT; break;
                        case 5: format = TIMESTAMPTZ5_FORMAT; break;
                        // .net types doesn't support more than 7 digits, so it doesn't make sense to generate 8/9
                        case >= 7: format = TIMESTAMPTZ7_FORMAT; break;
                        default: format = TIMESTAMPTZ6_FORMAT; break;
                    }
                    break;
                case DataFam.DateTime:
                default:
                    format = DATETIME_FORMAT;
                    break;
            }

            stringBuilder.AppendFormat(CultureInfo.InvariantCulture, format, value);

            return stringBuilder.ToString();
        }

#if NET6_0_OR_GREATER
        static string ConvertDateOnlyToSql(DbDataType dataType, DateOnly value)
        {
            return string.Format( DATE_FORMAT, value);
        }
#endif








#if SUPPORTS_COMPOSITE_FORMAT
		private static readonly CompositeFormat DATE_FORMAT     = CompositeFormat.Parse("DATE '{0:yyyy-MM-dd}'");
		private static readonly CompositeFormat DATETIME_FORMAT = CompositeFormat.Parse("TO_DATE('{0:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')");

		private static readonly CompositeFormat TIMESTAMP0_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss}'");
		private static readonly CompositeFormat TIMESTAMP1_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.f}'");
		private static readonly CompositeFormat TIMESTAMP2_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ff}'");
		private static readonly CompositeFormat TIMESTAMP3_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fff}'");
		private static readonly CompositeFormat TIMESTAMP4_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ffff}'");
		private static readonly CompositeFormat TIMESTAMP5_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fffff}'");
		private static readonly CompositeFormat TIMESTAMP6_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ffffff}'");
		private static readonly CompositeFormat TIMESTAMP7_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fffffff}'");

		private static readonly CompositeFormat TIMESTAMPTZ0_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss} +00:00'");
		private static readonly CompositeFormat TIMESTAMPTZ1_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.f} +00:00'");
		private static readonly CompositeFormat TIMESTAMPTZ2_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ff} +00:00'");
		private static readonly CompositeFormat TIMESTAMPTZ3_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fff} +00:00'");
		private static readonly CompositeFormat TIMESTAMPTZ4_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ffff} +00:00'");
		private static readonly CompositeFormat TIMESTAMPTZ5_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fffff} +00:00'");
		private static readonly CompositeFormat TIMESTAMPTZ6_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ffffff} +00:00'");
		private static readonly CompositeFormat TIMESTAMPTZ7_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fffffff} +00:00'");
#else
        private const string DATE_FORMAT = "DATE '{0:yyyy-MM-dd}'";

        private const string DATETIME_FORMAT = "TO_DATE('{0:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')";

        private const string TIMESTAMP0_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss}'";
        private const string TIMESTAMP1_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.f}'";
        private const string TIMESTAMP2_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ff}'";
        private const string TIMESTAMP3_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fff}'";
        private const string TIMESTAMP4_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ffff}'";
        private const string TIMESTAMP5_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fffff}'";
        private const string TIMESTAMP6_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ffffff}'";
        private const string TIMESTAMP7_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fffffff}'";

        private const string TIMESTAMPTZ0_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss} +00:00'";
        private const string TIMESTAMPTZ1_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.f} +00:00'";
        private const string TIMESTAMPTZ2_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ff} +00:00'";
        private const string TIMESTAMPTZ3_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fff} +00:00'";
        private const string TIMESTAMPTZ4_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ffff} +00:00'";
        private const string TIMESTAMPTZ5_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fffff} +00:00'";
        private const string TIMESTAMPTZ6_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ffffff} +00:00'";
        private const string TIMESTAMPTZ7_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fffffff} +00:00'";
#endif
    }
}
