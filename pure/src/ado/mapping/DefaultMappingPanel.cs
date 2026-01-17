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
            this.SetDataType<string>(DataFam.NVarChar);
            this.SetDataType<decimal>( DataFam.Decimal);
            this.SetDataType<DateTime>( DataFam.DateTime2);
            this.SetDataType<DateTimeOffset>( DataFam.DateTimeOffset);
            this.SetDataType<TimeSpan>( DataFam.Time);
#if NET6_0_OR_GREATER
				this.SetDataType<DateOnly>(DataFam.Date);
#endif
            this.SetDataType<byte[]>( DataFam.VarBinary);
            //this.SetDataType<Binary>( DataType.VarBinary);
            this.SetDataType<Guid>( DataFam.Guid);
            this.SetDataType<object>( DataFam.Variant);
            this.SetDataType<XmlDocument>( DataFam.Xml);
            this.SetDataType<XDocument>( DataFam.Xml);
            this.SetDataType<bool>( DataFam.Boolean);
            this.SetDataType<sbyte>( DataFam.SByte);
            this.SetDataType<short>( DataFam.Int16);
            this.SetDataType<int>( DataFam.Int32);
            this.SetDataType<long>( DataFam.Int64);
            this.SetDataType<byte>( DataFam.Byte);
            this.SetDataType<ushort>( DataFam.UInt16);
            this.SetDataType<uint>( DataFam.UInt32);
            this.SetDataType<ulong>( DataFam.UInt64);
            this.SetDataType<float>( DataFam.Single);
            this.SetDataType<double>( DataFam.Double);

            this.SetDataType<BitArray>( DataFam.BitArray);
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
            AddScalarType(typeof(char), DataFam.NChar);
            AddScalarType(typeof(string), DataFam.NVarChar);
            AddScalarType(typeof(decimal), DataFam.Decimal);
            AddScalarType(typeof(DateTime), DataFam.DateTime2);
            AddScalarType(typeof(DateTimeOffset), DataFam.DateTimeOffset);
            AddScalarType(typeof(TimeSpan), DataFam.Time);
#if NET6_0_OR_GREATER
				AddScalarType(typeof(DateOnly),        DataFam.Date);
#endif
            AddScalarType(typeof(byte[]), DataFam.VarBinary);
            //AddScalarType(typeof(Binary), DataType.VarBinary);
            AddScalarType(typeof(Guid), DataFam.Guid);
            AddScalarType(typeof(object), DataFam.Variant);
            AddScalarType(typeof(XmlDocument), DataFam.Xml);
            AddScalarType(typeof(XDocument), DataFam.Xml);
            AddScalarType(typeof(bool), DataFam.Boolean);
            AddScalarType(typeof(sbyte), DataFam.SByte);
            AddScalarType(typeof(short), DataFam.Int16);
            AddScalarType(typeof(int), DataFam.Int32);
            AddScalarType(typeof(long), DataFam.Int64);
            AddScalarType(typeof(byte), DataFam.Byte);
            AddScalarType(typeof(ushort), DataFam.UInt16);
            AddScalarType(typeof(uint), DataFam.UInt32);
            AddScalarType(typeof(ulong), DataFam.UInt64);
            AddScalarType(typeof(float), DataFam.Single);
            AddScalarType(typeof(double), DataFam.Double);

            AddScalarType(typeof(BitArray), DataFam.BitArray);
        }

        public override Type ConvertParameterType(Type type, DbDataType dataType)
        {
            switch (dataType.DataType)
            {
                case DataFam.Char:
                case DataFam.NChar:
                case DataFam.VarChar:
                case DataFam.NVarChar:
                case DataFam.Text:
                case DataFam.NText:
                    if (type == typeof(DateTimeOffset)) return typeof(string);
                    break;
                case DataFam.Image:
                case DataFam.Binary:
                case DataFam.Blob:
                //case DataType.VarBinary:
                //    if (type == typeof(Binary)) return typeof(byte[]);
                //    break;
                case DataFam.Int64:
                    if (type == typeof(TimeSpan)) return typeof(long);
                    break;
                case DataFam.Xml:
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
                case DataFam.Char:
                case DataFam.NChar:
                case DataFam.VarChar:
                case DataFam.NVarChar:
                case DataFam.Text:
                case DataFam.NText:
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
                case DataFam.Image:
                case DataFam.Binary:
                case DataFam.Blob:
                case DataFam.VarBinary:
                    //if (value is Binary binary) value = binary.ToArray();
                    break;
                case DataFam.Int64:
                    if (value is TimeSpan span) value = span.Ticks;
                    break;
                case DataFam.Xml:
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
                case DataFam.Char: dbType = DbType.AnsiStringFixedLength; break;
                case DataFam.VarChar: dbType = DbType.AnsiString; break;
                case DataFam.NChar: dbType = DbType.StringFixedLength; break;
                case DataFam.NVarChar: dbType = DbType.String; break;
                case DataFam.Blob:
                case DataFam.VarBinary: dbType = DbType.Binary; break;
                case DataFam.Boolean: dbType = DbType.Boolean; break;
                case DataFam.SByte: dbType = DbType.SByte; break;
                case DataFam.Int16: dbType = DbType.Int16; break;
                case DataFam.Int32: dbType = DbType.Int32; break;
                case DataFam.Int64: dbType = DbType.Int64; break;
                case DataFam.Byte: dbType = DbType.Byte; break;
                case DataFam.UInt16: dbType = DbType.UInt16; break;
                case DataFam.UInt32: dbType = DbType.UInt32; break;
                case DataFam.UInt64: dbType = DbType.UInt64; break;
                case DataFam.Single: dbType = DbType.Single; break;
                case DataFam.Double: dbType = DbType.Double; break;
                case DataFam.Decimal: dbType = DbType.Decimal; break;
                case DataFam.Guid: dbType = DbType.Guid; break;
                case DataFam.Date: dbType = DbType.Date; break;
                case DataFam.Time: dbType = DbType.Time; break;
                case DataFam.DateTime: dbType = DbType.DateTime; break;
                case DataFam.DateTime2: dbType = DbType.DateTime2; break;
                case DataFam.DateTimeOffset: dbType = DbType.DateTimeOffset; break;
                case DataFam.Variant: dbType = DbType.Object; break;
                case DataFam.VarNumeric: dbType = DbType.VarNumeric; break;
                default: return;
            }

            parameter.DbType = dbType;
        }
    }
}
