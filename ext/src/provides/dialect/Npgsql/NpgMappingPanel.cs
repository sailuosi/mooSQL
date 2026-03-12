using mooSQL.data.mapping;
using mooSQL.data.model;
using mooSQL.linq.Common;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Linq;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.Npgsql
{
    /// <summary>
    /// Npgsql数据库的映射面板
    /// </summary>
    public class NpgMappingPanel: DefaultMappingPanel
    {

        public NpgMappingPanel() {
            initStringDbTypeMap();

            this.SetDataType<PhysicalAddress>(DataFam.Udt);
            SetDataType<TimeSpan>(DataFam.Interval);
            //SetDataType<ulong>(DataType.ul);

            initValueToSQL();

        }

        private readonly IDictionary<string, NpgsqlDbType> _npgsqlTypeMap = new Dictionary<string, NpgsqlDbType>();
        private void initStringDbTypeMap() {
            // not all types are supported now
            // numeric types
            mapType("smallint",NpgsqlDbType.Smallint);
            mapType("integer",NpgsqlDbType.Integer);
            mapType("bigint",NpgsqlDbType.Bigint);
            mapType("numeric",NpgsqlDbType.Numeric);
            mapType("real",NpgsqlDbType.Real);
            mapType("double precision",NpgsqlDbType.Double);
            // monetary types
            mapType("money",NpgsqlDbType.Money);
            // character types
            mapType("character",NpgsqlDbType.Char);
            mapType("character varying",NpgsqlDbType.Varchar);
            mapType("text",NpgsqlDbType.Text);
            mapType("name",NpgsqlDbType.Name);
            mapType("char",NpgsqlDbType.InternalChar);
            // binary types
            mapType("bytea",NpgsqlDbType.Bytea);
            // date/time types (reltime missing from enum)
            mapType("timestamp",NpgsqlDbType.Timestamp);
            mapType("timestamp with time zone",NpgsqlDbType.TimestampTz);
            mapType("date",NpgsqlDbType.Date);
            mapType("time",NpgsqlDbType.Time);
            mapType("time with time zone",NpgsqlDbType.TimeTz);
            mapType("interval",NpgsqlDbType.Interval);
            mapType("abstime",NpgsqlDbType.Abstime);
            // boolean type
            mapType("boolean",NpgsqlDbType.Boolean);
            // geometric types
            mapType("point",NpgsqlDbType.Point);
            mapType("line",NpgsqlDbType.Line);
            mapType("lseg",NpgsqlDbType.LSeg);
            mapType("box",NpgsqlDbType.Box);
            mapType("path",NpgsqlDbType.Path);
            mapType("polygon",NpgsqlDbType.Polygon);
            mapType("circle",NpgsqlDbType.Circle);
            // network address types
            mapType("cidr",NpgsqlDbType.Cidr);
            mapType("inet",NpgsqlDbType.Inet);
            mapType("macaddr",NpgsqlDbType.MacAddr);
            mapType("macaddr8",NpgsqlDbType.MacAddr8);
            // bit string types
            mapType("bit",NpgsqlDbType.Bit);
            mapType("bit varying",NpgsqlDbType.Varbit);
            // text search types
            mapType("tsvector",NpgsqlDbType.TsVector);
            mapType("tsquery",NpgsqlDbType.TsQuery);
            // UUID type
            mapType("uuid",NpgsqlDbType.Uuid);
            // XML type
            mapType("xml",NpgsqlDbType.Xml);
            // JSON types
            mapType("json",NpgsqlDbType.Json);
            mapType("jsonb",NpgsqlDbType.Jsonb);
            // Object Identifier Types (only supported by npgsql)
            mapType("oid",NpgsqlDbType.Oid);
            mapType("regtype",NpgsqlDbType.Regtype);
            mapType("xid",NpgsqlDbType.Xid);
            mapType("xid8",NpgsqlDbType.Xid);
            mapType("cid",NpgsqlDbType.Cid);
            mapType("tid",NpgsqlDbType.Tid);
            // other types
            mapType("citext",NpgsqlDbType.Citext);
            mapType("hstore",NpgsqlDbType.Hstore);
            mapType("refcursor",NpgsqlDbType.Refcursor);
            mapType("oidvector",NpgsqlDbType.Oidvector);
            mapType("int2vector",NpgsqlDbType.Int2Vector);
#if NETFRAMEWORK
#else
            // ranges
            mapType("int4range",NpgsqlDbType.IntegerRange);
            mapType("int8range",NpgsqlDbType.BigIntRange);
            mapType("numrange",NpgsqlDbType.NumericRange);
            mapType("tsrange",NpgsqlDbType.TimestampRange);
            mapType("tstzrange",NpgsqlDbType.TimestampTzRange);
            mapType("daterange",NpgsqlDbType.DateRange);
            // multi-ranges
            mapType("int4multirange",NpgsqlDbType.IntegerMultirange);
            mapType("int8multirange",NpgsqlDbType.BigIntMultirange);
            mapType("nummultirange",NpgsqlDbType.NumericMultirange);
            mapType("tsmultirange",NpgsqlDbType.TimestampMultirange);
            mapType("tstzmultirange",NpgsqlDbType.TimestampTzMultirange);
            mapType("datemultirange",NpgsqlDbType.DateMultirange);
#endif



            bool mapType(string dbType,NpgsqlDbType type)
            {

                _npgsqlTypeMap.Add(dbType, type);
                return true;
            }
        }

        private void initValueToSQL() {
            SetValueToSql<bool>((v) => v.ToString());
            SetValueToSql<string>((v) => ConvertStringToSql(v));
            SetValueToSql<char>((v) => ConvertCharToSql(v));
            SetValueToSql<byte[]>((v) => ConvertBinaryToSql(v));
            SetValueToSql<Binary>((v) => ConvertBinaryToSql(v.ToArray()));
            SetValueToSql<Guid>((v) => string.Format("'{0:D}'::uuid", v));
            //对于一个c#类型可能对应多个数据库类型，比如int在数据库中可以是smallint, int, bigint等的情况，需要使用ValueWord类来处理
            SetValueToSql<ValueWord>((v) => ConvertDataTypeWordToSQL(v));
            SetValueToSql<BigInteger>((v) => v.ToString());

            // 增加对浮点数的支持，用以处理NaN和Infinity值
            SetValueToSql<float>((v) =>
            {
                var sb = new StringBuilder();
                var f = (float)v;
                var quote = float.IsNaN(f) || float.IsInfinity(f);
                if (quote) sb.Append('\'');
                sb.AppendFormat("{0:G9}", f);
                if (quote) sb.Append("'::float4");
                return sb.ToString();
            });
            SetValueToSql<double>((v) =>
            {
                var sb = new StringBuilder();
                var d = (double)v;
                var quote = double.IsNaN(d) || double.IsInfinity(d);
                if (quote) sb.Append('\'');
                sb.AppendFormat("{0:G17}", d);
                if (quote) sb.Append("'::float8");
                return sb.ToString();
            });
        }
        public override Type? GetProviderSpecificType(string? dataType)
        {
            switch (dataType)
            {
                //case "timestamp":
                //case "timestamptz":
                //case "timestamp with time zone":
                //case "timestamp without time zone": return typeof(NpgsqlDbType.) NpgsqlDateTimeType?.Name;
                //case "date": return _provider.Adapter.NpgsqlDateType?.Name;
                //case "interval": return _provider.Adapter.NpgsqlIntervalType?.Name;
                //case "point": return _provider.Adapter.NpgsqlPointType.Name;
                //case "lseg": return _provider.Adapter.NpgsqlLSegType.Name;
                //case "box": return _provider.Adapter.NpgsqlBoxType.Name;
                //case "circle": return _provider.Adapter.NpgsqlCircleType.Name;
                //case "path": return _provider.Adapter.NpgsqlPathType.Name;
                //case "polygon": return _provider.Adapter.NpgsqlPolygonType.Name;
                //case "line": return _provider.Adapter.NpgsqlLineType.Name;
                //case "cidr": return (_provider.Adapter.NpgsqlCidrType ?? _provider.Adapter.NpgsqlInetType).Name;
                //case "inet": return _provider.Adapter.NpgsqlInetType.Name;
                //case "geometry": return "PostgisGeometry";
            }

            return base.GetProviderSpecificType(dataType);
        }

        protected override void SetParameterType(DbCommand dataConnection, DbParameter parameter, DbDataType dataType)
        {
            // didn't tried to detect and cleanup unnecessary type mappings, as npgsql develops rapidly and
            // it doesn't pay efforts to track changes for each version in this area
            NpgsqlDbType? type = null;
            switch (dataType.DataType)
            {
                case DataFam.Money: type = NpgsqlDbType.Money; break;
                case DataFam.Image:
                case DataFam.Binary:
                case DataFam.VarBinary: type = NpgsqlDbType.Bytea; break;
                case DataFam.Boolean: type = NpgsqlDbType.Boolean; break;
                case DataFam.Xml: type = NpgsqlDbType.Xml; break;
                case DataFam.Text:
                case DataFam.NText: type = NpgsqlDbType.Text; break;
                case DataFam.BitArray: type = NpgsqlDbType.Bit; break;
                case DataFam.Dictionary: type = NpgsqlDbType.Hstore; break;
                case DataFam.Json: type = NpgsqlDbType.Json; break;
                case DataFam.BinaryJson: type = NpgsqlDbType.Jsonb; break;
                case DataFam.Interval: type = NpgsqlDbType.Interval; break;
                case DataFam.Int64: type = NpgsqlDbType.Bigint; break;
                // address npgsql 6.0.0 mapping DateTime by default to timestamptz
                case DataFam.DateTime:
                case DataFam.DateTime2: type = NpgsqlDbType.Timestamp; break;
                // npgsql 6.0.0 changed some DbType <-> NpgsqlDbType mappings
                // while it doesn't look like having any impact on queries
                // it makes sense to hint more precise types when we know that npgsql use less precise type
                //
                // Npgsql default was: NpgsqlDbType.Text
                case DataFam.NChar:
                case DataFam.Char: type = NpgsqlDbType.Char; break;
                case DataFam.NVarChar:
                case DataFam.VarChar: type = NpgsqlDbType.Varchar; break;
            }

            if (!string.IsNullOrEmpty(dataType.DbType))
            {
                type = GetNativeType(dataType.DbType);
            }

            if (type != null  && parameter is NpgsqlParameter npara)
            {
                npara.NpgsqlDbType = type.Value;
                    return;
                
            }

            switch (dataType.DataType)
            {
                case DataFam.SByte: parameter.DbType = DbType.Int16; return;
                case DataFam.UInt16: parameter.DbType = DbType.Int32; return;
                case DataFam.UInt32: parameter.DbType = DbType.Int64; return;
                case DataFam.UInt64:
                case DataFam.VarNumeric: parameter.DbType = DbType.Decimal; return;
                case DataFam.DateTime2: parameter.DbType = DbType.DateTime; return;
                // fallback mappings
                case DataFam.Money: parameter.DbType = DbType.Currency; break;
                case DataFam.Xml: parameter.DbType = DbType.Xml; break;
                case DataFam.Text: parameter.DbType = DbType.AnsiString; break;
                case DataFam.NText: parameter.DbType = DbType.String; break;
                case DataFam.Image:
                case DataFam.Binary:
                case DataFam.VarBinary: parameter.DbType = DbType.Binary; break;
                // those types doesn't have fallback DbType
                case DataFam.BitArray: parameter.DbType = DbType.Binary; break;
                case DataFam.Dictionary: parameter.DbType = DbType.Object; break;
                case DataFam.Json: parameter.DbType = DbType.String; break;
                case DataFam.BinaryJson: parameter.DbType = DbType.String; break;
            }

            base.SetParameterType(dataConnection, parameter, dataType);
        }


        public override DataFam GetDataType(string? dataType, string? columnType=null)
        {
            if (dataType == null)
                return DataFam.Undefined;
            //dataType = SimplifyDataType(dataType);
            switch (dataType)
            {
                case "bpchar":
                case "character": return DataFam.NChar;
                case "text": return DataFam.Text;
                case "int2":
                case "smallint": return DataFam.Int16;
                case "oid":
                case "xid":
                case "int4":
                case "integer": return DataFam.Int32;
                case "int8":
                case "bigint": return DataFam.Int64;
                case "float4":
                case "real": return DataFam.Single;
                case "float8":
                case "double precision": return DataFam.Double;
                case "bytea": return DataFam.Binary;
                case "bool":
                case "boolean": return DataFam.Boolean;
                case "numeric": return DataFam.Decimal;
                case "money": return DataFam.Money;
                case "uuid": return DataFam.Guid;
                case "varchar":
                case "character varying": return DataFam.NVarChar;
                case "timestamptz":
                case "timestamp with time zone": return DataFam.DateTimeOffset;
                case "timestamp":
                case "timestamp without time zone": return DataFam.DateTime2;
                case "timetz":
                case "time with time zone":
                case "time":
                case "time without time zone": return DataFam.Time;
                case "interval": return DataFam.Interval;
                case "date": return DataFam.Date;
                case "xml": return DataFam.Xml;
                case "point":
                case "lseg":
                case "box":
                case "circle":
                case "path":
                case "line":
                case "polygon":
                case "inet":
                case "cidr":
                case "macaddr":
                case "macaddr8":
                case "ARRAY":
                case "anyarray":
                case "anyelement":
                case "USER-DEFINED": return DataFam.Udt;
                case "bit":
                case "bit varying":
                case "varbit": return DataFam.BitArray;
                case "hstore": return DataFam.Dictionary;
                case "json": return DataFam.Json;
                case "jsonb": return DataFam.BinaryJson;
            }

            return DataFam.Undefined;
        }


        #region 值转换为SQL
        private const string DATE_FORMAT = "'{0:yyyy-MM-dd}'::{1}";
        private const string TIMESTAMP0_FORMAT = "'{0:yyyy-MM-dd HH:mm:ss}'::{1}";
        private const string TIMESTAMP3_FORMAT = "'{0:yyyy-MM-dd HH:mm:ss.fff}'::{1}";

        static string ConvertDataTypeWordToSQL(ValueWord dt) { 
            if(dt.Value is DateTime v)
            {
                return BuildDateTime(dt.ValueType,v);
            }
#if NET6_0_OR_GREATER
			else if (dt.Value is DateOnly dov)
			{
				return BuildDate(dt.ValueType,dov);
			}
#endif
            else
            {
                throw new InvalidOperationException("尚未支持");
            }
        }

        static string BuildDateTime(DbDataType dt, DateTime value)
        {
            StringBuilder stringBuilder = new StringBuilder();
            string dbType;
#if SUPPORTS_COMPOSITE_FORMAT
			CompositeFormat format;
#else
            string format;
#endif

            if (value.Millisecond == 0)
            {
                if (value.Hour == 0 && value.Minute == 0 && value.Second == 0)
                {
                    format = DATE_FORMAT;
                    dbType = dt.DbType ?? "date";
                }
                else
                {
                    format = TIMESTAMP0_FORMAT;
                    dbType = dt.DbType ?? "timestamp";
                }
            }
            else
            {
                format = TIMESTAMP3_FORMAT;
                dbType = dt.DbType ?? "timestamp";
            }

            stringBuilder.AppendFormat( format, value, dbType);
            return stringBuilder.ToString();
        }

#if NET6_0_OR_GREATER
		static string BuildDate(DbDataType dt, DateOnly value)
		{
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_FORMAT, value, dt.DbType ?? "date");
            return stringBuilder.ToString();
		}
#endif

        static string ConvertBinaryToSql( byte[] value)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("E'\\\\x");

            stringBuilder.AppendByteArrayAsHexViaLookup32(value);

            stringBuilder.Append("'::bytea");
            return stringBuilder.ToString();
        }


        static string ConvertStringToSql(string value)
        {
            return DataTools.ConvertStringToSql( "||", null,
                (sb,v)=>sb.Append($"chr({v})"), value, null);
        }

        static string ConvertCharToSql( char value)
        {
            return DataTools.ConvertCharToSql( "'", (sb, v) => sb.Append($"chr({v})"), value);
        }
        #endregion


        internal NpgsqlDbType? GetNativeType(string? dbType, bool convertAlways = false)
        {
            if (string.IsNullOrWhiteSpace(dbType))
                return null;

            dbType = dbType!.ToLowerInvariant();

            // detect arrays
            var isArray = false;
            var idx = dbType.IndexOf("array");

            if (idx == -1)
                idx = dbType.IndexOf("[");

            if (idx != -1)
            {
                isArray = true;
                dbType = dbType.Substring(0, idx);
            }

            var isRange = false;
            var isMultiRange = false;

            dbType = dbType.Trim();

            // normalize synonyms and parameterized type names
            switch (dbType)
            {
                case "int4range":
                    dbType = "integer";
                    isRange = true;
                    break;
                case "int8range":
                    dbType = "bigint";
                    isRange = true;
                    break;
                case "numrange":
                    dbType = "numeric";
                    isRange = true;
                    break;
                case "tsrange":
                    dbType = "timestamp";
                    isRange = true;
                    break;
                case "tstzrange":
                    dbType = "timestamp with time zone";
                    isRange = true;
                    break;
                case "daterange":
                    dbType = "date";
                    isRange = true;
                    break;

                case "int4multirange":
                    dbType = "integer";
                    isMultiRange = true;
                    break;
                case "int8multirange":
                    dbType = "bigint";
                    isMultiRange = true;
                    break;
                case "nummultirange":
                    dbType = "numeric";
                    isMultiRange = true;
                    break;
                case "tsmultirange":
                    dbType = "timestamp";
                    isMultiRange = true;
                    break;
                case "tstzmultirange":
                    dbType = "timestamp with time zone";
                    isMultiRange = true;
                    break;
                case "datemultirange":
                    dbType = "date";
                    isMultiRange = true;
                    break;

                case "timestamptz":
                    dbType = "timestamp with time zone";
                    break;
                case "int2":
                case "smallserial":
                case "serial2":
                    dbType = "smallint";
                    break;
                case "int":
                case "int4":
                case "serial":
                case "serial4":
                    dbType = "integer";
                    break;
                case "int8":
                case "bigserial":
                case "serial8":
                    dbType = "bigint";
                    break;
                case "float":
                    dbType = "double precision";
                    break;
                case "varchar":
                    dbType = "character varying";
                    break;
                case "varbit":
                    dbType = "bit varying";
                    break;
            }

            if (dbType.StartsWith("float(") && dbType.EndsWith(")"))
            {
                if (int.TryParse(dbType.Substring("float(".Length, dbType.Length - "float(".Length - 1), NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out var precision))
                {
                    if (precision >= 1 && precision <= 24)
                        dbType = "real";
                    else if (precision >= 25 && precision <= 53)
                        dbType = "real";
                    // else bad type
                }
            }

            if (dbType.StartsWith("numeric(") || dbType.StartsWith("decimal"))
                dbType = "numeric";

            if (dbType.StartsWith("varchar(") || dbType.StartsWith("character varying("))
                dbType = "character varying";

            if (dbType.StartsWith("char(") || dbType.StartsWith("character("))
                dbType = "character";

            if (dbType.StartsWith("interval"))
                dbType = "interval";

            if (dbType.StartsWith("timestamp"))
                dbType = dbType.Contains("with time zone") ? "timestamp with time zone" : "timestamp";

            if (dbType.StartsWith("time(") || dbType.StartsWith("time "))
                dbType = dbType.Contains("with time zone") ? "time with time zone" : "time";

            if (dbType.StartsWith("bit("))
                dbType = "bit";

            if (dbType.StartsWith("bit varying("))
                dbType = "bit varying";

            if (_npgsqlTypeMap.TryGetValue(dbType, out var result))
            {
                // because NpgsqlDbType fields numeric values changed in npgsql4,
                // applying flag-like array/range bits is not straightforward process
                //result = Adapter.ApplyDbTypeFlags(result, isArray, isRange, isMultiRange, convertAlways);

                return result;
            }

            return null;
        }

    }
}
