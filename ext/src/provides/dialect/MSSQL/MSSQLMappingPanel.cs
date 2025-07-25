using mooSQL.data.mapping;
using mooSQL.data.model;
using mooSQL.linq.Common;
using mooSQL.linq.DataProvider.SqlServer;
using mooSQL.linq.Extensions;
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.MSSQL
{
    public class MSSQLMappingPanel:DefaultMappingPanel
    {

        public MSSQLMappingPanel() { 
        
            InitDataTypeMap();


            SetValueToSql<byte[]>(( v) => ConvertBinaryToSql(v));
            SetValueToSql<Binary>( (v) => ConvertBinaryToSql(v.ToArray()));
            SetValueToSql<ValueWord>((v) => ConvertDataTypeWordToSQL(v));
        }





        public override DataFam GetDataType(string? dataType, string? columnType = null)
        {
            switch (dataType)
            {
                case "image": return DataFam.Image;
                case "text": return DataFam.Text;
                case "binary": return DataFam.Binary;
                case "tinyint": return DataFam.Byte;
                case "date": return DataFam.Date;
                case "time": return DataFam.Time;
                case "bit": return DataFam.Boolean;
                case "smallint": return DataFam.Int16;
                case "decimal": return DataFam.Decimal;
                case "int": return DataFam.Int32;
                case "smalldatetime": return DataFam.SmallDateTime;
                case "real": return DataFam.Single;
                case "money": return DataFam.Money;
                case "datetime": return DataFam.DateTime;
                case "float": return DataFam.Double;
                case "numeric": return DataFam.Decimal;
                case "smallmoney": return DataFam.SmallMoney;
                case "datetime2": return DataFam.DateTime2;
                case "bigint": return DataFam.Int64;
                case "varbinary": return DataFam.VarBinary;
                case "timestamp": return DataFam.Timestamp;
                case "sysname": return DataFam.NVarChar;
                case "nvarchar": return DataFam.NVarChar;
                case "varchar": return DataFam.VarChar;
                case "ntext": return DataFam.NText;
                case "uniqueidentifier": return DataFam.Guid;
                case "datetimeoffset": return DataFam.DateTimeOffset;
                case "sql_variant": return DataFam.Variant;
                case "xml": return DataFam.Xml;
                case "char": return DataFam.Char;
                case "nchar": return DataFam.NChar;
                case "hierarchyid":
                case "geography":
                case "geometry": return DataFam.Udt;
                case "table type": return DataFam.Structured;
            }

            return DataFam.Undefined;
        }

        public override Type? GetProviderSpecificType(string? dataType)
        {
            switch (dataType)
            {
                case "varbinary":
                case "timestamp":
                case "rowversion":
                case "image":
                case "binary": return typeof(SqlBinary);
                case "tinyint": return typeof(SqlByte);
                case "date":
                case "smalldatetime":
                case "datetime":
                case "datetime2": return typeof(SqlDateTime);
                case "bit": return typeof(SqlBoolean);
                case "smallint": return typeof(SqlInt16);
                case "numeric":
                case "decimal": return typeof(SqlDecimal);
                case "int": return typeof(SqlInt32);
                case "real": return typeof(SqlSingle);
                case "float": return typeof(SqlDouble);
                case "smallmoney":
                case "money": return typeof(SqlMoney);
                case "bigint": return typeof(SqlInt64);
                case "text":
                case "nvarchar":
                case "char":
                case "nchar":
                case "varchar":
                case "ntext": return typeof(SqlString);
                case "uniqueidentifier": return typeof(SqlGuid);
                case "xml": return typeof(SqlXml);
                //case "hierarchyid": return $"{SqlServerTypes.TypesNamespace}.{SqlServerTypes.SqlHierarchyIdType}";
                //case "geography": return $"{SqlServerTypes.TypesNamespace}.{SqlServerTypes.SqlGeographyType}";
                //case "geometry": return $"{SqlServerTypes.TypesNamespace}.{SqlServerTypes.SqlGeometryType}";
            }

            return base.GetProviderSpecificType(dataType);
        }





        private void InitDataTypeMap() {
            SetDefaultValue<SqlChars>( SqlChars.Null);
            SetNullable<SqlChars>(true);
            SetDataType<SqlBinary>(SqlBinary.Null, true, DataFam.VarBinary);
            SetDataType<SqlBoolean>(SqlBoolean.Null, true, DataFam.Boolean);
            SetDataType<SqlByte>(SqlByte.Null, true, DataFam.Byte);
            SetDataType<SqlDateTime>(SqlDateTime.Null, true, DataFam.DateTime);
            SetDataType<SqlDecimal>(SqlDecimal.Null, true, DataFam.Decimal);
            SetDataType<SqlDouble>(SqlDouble.Null, true, DataFam.Double);
            SetDataType<SqlGuid>(SqlGuid.Null, true, DataFam.Guid);
            SetDataType<SqlInt16>(SqlInt16.Null, true, DataFam.Int16);
            SetDataType<SqlInt32>(SqlInt32.Null, true, DataFam.Int32);
            SetDataType<SqlInt64>(SqlInt64.Null, true, DataFam.Int64);
            SetDataType<SqlMoney>(SqlMoney.Null, true, DataFam.Money);
            SetDataType<SqlSingle>(SqlSingle.Null, true, DataFam.Single);
            SetDataType<SqlString>(SqlString.Null, true, DataFam.NVarChar);
            SetDataType<SqlXml>( SqlXml.Null, true, DataFam.Xml);

            SetDataType<DateTime>(DataFam.DateTime2);
            SetDataType<string>(DataFam.NVarChar);
        }
        /// <summary>
        /// 字符串和char都需要结合字段类型进行转换
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        static string ConvertDataTypeWordToSQL(ValueWord dt)
        {

            //SetValueToSql(typeof(string), (sb, dt, _, v) => ConvertStringToSql(sb, dt.Type.DataType, (string)v));
            //SetValueToSql(typeof(char), (sb, dt, _, v) => ConvertCharToSql(sb, dt, (char)v));

            if (dt.Value is string strV)
            {
                return ConvertStringToSql(dt.ValueType, strV);
            }
            else if (dt.Value is char chV)
            {
                return ConvertCharToSql(dt.ValueType, chV);
            }
            else if (dt.Value is DateTime dtV) { 
                return ConvertDateTimeToSql(dt.ValueType, dtV, true, true);
            }
            else if (dt.Value is TimeSpan tsV) {
                return ConvertTimeSpanToSql(dt.ValueType, tsV, true, true);
            }
            else if (dt.Value is SqlDateTime sdsV) {
                return ConvertDateTimeToSql(dt.ValueType, sdsV.Value, true, true);
            }
            else if (dt.Value is DateTimeOffset dtosV) {
                return ConvertDateTimeOffsetToSql(dt.ValueType, dtosV, true, true);
            }
#if NET6_0_OR_GREATER
            else if (dt.Value is DateOnly doV) {
                return ConvertDateOnlyToSql(dt.ValueType, doV, true, true);
            }
#endif
            else
            {
                throw new InvalidOperationException("尚未支持");
            }
        }

        static string ConvertStringToSql( DbDataType dataType, string value)
        {
            string? startPrefix;

            switch (dataType.DataType)
            {
                case DataFam.Char:
                case DataFam.VarChar:
                case DataFam.Text:
                    startPrefix = null;
                    break;
                default:
                    startPrefix = "N";
                    break;
            }

            return DataTools.ConvertStringToSql( "+", startPrefix
                , (sb,v)=>sb.Append( $"char({v})"), value, null);
        }
        static string ConvertCharToSql(DbDataType sqlDataType, char value)
        {
            string start;

            switch (sqlDataType.DataType)
            {
                case DataFam.Char:
                case DataFam.VarChar:
                case DataFam.Text:
                    start = "'";
                    break;
                default:
                    start = "N'";
                    break;
            }

            return DataTools.ConvertCharToSql( start, (sb, v) => sb.Append($"char({v})"), value);
        }

        static string ConvertDateTimeToSql(DbDataType dt, DateTime value, bool v2008plus, bool supportsFromParts)
        {
            StringBuilder stringBuilder = new();
            switch (dt.DataType, v2008plus, supportsFromParts)
            {
                case (DataFam.Text, _, _) or (DataFam.Char, _, _) or (DataFam.VarChar, _, _)
                    when value.Hour == 0 && value.Minute == 0 && value.Second == 0 && value.Millisecond == 0:
                    stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_FORMAT, value);
                    break;
                case (DataFam.Text, _, _) or (DataFam.Char, _, _) or (DataFam.VarChar, _, _):
                    stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATETIME_FORMAT, value);
                    break;

                case (DataFam.NText, _, _) or (DataFam.NChar, _, _) or (DataFam.NVarChar, _, _)
                    when value.Hour == 0 && value.Minute == 0 && value.Second == 0 && value.Millisecond == 0:
                    stringBuilder.Append('N');
                    stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_FORMAT, value);
                    break;
                case (DataFam.NText, _, _) or (DataFam.NChar, _, _) or (DataFam.NVarChar, _, _):
                    stringBuilder.Append('N');
                    stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATETIME_FORMAT, value);
                    break;

                case (DataFam.SmallDateTime, _, _):
                    // don't use SMALLDATETIMEFROMPARTS as it doesn't accept seconds/milliseconds, which used for rounding
                    stringBuilder.AppendFormat(CultureInfo.InvariantCulture, SMALLDATETIME_TYPED_FORMAT, value);
                    break;

                case (DataFam.Date, true, true):
                    // DATEFROMPARTS ( year, month, day )
                    stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_FROMPARTS_FORMAT, value.Year, value.Month, value.Day);
                    break;
                case (DataFam.Date, true, false):
                    stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_TYPED_FORMAT, value);
                    break;
                case (DataFam.Date, false, _):
                    stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_AS_DATETIME_TYPED_FORMAT, value);
                    break;

                case (DataFam.DateTime2, true, true):
                    {
                        var precision = dt.Precision ?? 7;
                        if (precision < 0 || precision > 7)
                            throw new InvalidOperationException($"DATETIME2 type precision is out-of-bounds: {precision}");

                        // DATETIME2FROMPARTS ( year, month, day, hour, minute, seconds, fractions, precision )
                        stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DaTETIME2_FROMPARTS_FORMAT, value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, GetFractionalSecondFromTicks(value.Ticks, precision), precision);
                        break;
                    }
                case (DataFam.DateTime2, true, false):
                    {
                        var precision = dt.Precision ?? 7;
                        if (precision < 0 || precision > 7)
                            throw new InvalidOperationException($"DATETIME2 type precision is out-of-bounds: {precision}");

                        stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATETIME2_TYPED_FORMATS[precision], value);
                        break;
                    }
                case (DataFam.DateTime2, false, _):
                    {
                        var precision = dt.Precision ?? 7;
                        if (precision < 0 || precision > 7)
                            throw new InvalidOperationException($"DATETIME2 type precision is out-of-bounds: {precision}");

                        stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATETIME_WITH_PRECISION_FORMATS[precision], value);
                        break;
                    }

                default:
                    // default: DATETIME
                    if (supportsFromParts)
                        // DATETIMEFROMPARTS ( year, month, day, hour, minute, seconds, milliseconds )
                        stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATETIME_FROMPARTS_FORMAT, value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Millisecond);
                    else
                        stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATETIME_TYPED_FORMAT, value);
                    break;
            }
            return stringBuilder.ToString();
        }

        static string ConvertTimeSpanToSql(DbDataType sqlDataType, TimeSpan value, bool supportsTime, bool supportsFromParts)
        {
            StringBuilder stringBuilder = new StringBuilder();
            switch (sqlDataType.DataType, supportsTime, supportsFromParts)
            {
                case (DataFam.Int64, _, _):
                    {
                        var precision = sqlDataType.Precision ?? 7;
                        if (precision < 0 || precision > 7)
                            throw new InvalidOperationException($"TIME type precision is out-of-bounds: {precision}");

                        var ticks = value.Ticks - (value.Ticks % ValueExtensions.TICKS_DIVIDERS[precision]);

                        stringBuilder.AppendFormat(CultureInfo.InvariantCulture, TIME_TICKS_FORMAT, ticks);
                        break;
                    }
                case (DataFam.Text, _, _) or (DataFam.Char, _, _) or (DataFam.VarChar, _, _):
                    {
                        var precision = sqlDataType.Precision ?? 7;
                        if (precision < 0 || precision > 7)
                            throw new InvalidOperationException($"TIME type precision is out-of-bounds: {precision}");

                        var ticks = value.Ticks - (value.Ticks % ValueExtensions.TICKS_DIVIDERS[precision]);

                        stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "'{0:c}'", TimeSpan.FromTicks(ticks));
                        break;
                    }
                case (DataFam.NText, _, _) or (DataFam.NChar, _, _) or (DataFam.NVarChar, _, _) or (_, false, _):
                    {
                        var precision = sqlDataType.Precision ?? 7;
                        if (precision < 0 || precision > 7)
                            throw new InvalidOperationException($"TIME type precision is out-of-bounds: {precision}");

                        var ticks = value.Ticks - (value.Ticks % ValueExtensions.TICKS_DIVIDERS[precision]);

                        stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "N'{0:c}'", TimeSpan.FromTicks(ticks));
                        break;
                    }
                default:
                    {
                        if (value < TimeSpan.Zero || value >= TimeSpan.FromDays(1))
                            throw new InvalidOperationException($"TIME value is out-of-bounds: {value:c}");

                        var precision = sqlDataType.Precision ?? 7;

                        if (precision < 0 || precision > 7)
                            throw new InvalidOperationException($"TIME type precision is out-of-bounds: {precision}");

                        if (supportsFromParts)
                            // TIMEFROMPARTS ( hour, minute, seconds, fractions, precision )
                            stringBuilder.AppendFormat(CultureInfo.InvariantCulture, TIME_FROMPARTS_FORMAT, value.Hours, value.Minutes, value.Seconds, GetFractionalSecondFromTicks(value.Ticks, precision), precision);
                        else
                            stringBuilder.AppendFormat(CultureInfo.InvariantCulture, TIME_TYPED_FORMATS[precision], value);
                        break;
                    }
            }
            return stringBuilder.ToString();
        }

        static string ConvertDateTimeOffsetToSql(DbDataType sqlDataType, DateTimeOffset value, bool v2008plus, bool supportsFromParts)
        {
            StringBuilder stringBuilder = new StringBuilder();
            switch (sqlDataType.DataType, v2008plus, supportsFromParts)
            {
                case (DataFam.Text, _, _) or (DataFam.Char, _, _) or (DataFam.VarChar, _, _):
                    {
                        var precision = sqlDataType.Precision ?? 7;
                        if (precision < 0 || precision > 7)
                            throw new InvalidOperationException($"DATETIMEOFFSET type precision is out-of-bounds: {precision}");

                        stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATETIMEOFFSET_FORMATS[precision], value);
                        break;
                    }
                case (DataFam.NText, _, _) or (DataFam.NChar, _, _) or (DataFam.NVarChar, _, _):
                    {
                        var precision = sqlDataType.Precision ?? 7;
                        if (precision < 0 || precision > 7)
                            throw new InvalidOperationException($"DATETIMEOFFSET type precision is out-of-bounds: {precision}");

                        stringBuilder.Append('N');
                        stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATETIMEOFFSET_FORMATS[precision], value);
                        break;
                    }

                case (DataFam.Date, _, _) or (DataFam.DateTime, _, _) or (DataFam.DateTime2, _, _) or (DataFam.SmallDateTime, _, _):
                    return ConvertDateTimeToSql( sqlDataType, value.LocalDateTime, v2008plus, supportsFromParts);
                    

                case (_, false, _):
                    {
                        var precision = sqlDataType.Precision ?? 7;
                        if (precision < 0 || precision > 7)
                            throw new InvalidOperationException($"DATETIMEOFFSET type precision is out-of-bounds: {precision}");

                        stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATETIMEOFFSET_AS_DATETIME_TYPED_FORMATS[precision], value.LocalDateTime);
                        break;
                    }

                default:
                    {
                        var precision = sqlDataType.Precision ?? 7;
                        if (precision < 0 || precision > 7)
                            throw new InvalidOperationException($"DATETIMEOFFSET type precision is out-of-bounds: {precision}");

                        if (supportsFromParts)
                            // DATETIMEOFFSETFROMPARTS ( year, month, day, hour, minute, seconds, fractions, hour_offset, minute_offset, precision )
                            stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DaTETIMEOFFSET_FROMPARTS_FORMAT, value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, GetFractionalSecondFromTicks(value.Ticks, precision), value.Offset.Hours, value.Offset.Minutes, precision);
                        else
                            stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATETIMEOFFSET_TYPED_FORMATS[precision], value);
                        break;
                    }
            }
            return stringBuilder.ToString();
        }

#if NET6_0_OR_GREATER
        static string ConvertDateOnlyToSql(DbDataType sqlDataType, DateOnly value, bool v2008plus, bool supportsFromParts)
        {
            StringBuilder stringBuilder = new StringBuilder();
            switch (sqlDataType.DataType, v2008plus, supportsFromParts)
            {
                case (DataFam.NText, _, _) or (DataFam.NChar, _, _) or (DataFam.NVarChar, _, _):
                    stringBuilder.Append('N');
                    stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_FORMAT, value);
                    break;
                case (DataFam.Text, _, _) or (DataFam.Char, _, _) or (DataFam.VarChar, _, _):
                    stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_FORMAT, value);
                    break;

                case (_, false, _):
                    return ConvertDateTimeToSql( sqlDataType, value.ToDateTime(default), v2008plus, supportsFromParts);
                    

                default:
                    {
                        if (supportsFromParts)
                            // DATEFROMPARTS ( year, month, day )
                            stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_FROMPARTS_FORMAT, value.Year, value.Month, value.Day);
                        else
                            stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_TYPED_FORMAT, value);
                        break;
                    }
            }
            return stringBuilder.ToString();
        }
#endif


        private static long GetFractionalSecondFromTicks(long ticks, int precision) => (ticks % ValueExtensions.TICKS_DIVIDERS[0]) / ValueExtensions.TICKS_DIVIDERS[precision];
#if SUPPORTS_COMPOSITE_FORMAT
		// TIME(p)
		private static readonly CompositeFormat TIME_TICKS_FORMAT     = CompositeFormat.Parse("CAST({0} AS BIGINT)");
		private static readonly CompositeFormat TIME_FROMPARTS_FORMAT = CompositeFormat.Parse("TIMEFROMPARTS({0}, {1}, {2}, {3}, {4})");
		private static readonly CompositeFormat[] TIME_TYPED_FORMATS  = new[]
		{
			CompositeFormat.Parse("CAST('{0:hh\\:mm\\:ss}' AS TIME(0))"),
			CompositeFormat.Parse("CAST('{0:hh\\:mm\\:ss\\.f}' AS TIME(1))"),
			CompositeFormat.Parse("CAST('{0:hh\\:mm\\:ss\\.ff}' AS TIME(2))"),
			CompositeFormat.Parse("CAST('{0:hh\\:mm\\:ss\\.fff}' AS TIME(3))"),
			CompositeFormat.Parse("CAST('{0:hh\\:mm\\:ss\\.ffff}' AS TIME(4))"),
			CompositeFormat.Parse("CAST('{0:hh\\:mm\\:ss\\.fffff}' AS TIME(5))"),
			CompositeFormat.Parse("CAST('{0:hh\\:mm\\:ss\\.ffffff}' AS TIME(6))"),
			CompositeFormat.Parse("CAST('{0:hh\\:mm\\:ss\\.fffffff}' AS TIME)")
		};

		// DATE
		private static readonly CompositeFormat DATE_FROMPARTS_FORMAT             = CompositeFormat.Parse("DATEFROMPARTS({0}, {1}, {2})");
		private static readonly CompositeFormat DATE_FORMAT                       = CompositeFormat.Parse("'{0:yyyy-MM-dd}'");
		private static readonly CompositeFormat DATE_TYPED_FORMAT                 = CompositeFormat.Parse("CAST('{0:yyyy-MM-dd}' AS DATE)");
		private static readonly CompositeFormat DATE_AS_DATETIME_TYPED_FORMAT     = CompositeFormat.Parse("CAST('{0:yyyy-MM-dd}' AS DATETIME)");
		// SMALLDATETIME
		private static readonly CompositeFormat SMALLDATETIME_TYPED_FORMAT        = CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS SMALLDATETIME)");
		// DATETIME
		private static readonly CompositeFormat DATETIME_FROMPARTS_FORMAT         = CompositeFormat.Parse("DATETIMEFROMPARTS({0}, {1}, {2}, {3}, {4}, {5}, {6})");
		// precision=3 to match SqlClient behavior for parameters
		// alternative option will be to generate parameter value explicitly
		private static readonly CompositeFormat DATETIME_FORMAT                   = CompositeFormat.Parse("'{0:yyyy-MM-ddTHH:mm:ss.fff}'");
		private static readonly CompositeFormat DATETIME_TYPED_FORMAT             = CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)");
		private static readonly CompositeFormat[] DATETIME_WITH_PRECISION_FORMATS = new[]
		{
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss}' AS DATETIME)"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.f}' AS DATETIME)"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.ff}' AS DATETIME)"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)")
		};
		// DATETIME2(p)
		private static readonly CompositeFormat DaTETIME2_FROMPARTS_FORMAT      = CompositeFormat.Parse("DATETIME2FROMPARTS({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7})");
		private static readonly CompositeFormat[] DATETIME2_TYPED_FORMATS       = new[]
		{
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss}' AS DATETIME2(0))"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.f}' AS DATETIME2(1))"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.ff}' AS DATETIME2(2))"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME2(3))"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.ffff}' AS DATETIME2(4))"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fffff}' AS DATETIME2(5))"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.ffffff}' AS DATETIME2(6))"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fffffff}' AS DATETIME2)")
		};
		// DATETIMEOFFSET(p)
		private static readonly CompositeFormat DaTETIMEOFFSET_FROMPARTS_FORMAT = CompositeFormat.Parse("DATETIMEOFFSETFROMPARTS({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9})");
		private static readonly CompositeFormat[] DATETIMEOFFSET_FORMATS        = new[]
		{
			CompositeFormat.Parse("'{0:yyyy-MM-ddTHH:mm:sszzz}'"),
			CompositeFormat.Parse("'{0:yyyy-MM-ddTHH:mm:ss.fzzz}'"),
			CompositeFormat.Parse("'{0:yyyy-MM-ddTHH:mm:ss.ffzzz}'"),
			CompositeFormat.Parse("'{0:yyyy-MM-ddTHH:mm:ss.fffzzz}'"),
			CompositeFormat.Parse("'{0:yyyy-MM-ddTHH:mm:ss.ffffzzz}'"),
			CompositeFormat.Parse("'{0:yyyy-MM-ddTHH:mm:ss.fffffzzz}'"),
			CompositeFormat.Parse("'{0:yyyy-MM-ddTHH:mm:ss.ffffffzzz}'"),
			CompositeFormat.Parse("'{0:yyyy-MM-ddTHH:mm:ss.fffffffzzz}'")
		};

		private static readonly CompositeFormat[] DATETIMEOFFSET_TYPED_FORMATS = new[]
		{
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:sszzz}' AS DATETIMEOFFSET(0))"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fzzz}' AS DATETIMEOFFSET(1))"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.ffzzz}' AS DATETIMEOFFSET(2))"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fffzzz}' AS DATETIMEOFFSET(3))"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.ffffzzz}' AS DATETIMEOFFSET(4))"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fffffzzz}' AS DATETIMEOFFSET(5))"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.ffffffzzz}' AS DATETIMEOFFSET(6))"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fffffffzzz}' AS DATETIMEOFFSET)")
		};
		private static readonly CompositeFormat[] DATETIMEOFFSET_AS_DATETIME_TYPED_FORMATS = new[]
		{
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss}' AS DATETIME)"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.f}' AS DATETIME)"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.ff}' AS DATETIME)"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)"),
		};
#else
        // TIME(p)
        private const string TIME_TICKS_FORMAT = "CAST({0} AS BIGINT)";
        private const string TIME_FROMPARTS_FORMAT = "TIMEFROMPARTS({0}, {1}, {2}, {3}, {4})";
        private static readonly string[] TIME_TYPED_FORMATS = new[]
        {
            "CAST('{0:hh\\:mm\\:ss}' AS TIME(0))",
            "CAST('{0:hh\\:mm\\:ss\\.f}' AS TIME(1))",
            "CAST('{0:hh\\:mm\\:ss\\.ff}' AS TIME(2))",
            "CAST('{0:hh\\:mm\\:ss\\.fff}' AS TIME(3))",
            "CAST('{0:hh\\:mm\\:ss\\.ffff}' AS TIME(4))",
            "CAST('{0:hh\\:mm\\:ss\\.fffff}' AS TIME(5))",
            "CAST('{0:hh\\:mm\\:ss\\.ffffff}' AS TIME(6))",
            "CAST('{0:hh\\:mm\\:ss\\.fffffff}' AS TIME)"
        };

        // DATE
        private const string DATE_FROMPARTS_FORMAT = "DATEFROMPARTS({0}, {1}, {2})";
        private const string DATE_FORMAT = "'{0:yyyy-MM-dd}'";
        private const string DATE_TYPED_FORMAT = "CAST('{0:yyyy-MM-dd}' AS DATE)";
        private const string DATE_AS_DATETIME_TYPED_FORMAT = "CAST('{0:yyyy-MM-dd}' AS DATETIME)";
        // SMALLDATETIME
        private const string SMALLDATETIME_TYPED_FORMAT = "CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS SMALLDATETIME)";
        // DATETIME
        private const string DATETIME_FROMPARTS_FORMAT = "DATETIMEFROMPARTS({0}, {1}, {2}, {3}, {4}, {5}, {6})";
        // precision=3 to match SqlClient behavior for parameters
        // alternative option will be to generate parameter value explicitly
        private const string DATETIME_FORMAT = "'{0:yyyy-MM-ddTHH:mm:ss.fff}'";
        private const string DATETIME_TYPED_FORMAT = "CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)";
        private static readonly string[] DATETIME_WITH_PRECISION_FORMATS = new[]
        {
            "CAST('{0:yyyy-MM-ddTHH:mm:ss}' AS DATETIME)",
            "CAST('{0:yyyy-MM-ddTHH:mm:ss.f}' AS DATETIME)",
            "CAST('{0:yyyy-MM-ddTHH:mm:ss.ff}' AS DATETIME)",
            "CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)",
            "CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)",
            "CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)",
            "CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)",
            "CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)"
        };
        // DATETIME2(p)
        private const string DaTETIME2_FROMPARTS_FORMAT = "DATETIME2FROMPARTS({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7})";
        private static readonly string[] DATETIME2_TYPED_FORMATS = new[]
        {
            "CAST('{0:yyyy-MM-ddTHH:mm:ss}' AS DATETIME2(0))",
            "CAST('{0:yyyy-MM-ddTHH:mm:ss.f}' AS DATETIME2(1))",
            "CAST('{0:yyyy-MM-ddTHH:mm:ss.ff}' AS DATETIME2(2))",
            "CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME2(3))",
            "CAST('{0:yyyy-MM-ddTHH:mm:ss.ffff}' AS DATETIME2(4))",
            "CAST('{0:yyyy-MM-ddTHH:mm:ss.fffff}' AS DATETIME2(5))",
            "CAST('{0:yyyy-MM-ddTHH:mm:ss.ffffff}' AS DATETIME2(6))",
            "CAST('{0:yyyy-MM-ddTHH:mm:ss.fffffff}' AS DATETIME2)"
        };
        // DATETIMEOFFSET(p)
        private const string DaTETIMEOFFSET_FROMPARTS_FORMAT = "DATETIMEOFFSETFROMPARTS({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9})";
        private static readonly string[] DATETIMEOFFSET_FORMATS = new[]
        {
            "'{0:yyyy-MM-ddTHH:mm:sszzz}'",
            "'{0:yyyy-MM-ddTHH:mm:ss.fzzz}'",
            "'{0:yyyy-MM-ddTHH:mm:ss.ffzzz}'",
            "'{0:yyyy-MM-ddTHH:mm:ss.fffzzz}'",
            "'{0:yyyy-MM-ddTHH:mm:ss.ffffzzz}'",
            "'{0:yyyy-MM-ddTHH:mm:ss.fffffzzz}'",
            "'{0:yyyy-MM-ddTHH:mm:ss.ffffffzzz}'",
            "'{0:yyyy-MM-ddTHH:mm:ss.fffffffzzz}'",
        };

        private static readonly string[] DATETIMEOFFSET_TYPED_FORMATS = new[]
        {
            "CAST('{0:yyyy-MM-ddTHH:mm:sszzz}' AS DATETIMEOFFSET(0))",
            "CAST('{0:yyyy-MM-ddTHH:mm:ss.fzzz}' AS DATETIMEOFFSET(1))",
            "CAST('{0:yyyy-MM-ddTHH:mm:ss.ffzzz}' AS DATETIMEOFFSET(2))",
            "CAST('{0:yyyy-MM-ddTHH:mm:ss.fffzzz}' AS DATETIMEOFFSET(3))",
            "CAST('{0:yyyy-MM-ddTHH:mm:ss.ffffzzz}' AS DATETIMEOFFSET(4))",
            "CAST('{0:yyyy-MM-ddTHH:mm:ss.fffffzzz}' AS DATETIMEOFFSET(5))",
            "CAST('{0:yyyy-MM-ddTHH:mm:ss.ffffffzzz}' AS DATETIMEOFFSET(6))",
            "CAST('{0:yyyy-MM-ddTHH:mm:ss.fffffffzzz}' AS DATETIMEOFFSET)"
        };
        private static readonly string[] DATETIMEOFFSET_AS_DATETIME_TYPED_FORMATS = new[]
        {
            "CAST('{0:yyyy-MM-ddTHH:mm:ss}' AS DATETIME)",
            "CAST('{0:yyyy-MM-ddTHH:mm:ss.f}' AS DATETIME)",
            "CAST('{0:yyyy-MM-ddTHH:mm:ss.ff}' AS DATETIME)",
            "CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)",
            "CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)",
            "CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)",
            "CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)",
            "CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)",
        };
#endif

        private static readonly string[] TIME_RAW_FORMATS = new[]
        {
            "hh\\:mm\\:ss",
            "hh\\:mm\\:ss\\.f",
            "hh\\:mm\\:ss\\.ff",
            "hh\\:mm\\:ss\\.fff",
            "hh\\:mm\\:ss\\.ffff",
            "hh\\:mm\\:ss\\.fffff",
            "hh\\:mm\\:ss\\.ffffff",
            "hh\\:mm\\:ss\\.fffffff"
        };
        private static readonly string[] DATETIMEOFFSET_RAW_FORMATS = new[]
        {
            "yyyy-MM-ddTHH:mm:sszzz",
            "yyyy-MM-ddTHH:mm:ss.fzzz",
            "yyyy-MM-ddTHH:mm:ss.ffzzz",
            "yyyy-MM-ddTHH:mm:ss.fffzzz",
            "yyyy-MM-ddTHH:mm:ss.ffffzzz",
            "yyyy-MM-ddTHH:mm:ss.fffffzzz",
            "yyyy-MM-ddTHH:mm:ss.ffffffzzz",
            "yyyy-MM-ddTHH:mm:ss.fffffffzzz",
        };

        static string ConvertBinaryToSql( byte[] value)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("0x");
            stringBuilder.AppendByteArrayAsHexViaLookup32(value);
            return stringBuilder.ToString();
        }
    }
}
