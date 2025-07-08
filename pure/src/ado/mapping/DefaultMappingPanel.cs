using mooSQL.data.model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace mooSQL.data.mapping
{
    /// <summary>
    /// 默认的映射面板，用于存放默认的映射规则。
    /// </summary>
    public class DefaultMappingPanel:MappingPanel
    {
        /// <summary>
        /// 初始化默认的映射面板。
        /// </summary>
        public DefaultMappingPanel() { 
            this.Init();
        }
        private void Init()
        {
            
            this.InitShartoDataType();

            this.InitValueConvert();

            this.InitScalarType();
        }

        private void InitShartoDataType()
        {
            var tar = this.SharpToDataType;
            this.SetDataType<string>(DataType.NVarChar);
            this.SetDataType<decimal>( DataType.Decimal);
            this.SetDataType<DateTime>( DataType.DateTime2);
            this.SetDataType<DateTimeOffset>( DataType.DateTimeOffset);
            this.SetDataType<TimeSpan>( DataType.Time);
#if NET6_0_OR_GREATER
				this.SetDataType<DateOnly>(DataType.Date);
#endif
            this.SetDataType<byte[]>( DataType.VarBinary);
            //this.SetDataType<Binary>( DataType.VarBinary);
            this.SetDataType<Guid>( DataType.Guid);
            this.SetDataType<object>( DataType.Variant);
            this.SetDataType<XmlDocument>( DataType.Xml);
            this.SetDataType<XDocument>( DataType.Xml);
            this.SetDataType<bool>( DataType.Boolean);
            this.SetDataType<sbyte>( DataType.SByte);
            this.SetDataType<short>( DataType.Int16);
            this.SetDataType<int>( DataType.Int32);
            this.SetDataType<long>( DataType.Int64);
            this.SetDataType<byte>( DataType.Byte);
            this.SetDataType<ushort>( DataType.UInt16);
            this.SetDataType<uint>( DataType.UInt32);
            this.SetDataType<ulong>( DataType.UInt64);
            this.SetDataType<float>( DataType.Single);
            this.SetDataType<double>( DataType.Double);

            this.SetDataType<BitArray>( DataType.BitArray);
        }

        private void InitValueConvert()
        {

            this.SetValueConverter((sbyte v) => v.ToString());
            SetValueConverter((sbyte? v) => v!.Value.ToString());
            SetValueConverter((string s) => sbyte.Parse(s ));
            SetValueConverter((string s) => (sbyte?)sbyte.Parse(s ));

            SetValueConverter((short v) => v.ToString());
            SetValueConverter((short? v) => v!.Value.ToString());
            SetValueConverter((string s) => short.Parse(s ));
            SetValueConverter((string s) => (short?)short.Parse(s ));

            SetValueConverter((int v) => v.ToString());
            SetValueConverter((int? v) => v!.Value.ToString());
            SetValueConverter((string s) => int.Parse(s));
            SetValueConverter((string s) => (int?)int.Parse(s));

            SetValueConverter((long v) => v.ToString());
            SetValueConverter((long? v) => v!.Value.ToString());
            SetValueConverter((string s) => long.Parse(s));
            SetValueConverter((string s) => (long?)long.Parse(s));

            SetValueConverter((byte v) => v.ToString());
            SetValueConverter((byte? v) => v!.Value.ToString());
            SetValueConverter((string s) => byte.Parse(s));
            SetValueConverter((string s) => (byte?)byte.Parse(s));

            SetValueConverter((ushort v) => v.ToString());
            SetValueConverter((ushort? v) => v!.Value.ToString());
            SetValueConverter((string s) => ushort.Parse(s));
            SetValueConverter((string s) => (ushort?)ushort.Parse(s));

            SetValueConverter((uint v) => v.ToString());
            SetValueConverter((uint? v) => v!.Value.ToString());
            SetValueConverter((string s) => uint.Parse(s));
            SetValueConverter((string s) => (uint?)uint.Parse(s));

            SetValueConverter((ulong v) => v.ToString());
            SetValueConverter((ulong? v) => v!.Value.ToString());
            SetValueConverter((string s) => ulong.Parse(s));
            SetValueConverter((string s) => (ulong?)ulong.Parse(s));

            SetValueConverter((float v) => v.ToString());
            SetValueConverter((float? v) => v!.Value.ToString());
            SetValueConverter((string s) => float.Parse(s));
            SetValueConverter((string s) => (float?)float.Parse(s));

            SetValueConverter((double v) => v.ToString());
            SetValueConverter((double? v) => v!.Value.ToString());
            SetValueConverter((string s) => double.Parse(s));
            SetValueConverter((string s) => (double?)double.Parse(s));

            SetValueConverter((decimal v) => v.ToString());
            SetValueConverter((decimal? v) => v!.Value.ToString());
            SetValueConverter((string s) => decimal.Parse(s));
            SetValueConverter((string s) => (decimal?)decimal.Parse(s));

            SetValueConverter((DateTime v) => v.ToString());
            SetValueConverter((DateTime? v) => v!.Value.ToString());
            SetValueConverter((string s) => DateTime.Parse(s ));
            SetValueConverter((string s) => (DateTime?)DateTime.Parse(s ));

            SetValueConverter((DateTimeOffset v) => v.ToString());
            SetValueConverter((DateTimeOffset? v) => v!.Value.ToString());
            SetValueConverter((string s) => DateTimeOffset.Parse(s ));
            SetValueConverter((string s) => (DateTimeOffset?)DateTimeOffset.Parse(s ));
        }

        private void InitScalarType() {
            AddScalarType(typeof(char), DataType.NChar);
            AddScalarType(typeof(string), DataType.NVarChar);
            AddScalarType(typeof(decimal), DataType.Decimal);
            AddScalarType(typeof(DateTime), DataType.DateTime2);
            AddScalarType(typeof(DateTimeOffset), DataType.DateTimeOffset);
            AddScalarType(typeof(TimeSpan), DataType.Time);
#if NET6_0_OR_GREATER
				AddScalarType(typeof(DateOnly),        DataType.Date);
#endif
            AddScalarType(typeof(byte[]), DataType.VarBinary);
            //AddScalarType(typeof(Binary), DataType.VarBinary);
            AddScalarType(typeof(Guid), DataType.Guid);
            AddScalarType(typeof(object), DataType.Variant);
            AddScalarType(typeof(XmlDocument), DataType.Xml);
            AddScalarType(typeof(XDocument), DataType.Xml);
            AddScalarType(typeof(bool), DataType.Boolean);
            AddScalarType(typeof(sbyte), DataType.SByte);
            AddScalarType(typeof(short), DataType.Int16);
            AddScalarType(typeof(int), DataType.Int32);
            AddScalarType(typeof(long), DataType.Int64);
            AddScalarType(typeof(byte), DataType.Byte);
            AddScalarType(typeof(ushort), DataType.UInt16);
            AddScalarType(typeof(uint), DataType.UInt32);
            AddScalarType(typeof(ulong), DataType.UInt64);
            AddScalarType(typeof(float), DataType.Single);
            AddScalarType(typeof(double), DataType.Double);

            AddScalarType(typeof(BitArray), DataType.BitArray);
        }

        public override Type ConvertParameterType(Type type, DbDataType dataType)
        {
            switch (dataType.DataType)
            {
                case DataType.Char:
                case DataType.NChar:
                case DataType.VarChar:
                case DataType.NVarChar:
                case DataType.Text:
                case DataType.NText:
                    if (type == typeof(DateTimeOffset)) return typeof(string);
                    break;
                case DataType.Image:
                case DataType.Binary:
                case DataType.Blob:
                //case DataType.VarBinary:
                //    if (type == typeof(Binary)) return typeof(byte[]);
                //    break;
                case DataType.Int64:
                    if (type == typeof(TimeSpan)) return typeof(long);
                    break;
                case DataType.Xml:
                    if (type == typeof(XDocument) ||
                        type == typeof(XmlDocument)) return typeof(string);
                    break;
            }

            return type;
        }

        public virtual void SetParameter(DbCommand cmd, DbParameter parameter, string name, DbDataType dataType, object? value)
        {
            switch (dataType.DataType)
            {
                case DataType.Char:
                case DataType.NChar:
                case DataType.VarChar:
                case DataType.NVarChar:
                case DataType.Text:
                case DataType.NText:
                    if (value is DateTimeOffset dto) value = dto.ToString("yyyy-MM-ddTHH:mm:ss.ffffff zzz", DateTimeFormatInfo.InvariantInfo);
                    else if (value is DateTime dt)
                    {
                        value = dt.ToString(
                            dt.Millisecond == 0
                                ? dt.Hour == 0 && dt.Minute == 0 && dt.Second == 0
                                    ? "yyyy-MM-dd"
                                    : "yyyy-MM-ddTHH:mm:ss"
                                : "yyyy-MM-ddTHH:mm:ss.fff",
                            DateTimeFormatInfo.InvariantInfo);
                    }
                    else if (value is TimeSpan ts)
                    {
                        value = ts.ToString(
                            ts.Days > 0
                                ? ts.Milliseconds > 0
                                    ? "d\\.hh\\:mm\\:ss\\.fff"
                                    : "d\\.hh\\:mm\\:ss"
                                : ts.Milliseconds > 0
                                    ? "hh\\:mm\\:ss\\.fff"
                                    : "hh\\:mm\\:ss",
                            DateTimeFormatInfo.InvariantInfo);
                    }
                    break;
                case DataType.Image:
                case DataType.Binary:
                case DataType.Blob:
                case DataType.VarBinary:
                    //if (value is Binary binary) value = binary.ToArray();
                    break;
                case DataType.Int64:
                    if (value is TimeSpan span) value = span.Ticks;
                    break;
                case DataType.Xml:
                    if (value is XDocument xdoc) value = xdoc.ToString();
                    else if (value is XmlDocument document) value = document.InnerXml;
                    break;
            }

            parameter.ParameterName = name;
            SetParameterType(cmd, parameter, dataType);
            parameter.Value = value ?? DBNull.Value;
        }

        protected override void SetParameterType(DbCommand cmd, DbParameter parameter, DbDataType dataType)
        {
            DbType dbType;

            switch (dataType.DataType)
            {
                case DataType.Char: dbType = DbType.AnsiStringFixedLength; break;
                case DataType.VarChar: dbType = DbType.AnsiString; break;
                case DataType.NChar: dbType = DbType.StringFixedLength; break;
                case DataType.NVarChar: dbType = DbType.String; break;
                case DataType.Blob:
                case DataType.VarBinary: dbType = DbType.Binary; break;
                case DataType.Boolean: dbType = DbType.Boolean; break;
                case DataType.SByte: dbType = DbType.SByte; break;
                case DataType.Int16: dbType = DbType.Int16; break;
                case DataType.Int32: dbType = DbType.Int32; break;
                case DataType.Int64: dbType = DbType.Int64; break;
                case DataType.Byte: dbType = DbType.Byte; break;
                case DataType.UInt16: dbType = DbType.UInt16; break;
                case DataType.UInt32: dbType = DbType.UInt32; break;
                case DataType.UInt64: dbType = DbType.UInt64; break;
                case DataType.Single: dbType = DbType.Single; break;
                case DataType.Double: dbType = DbType.Double; break;
                case DataType.Decimal: dbType = DbType.Decimal; break;
                case DataType.Guid: dbType = DbType.Guid; break;
                case DataType.Date: dbType = DbType.Date; break;
                case DataType.Time: dbType = DbType.Time; break;
                case DataType.DateTime: dbType = DbType.DateTime; break;
                case DataType.DateTime2: dbType = DbType.DateTime2; break;
                case DataType.DateTimeOffset: dbType = DbType.DateTimeOffset; break;
                case DataType.Variant: dbType = DbType.Object; break;
                case DataType.VarNumeric: dbType = DbType.VarNumeric; break;
                default: return;
            }

            parameter.DbType = dbType;
        }
    }
}
