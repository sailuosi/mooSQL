using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace mooSQL.data.model
{
	using Common;
	using Common.Internal;
    using mooSQL.data.Extensions;
    using mooSQL.utils;

    public class DataTypeWord :Clause, IExpWord, IEquatable<DataTypeWord>
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitDataTypeWord(this);
        }

        #region Init
        public DataTypeWord(DataType dataType, Type type, int? length, int? precision, int? scale, string? dbType)
    : this(new DbDataType(type, dataType, dbType, length, precision, scale))
        {
        }
        public DataTypeWord(DbDataType dataType) : base(ClauseType.SqlDataType, null)
        {
			Type = dataType;
		}

		public DataTypeWord(DataType dataType) : base(ClauseType.SqlDataType, null)
        {
			Type = GetDataType(dataType).Type.WithDataType(dataType);
		}

		public DataTypeWord(DataType dataType, int? length) : base(ClauseType.SqlDataType, null)
        {
			Type = GetDataType(dataType).Type.WithDataType(dataType).WithLength(length);
		}

		public DataTypeWord(DataType dataType, Type type) : base(ClauseType.SqlDataType, null)
        {
			if (type == null) throw new ArgumentNullException(nameof(type));

			Type = GetDataType(dataType).Type
				.WithDataType(dataType)
				.WithSystemType(type);
		}

		public DataTypeWord(DataType dataType, Type type, string dbType) : base(ClauseType.SqlDataType, null)
        {
			if (type == null) throw new ArgumentNullException(nameof(type));

			Type = GetDataType(dataType).Type
				.WithDataType(dataType)
				.WithSystemType(type)
				.WithDbType(dbType);
		}

		public DataTypeWord(DataType dataType, Type type, int length) : base(ClauseType.SqlDataType, null)
        {
			if (type == null) throw new ArgumentNullException(nameof(type));
			if (length <= 0)  throw new ArgumentOutOfRangeException(nameof(length));

			Type = GetDataType(dataType).Type
				.WithDataType(dataType)
				.WithSystemType(type)
				.WithLength(length);
		}

		public DataTypeWord(DataType dataType, Type type, int precision, int scale) : base(ClauseType.SqlDataType, null)
        {
			if (type      == null) throw new ArgumentNullException(nameof(type));
			if (precision <= 0   ) throw new ArgumentOutOfRangeException(nameof(precision));
			if (scale     <  0   ) throw new ArgumentOutOfRangeException(nameof(scale));

			Type = GetDataType(dataType).Type
				.WithDataType(dataType)
				.WithSystemType(type)
				.WithPrecision(precision)
				.WithScale(scale);
		}
        /*
		internal SqlDataType(ColumnDescriptor column)
			: this(column.GetDbDataType(true))
		{
		}*/

        public DataTypeWord(FieldWord field)
			: this(field.Type)
		{
		}

		#endregion

		#region Public Members

		public DbDataType Type { get; internal set; }

		public static readonly DataTypeWord Undefined = new (DataType.Undefined, typeof(object), (int?)null, (int?)null, null, null);

		public bool IsCharDataType
		{
			get
			{
				switch (Type.DataType)
				{
					case DataType.Char     :
					case DataType.NChar    :
					case DataType.VarChar  :
					case DataType.NVarChar : return true;
					default                : return false;
				}
			}
		}

		#endregion

		#region Static Members

		readonly struct TypeInfo
		{
			public TypeInfo(DataType dbType, int? maxLength, int? maxPrecision, int? maxScale, int? maxDisplaySize)
			{
				DataType       = dbType;
				MaxLength      = maxLength;
				MaxPrecision   = maxPrecision;
				MaxScale       = maxScale;
				MaxDisplaySize = maxDisplaySize;
			}

			public readonly DataType DataType;
			public readonly int?     MaxLength;
			public readonly int?     MaxPrecision;
			public readonly int?     MaxScale;
			public readonly int?     MaxDisplaySize;
		}

		static TypeInfo[] SortTypeInfo(params TypeInfo[] info)
		{
			var sortedInfo = new TypeInfo[info.Max(ti => (int)ti.DataType) + 1];

			foreach (var typeInfo in info)
				sortedInfo[(int)typeInfo.DataType] = typeInfo;

			return sortedInfo;
		}

		static int Len(object obj)
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}", obj).Length;
		}

		static readonly TypeInfo[] _typeInfo = SortTypeInfo
		(
			//           DbType                 MaxLength           MaxPrecision               MaxScale       MaxDisplaySize
			//
			new TypeInfo(DataType.Int64,                8,   Len( long.MaxValue),                     0,     Len( long.MinValue)),
			new TypeInfo(DataType.Int32,                4,   Len(  int.MaxValue),                     0,     Len(  int.MinValue)),
			new TypeInfo(DataType.Int16,                2,   Len(short.MaxValue),                     0,     Len(short.MinValue)),
			new TypeInfo(DataType.Byte,                 1,   Len( byte.MaxValue),                     0,     Len( byte.MaxValue)),
			new TypeInfo(DataType.Boolean,              1,                     1,                     0,                       1),

			new TypeInfo(DataType.Decimal,             17, Len(decimal.MaxValue), Len(decimal.MaxValue), Len(decimal.MinValue)+1),
			new TypeInfo(DataType.Money,                8,                    19,                     4,                  19 + 2),
			new TypeInfo(DataType.SmallMoney,           4,                    10,                     4,                  10 + 2),
			new TypeInfo(DataType.Double,               8,                    15,                    15,              15 + 2 + 5),
			new TypeInfo(DataType.Single,               4,                     7,                     7,               7 + 2 + 4),

			new TypeInfo(DataType.DateTime,             8,                  null,                  null,                      23),
			new TypeInfo(DataType.DateTime2,            8,                  null,                  null,                      27),
			new TypeInfo(DataType.SmallDateTime,        4,                  null,                  null,                      19),
			new TypeInfo(DataType.Date,                 3,                  null,                  null,                      10),
			new TypeInfo(DataType.Time,                 5,                  null,                  null,                      16),
			new TypeInfo(DataType.DateTimeOffset,      10,                  null,                  null,                      34),

			new TypeInfo(DataType.Char,              8000,                  null,                  null,                    8000),
			new TypeInfo(DataType.VarChar,           8000,                  null,                  null,                    8000),
			new TypeInfo(DataType.Text,              null,                  null,                  null,            int.MaxValue),
			new TypeInfo(DataType.NChar,             4000,                  null,                  null,                    4000),
			new TypeInfo(DataType.NVarChar,          4000,                  null,                  null,                    4000),
			new TypeInfo(DataType.NText,             null,                  null,                  null,        int.MaxValue / 2),
			new TypeInfo(DataType.Json,              null,                  null,                  null,        int.MaxValue / 2),
			new TypeInfo(DataType.BinaryJson,        null,                  null,                  null,        int.MaxValue / 2),

			new TypeInfo(DataType.Binary,            8000,                  null,                  null,                    null),
			new TypeInfo(DataType.VarBinary,         8000,                  null,                  null,                    null),
			new TypeInfo(DataType.Image,     int.MaxValue,                  null,                  null,                    null),

			new TypeInfo(DataType.Timestamp,            8,                  null,                  null,                    null),
			new TypeInfo(DataType.Guid,                16,                  null,                  null,                      36),

			new TypeInfo(DataType.Variant,           null,                  null,                  null,                    null),
			new TypeInfo(DataType.Xml,               null,                  null,                  null,                    null),
			new TypeInfo(DataType.Udt,               null,                  null,                  null,                    null),
			new TypeInfo(DataType.BitArray,          null,                  null,                  null,                    null)
		);

		public static int? GetMaxLength(DataType dbType)
		{
			var idx = (int)dbType;
			if (idx >= _typeInfo.Length)
				return null;
			return _typeInfo[idx].MaxLength;
		}

		public static int? GetMaxPrecision(DataType dbType)
		{
			var idx = (int)dbType;
			if (idx >= _typeInfo.Length)
				return null;
			return _typeInfo[idx].MaxPrecision;
		}

		public static int? GetMaxScale(DataType dbType)
		{
			var idx = (int)dbType;
			if (idx >= _typeInfo.Length)
				return null;
			return _typeInfo[idx].MaxScale;
		}

		public static int? GetMaxDisplaySize(DataType dbType)
		{
			var idx = (int)dbType;
			if (idx >= _typeInfo.Length)
				return null;
			return _typeInfo[idx].MaxDisplaySize;
		}

		public static DataTypeWord GetDataType(DataType type)
		{
			return type switch
			{
				DataType.Int64          => DbInt64,
				DataType.Binary         => DbBinary,
				DataType.Boolean        => DbBoolean,
				DataType.Char           => DbChar,
				DataType.DateTime       => DbDateTime,
				DataType.Decimal        => DbDecimal,
				DataType.Double         => DbDouble,
				DataType.Image          => DbImage,
				DataType.Int32          => DbInt32,
				DataType.Money          => DbMoney,
				DataType.NChar          => DbNChar,
				DataType.NText          => DbNText,
				DataType.NVarChar       => DbNVarChar,
				DataType.Single         => DbSingle,
				DataType.Guid           => DbGuid,
				DataType.SmallDateTime  => DbSmallDateTime,
				DataType.Int16          => DbInt16,
				DataType.SmallMoney     => DbSmallMoney,
				DataType.Text           => DbText,
				DataType.Timestamp      => DbTimestamp,
				DataType.Byte           => DbByte,
				DataType.VarBinary      => DbVarBinary,
				DataType.VarChar        => DbVarChar,
				DataType.Variant        => DbVariant,
				DataType.Xml            => DbXml,
				DataType.BitArray       => DbBitArray,
				DataType.Udt            => DbUdt,
				DataType.Date           => DbDate,
				DataType.Time           => DbTime,
				DataType.DateTime2      => DbDateTime2,
				DataType.DateTimeOffset => DbDateTimeOffset,
				DataType.UInt16         => DbUInt16,
				DataType.UInt32         => DbUInt32,
				DataType.UInt64         => DbUInt64,
				DataType.Dictionary     => DbDictionary,
				DataType.Json           => DbJson,
				DataType.BinaryJson     => DbBinaryJson,
				DataType.SByte          => DbSByte,
				DataType.Int128         => DbInt128,
				DataType.DecFloat       => DbDecFloat,
				DataType.TimeTZ         => DbTimeTZ,
				_                       => throw new InvalidOperationException($"Unexpected type: {type}"),
			};
		}

		public static bool TypeCanBeNull(Type type)
		{
			if (type.IsNullableType() ||
				typeof(INullable).IsSameOrParentOf(type))
				return true;

			return false;
		}

        #endregion

        #region Default Types

  //      public DataTypeWord(DataType dataType, Type type, int? length, int? precision, int? scale, string? dbType)
		//	: this(new DbDataType(type, dataType, dbType, length, precision, scale))
		//{
		//}

        public DataTypeWord(DataType dataType, Type type, Func<DataType,int?> length, int? precision, int? scale, string? dbType)
			: this(dataType, type, length(dataType), precision, scale, dbType)
		{
		}

        public DataTypeWord(DataType dataType, Type type, int? length, Func<DataType,int?> precision, int? scale, string? dbType)
			: this(dataType, type, length, precision(dataType), scale, dbType)
		{
		}

		public static readonly DataTypeWord DbInt128         = new (DataType.Int128,         typeof(BigInteger),    (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbInt64          = new (DataType.Int64,          typeof(long),          (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbInt32          = new (DataType.Int32,          typeof(int),           (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbInt16          = new (DataType.Int16,          typeof(short),         (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbUInt64         = new (DataType.UInt64,         typeof(ulong),         (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbUInt32         = new (DataType.UInt32,         typeof(uint),          (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbUInt16         = new (DataType.UInt16,         typeof(ushort),        (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbSByte          = new (DataType.SByte,          typeof(sbyte),         (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbByte           = new (DataType.Byte,           typeof(byte),          (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbBoolean        = new (DataType.Boolean,        typeof(bool),          (int?)null,     (int?)null, null, null);

		public static readonly DataTypeWord DbDecimal        = new (DataType.Decimal,        typeof(decimal),             null, GetMaxPrecision, 10, null);
		public static readonly DataTypeWord DbMoney          = new (DataType.Money,          typeof(decimal),             null, GetMaxPrecision,  4, null);
		public static readonly DataTypeWord DbSmallMoney     = new (DataType.SmallMoney,     typeof(decimal),             null, GetMaxPrecision,  4, null);
		public static readonly DataTypeWord DbDouble         = new (DataType.Double,         typeof(double),        (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbSingle         = new (DataType.Single,         typeof(float),         (int?)null,     (int?)null, null, null);

		public static readonly DataTypeWord DbDateTime       = new (DataType.DateTime,       typeof(DateTime),      (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbDateTime2      = new (DataType.DateTime2,      typeof(DateTime),      (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbSmallDateTime  = new (DataType.SmallDateTime,  typeof(DateTime),      (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbDate           = new (DataType.Date,           typeof(DateTime),      (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbTime           = new (DataType.Time,           typeof(TimeSpan),      (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbDateTimeOffset = new (DataType.DateTimeOffset, typeof(DateTimeOffset),(int?)null,     (int?)null, null, null);

		public static readonly DataTypeWord DbChar           = new (DataType.Char,           typeof(string),      GetMaxLength,           null, null, null);
		public static readonly DataTypeWord DbVarChar        = new (DataType.VarChar,        typeof(string),      GetMaxLength,           null, null, null);
		public static readonly DataTypeWord DbText           = new (DataType.Text,           typeof(string),      GetMaxLength,           null, null, null);
		public static readonly DataTypeWord DbNChar          = new (DataType.NChar,          typeof(string),      GetMaxLength,           null, null, null);
		public static readonly DataTypeWord DbNVarChar       = new (DataType.NVarChar,       typeof(string),      GetMaxLength,           null, null, null);
		public static readonly DataTypeWord DbNText          = new (DataType.NText,          typeof(string),      GetMaxLength,           null, null, null);
		public static readonly DataTypeWord DbJson           = new (DataType.Json,           typeof(string),      GetMaxLength,           null, null, null);
		public static readonly DataTypeWord DbBinaryJson     = new (DataType.BinaryJson,     typeof(string),      GetMaxLength,           null, null, null);

		public static readonly DataTypeWord DbBinary         = new (DataType.Binary,         typeof(byte[]),      GetMaxLength,           null, null, null);
		public static readonly DataTypeWord DbVarBinary      = new (DataType.VarBinary,      typeof(byte[]),      GetMaxLength,           null, null, null);
		public static readonly DataTypeWord DbImage          = new (DataType.Image,          typeof(byte[]),      GetMaxLength,           null, null, null);

		public static readonly DataTypeWord DbTimestamp      = new (DataType.Timestamp,      typeof(byte[]),        (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbGuid           = new (DataType.Guid,           typeof(Guid),          (int?)null,     (int?)null, null, null);

		public static readonly DataTypeWord DbVariant        = new (DataType.Variant,        typeof(object),        (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbXml            = new (DataType.Xml,            typeof(SqlXml),        (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbBitArray       = new (DataType.BitArray,       typeof(BitArray),      (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbUdt            = new (DataType.Udt,            typeof(object),        (int?)null,     (int?)null, null, null);

		public static readonly DataTypeWord Boolean          = DbBoolean;
		public static readonly DataTypeWord Char             = new (DataType.Char,           typeof(char),                   1,     (int?)null, null, null);
		public static readonly DataTypeWord SByte            = DbSByte;
		public static readonly DataTypeWord Byte             = DbByte;
		public static readonly DataTypeWord Int16            = DbInt16;
		public static readonly DataTypeWord UInt16           = new (DataType.UInt16,         typeof(ushort),        (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord Int32            = DbInt32;
		public static readonly DataTypeWord UInt32           = new (DataType.UInt32,         typeof(uint),          (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord UInt64           = new (DataType.UInt64,         typeof(ulong),         (int?)null, ulong.MaxValue.ToString(NumberFormatInfo.InvariantInfo).Length, null, null);
		public static readonly DataTypeWord Single           = DbSingle;
		public static readonly DataTypeWord Double           = DbDouble;
		public static readonly DataTypeWord Decimal          = DbDecimal;
		public static readonly DataTypeWord DateTime         = DbDateTime2;
		public static readonly DataTypeWord String           = DbNVarChar;
		public static readonly DataTypeWord Guid             = DbGuid;
		public static readonly DataTypeWord ByteArray        = DbVarBinary;
		public static readonly DataTypeWord LinqBinary       = DbVarBinary;
		public static readonly DataTypeWord CharArray        = new (DataType.NVarChar,       typeof(char[]),      GetMaxLength,           null, null, null);
		public static readonly DataTypeWord DateTimeOffset   = DbDateTimeOffset;
		public static readonly DataTypeWord TimeSpan         = DbTime;
		public static readonly DataTypeWord DbDictionary     = new (DataType.Dictionary,     typeof(Dictionary<string, string>), (int?)null, (int?)null, null, null);

		public static readonly DataTypeWord SqlByte          = new (DataType.Byte,           typeof(SqlByte),       (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord SqlInt16         = new (DataType.Int16,          typeof(SqlInt16),      (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord SqlInt32         = new (DataType.Int32,          typeof(SqlInt32),      (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord SqlInt64         = new (DataType.Int64,          typeof(SqlInt64),      (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord SqlSingle        = new (DataType.Single,         typeof(SqlSingle),     (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord SqlBoolean       = new (DataType.Boolean,        typeof(SqlBoolean),    (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord SqlDouble        = new (DataType.Double,         typeof(SqlDouble),     (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord SqlDateTime      = new (DataType.DateTime,       typeof(SqlDateTime),   (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord SqlDecimal       = new (DataType.Decimal,        typeof(SqlDecimal),          null, GetMaxPrecision,  10, null);
		public static readonly DataTypeWord SqlMoney         = new (DataType.Money,          typeof(SqlMoney),            null, GetMaxPrecision,   4, null);
		public static readonly DataTypeWord SqlString        = new (DataType.NVarChar,       typeof(SqlString),   GetMaxLength,           null, null, null);
		public static readonly DataTypeWord SqlBinary        = new (DataType.Binary,         typeof(SqlBinary),   GetMaxLength,           null, null, null);
		public static readonly DataTypeWord SqlGuid          = new (DataType.Guid,           typeof(SqlGuid),       (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord SqlBytes         = new (DataType.Image,          typeof(SqlBytes),    GetMaxLength,           null, null, null);
		public static readonly DataTypeWord SqlChars         = new (DataType.Text,           typeof(SqlChars),    GetMaxLength,           null, null, null);
		public static readonly DataTypeWord SqlXml           = new (DataType.Xml,            typeof(SqlXml),        (int?)null,     (int?)null, null, null);

		// types without default .net type mapping
		public static readonly DataTypeWord DbDecFloat       = new (DataType.DecFloat,       typeof(object),        (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbTimeTZ         = new (DataType.TimeTZ,         typeof(object),        (int?)null,     (int?)null, null, null);
		#endregion

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return this.ToDebugString();
		}

#endif

		#endregion

		#region ISqlExpression Members

		public int  Precedence => PrecedenceLv.Primary;
		public Type SystemType => Type.SystemType;

		#endregion

		#region IEquatable<ISqlExpression> Members

		bool IEquatable<IExpWord>.Equals(IExpWord? other)
		{
			if (this == other)
				return true;

			return other is DataTypeWord type && Type.Equals(type.Type);
		}

		#endregion

		#region ISqlExpression Members

		public bool CanBeNullable(ISQLNode nullability) => CanBeNull;

		public bool CanBeNull => false;

		public bool Equals(IExpWord other, Func<IExpWord,IExpWord,bool> comparer)
		{
			return ((IExpWord)this).Equals(other) && comparer(this, other);
		}

		#endregion

		#region IQueryElement Members

#if DEBUG
		public string DebugText => this.ToDebugString();
#endif
		public ClauseType NodeType => ClauseType.SqlDataType;

		IElementWriter ToString(IElementWriter writer)
		{
			writer.Append(Type.DataType.ToString());

			if (!string.IsNullOrEmpty(Type.DbType))
				writer.Append(":\"").Append(Type.DbType).Append('"');

			if (Type.Length != null && Type.Length != 0)
				writer.Append('(').Append(Type.Length.Value).Append(')');
			else if (Type.Precision != null && Type.Precision != 0)
				writer.Append('(').Append(Type.Precision.Value).Append(',').Append(Type.Scale.Value).Append(')');

			return writer;
		}

		#endregion

		#region IEquatable<SqlDataType>

		public override int GetHashCode()
		{
			return Type.GetHashCode();
		}

		public bool Equals(DataTypeWord? other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;

			return Type.Equals(other.Type);
		}

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;

			return Equals((DataTypeWord)obj);
		}

		#endregion
	}
}
