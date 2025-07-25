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
        public DataTypeWord(DataFam dataType, Type type, int? length, int? precision, int? scale, string? dbType)
    : this(new DbDataType(type, dataType, dbType, length, precision, scale))
        {
        }
        public DataTypeWord(DbDataType dataType) : base(ClauseType.SqlDataType, null)
        {
			Type = dataType;
		}

		public DataTypeWord(DataFam dataType) : base(ClauseType.SqlDataType, null)
        {
			Type = GetDataType(dataType).Type.WithDataType(dataType);
		}

		public DataTypeWord(DataFam dataType, int? length) : base(ClauseType.SqlDataType, null)
        {
			Type = GetDataType(dataType).Type.WithDataType(dataType).WithLength(length);
		}

		public DataTypeWord(DataFam dataType, Type type) : base(ClauseType.SqlDataType, null)
        {
			if (type == null) throw new ArgumentNullException(nameof(type));

			Type = GetDataType(dataType).Type
				.WithDataType(dataType)
				.WithSystemType(type);
		}

		public DataTypeWord(DataFam dataType, Type type, string dbType) : base(ClauseType.SqlDataType, null)
        {
			if (type == null) throw new ArgumentNullException(nameof(type));

			Type = GetDataType(dataType).Type
				.WithDataType(dataType)
				.WithSystemType(type)
				.WithDbType(dbType);
		}

		public DataTypeWord(DataFam dataType, Type type, int length) : base(ClauseType.SqlDataType, null)
        {
			if (type == null) throw new ArgumentNullException(nameof(type));
			if (length <= 0)  throw new ArgumentOutOfRangeException(nameof(length));

			Type = GetDataType(dataType).Type
				.WithDataType(dataType)
				.WithSystemType(type)
				.WithLength(length);
		}

		public DataTypeWord(DataFam dataType, Type type, int precision, int scale) : base(ClauseType.SqlDataType, null)
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

		public static readonly DataTypeWord Undefined = new (DataFam.Undefined, typeof(object), (int?)null, (int?)null, null, null);

		public bool IsCharDataType
		{
			get
			{
				switch (Type.DataType)
				{
					case DataFam.Char     :
					case DataFam.NChar    :
					case DataFam.VarChar  :
					case DataFam.NVarChar : return true;
					default                : return false;
				}
			}
		}

		#endregion

		#region Static Members

		readonly struct TypeInfo
		{
			public TypeInfo(DataFam dbType, int? maxLength, int? maxPrecision, int? maxScale, int? maxDisplaySize)
			{
				DataType       = dbType;
				MaxLength      = maxLength;
				MaxPrecision   = maxPrecision;
				MaxScale       = maxScale;
				MaxDisplaySize = maxDisplaySize;
			}

			public readonly DataFam DataType;
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
			new TypeInfo(DataFam.Int64,                8,   Len( long.MaxValue),                     0,     Len( long.MinValue)),
			new TypeInfo(DataFam.Int32,                4,   Len(  int.MaxValue),                     0,     Len(  int.MinValue)),
			new TypeInfo(DataFam.Int16,                2,   Len(short.MaxValue),                     0,     Len(short.MinValue)),
			new TypeInfo(DataFam.Byte,                 1,   Len( byte.MaxValue),                     0,     Len( byte.MaxValue)),
			new TypeInfo(DataFam.Boolean,              1,                     1,                     0,                       1),

			new TypeInfo(DataFam.Decimal,             17, Len(decimal.MaxValue), Len(decimal.MaxValue), Len(decimal.MinValue)+1),
			new TypeInfo(DataFam.Money,                8,                    19,                     4,                  19 + 2),
			new TypeInfo(DataFam.SmallMoney,           4,                    10,                     4,                  10 + 2),
			new TypeInfo(DataFam.Double,               8,                    15,                    15,              15 + 2 + 5),
			new TypeInfo(DataFam.Single,               4,                     7,                     7,               7 + 2 + 4),

			new TypeInfo(DataFam.DateTime,             8,                  null,                  null,                      23),
			new TypeInfo(DataFam.DateTime2,            8,                  null,                  null,                      27),
			new TypeInfo(DataFam.SmallDateTime,        4,                  null,                  null,                      19),
			new TypeInfo(DataFam.Date,                 3,                  null,                  null,                      10),
			new TypeInfo(DataFam.Time,                 5,                  null,                  null,                      16),
			new TypeInfo(DataFam.DateTimeOffset,      10,                  null,                  null,                      34),

			new TypeInfo(DataFam.Char,              8000,                  null,                  null,                    8000),
			new TypeInfo(DataFam.VarChar,           8000,                  null,                  null,                    8000),
			new TypeInfo(DataFam.Text,              null,                  null,                  null,            int.MaxValue),
			new TypeInfo(DataFam.NChar,             4000,                  null,                  null,                    4000),
			new TypeInfo(DataFam.NVarChar,          4000,                  null,                  null,                    4000),
			new TypeInfo(DataFam.NText,             null,                  null,                  null,        int.MaxValue / 2),
			new TypeInfo(DataFam.Json,              null,                  null,                  null,        int.MaxValue / 2),
			new TypeInfo(DataFam.BinaryJson,        null,                  null,                  null,        int.MaxValue / 2),

			new TypeInfo(DataFam.Binary,            8000,                  null,                  null,                    null),
			new TypeInfo(DataFam.VarBinary,         8000,                  null,                  null,                    null),
			new TypeInfo(DataFam.Image,     int.MaxValue,                  null,                  null,                    null),

			new TypeInfo(DataFam.Timestamp,            8,                  null,                  null,                    null),
			new TypeInfo(DataFam.Guid,                16,                  null,                  null,                      36),

			new TypeInfo(DataFam.Variant,           null,                  null,                  null,                    null),
			new TypeInfo(DataFam.Xml,               null,                  null,                  null,                    null),
			new TypeInfo(DataFam.Udt,               null,                  null,                  null,                    null),
			new TypeInfo(DataFam.BitArray,          null,                  null,                  null,                    null)
		);

		public static int? GetMaxLength(DataFam dbType)
		{
			var idx = (int)dbType;
			if (idx >= _typeInfo.Length)
				return null;
			return _typeInfo[idx].MaxLength;
		}

		public static int? GetMaxPrecision(DataFam dbType)
		{
			var idx = (int)dbType;
			if (idx >= _typeInfo.Length)
				return null;
			return _typeInfo[idx].MaxPrecision;
		}

		public static int? GetMaxScale(DataFam dbType)
		{
			var idx = (int)dbType;
			if (idx >= _typeInfo.Length)
				return null;
			return _typeInfo[idx].MaxScale;
		}

		public static int? GetMaxDisplaySize(DataFam dbType)
		{
			var idx = (int)dbType;
			if (idx >= _typeInfo.Length)
				return null;
			return _typeInfo[idx].MaxDisplaySize;
		}

		public static DataTypeWord GetDataType(DataFam type)
		{
			return type switch
			{
				DataFam.Int64          => DbInt64,
				DataFam.Binary         => DbBinary,
				DataFam.Boolean        => DbBoolean,
				DataFam.Char           => DbChar,
				DataFam.DateTime       => DbDateTime,
				DataFam.Decimal        => DbDecimal,
				DataFam.Double         => DbDouble,
				DataFam.Image          => DbImage,
				DataFam.Int32          => DbInt32,
				DataFam.Money          => DbMoney,
				DataFam.NChar          => DbNChar,
				DataFam.NText          => DbNText,
				DataFam.NVarChar       => DbNVarChar,
				DataFam.Single         => DbSingle,
				DataFam.Guid           => DbGuid,
				DataFam.SmallDateTime  => DbSmallDateTime,
				DataFam.Int16          => DbInt16,
				DataFam.SmallMoney     => DbSmallMoney,
				DataFam.Text           => DbText,
				DataFam.Timestamp      => DbTimestamp,
				DataFam.Byte           => DbByte,
				DataFam.VarBinary      => DbVarBinary,
				DataFam.VarChar        => DbVarChar,
				DataFam.Variant        => DbVariant,
				DataFam.Xml            => DbXml,
				DataFam.BitArray       => DbBitArray,
				DataFam.Udt            => DbUdt,
				DataFam.Date           => DbDate,
				DataFam.Time           => DbTime,
				DataFam.DateTime2      => DbDateTime2,
				DataFam.DateTimeOffset => DbDateTimeOffset,
				DataFam.UInt16         => DbUInt16,
				DataFam.UInt32         => DbUInt32,
				DataFam.UInt64         => DbUInt64,
				DataFam.Dictionary     => DbDictionary,
				DataFam.Json           => DbJson,
				DataFam.BinaryJson     => DbBinaryJson,
				DataFam.SByte          => DbSByte,
				DataFam.Int128         => DbInt128,
				DataFam.DecFloat       => DbDecFloat,
				DataFam.TimeTZ         => DbTimeTZ,
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

        public DataTypeWord(DataFam dataType, Type type, Func<DataFam,int?> length, int? precision, int? scale, string? dbType)
			: this(dataType, type, length(dataType), precision, scale, dbType)
		{
		}

        public DataTypeWord(DataFam dataType, Type type, int? length, Func<DataFam,int?> precision, int? scale, string? dbType)
			: this(dataType, type, length, precision(dataType), scale, dbType)
		{
		}

		public static readonly DataTypeWord DbInt128         = new (DataFam.Int128,         typeof(BigInteger),    (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbInt64          = new (DataFam.Int64,          typeof(long),          (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbInt32          = new (DataFam.Int32,          typeof(int),           (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbInt16          = new (DataFam.Int16,          typeof(short),         (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbUInt64         = new (DataFam.UInt64,         typeof(ulong),         (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbUInt32         = new (DataFam.UInt32,         typeof(uint),          (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbUInt16         = new (DataFam.UInt16,         typeof(ushort),        (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbSByte          = new (DataFam.SByte,          typeof(sbyte),         (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbByte           = new (DataFam.Byte,           typeof(byte),          (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbBoolean        = new (DataFam.Boolean,        typeof(bool),          (int?)null,     (int?)null, null, null);

		public static readonly DataTypeWord DbDecimal        = new (DataFam.Decimal,        typeof(decimal),             null, GetMaxPrecision, 10, null);
		public static readonly DataTypeWord DbMoney          = new (DataFam.Money,          typeof(decimal),             null, GetMaxPrecision,  4, null);
		public static readonly DataTypeWord DbSmallMoney     = new (DataFam.SmallMoney,     typeof(decimal),             null, GetMaxPrecision,  4, null);
		public static readonly DataTypeWord DbDouble         = new (DataFam.Double,         typeof(double),        (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbSingle         = new (DataFam.Single,         typeof(float),         (int?)null,     (int?)null, null, null);

		public static readonly DataTypeWord DbDateTime       = new (DataFam.DateTime,       typeof(DateTime),      (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbDateTime2      = new (DataFam.DateTime2,      typeof(DateTime),      (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbSmallDateTime  = new (DataFam.SmallDateTime,  typeof(DateTime),      (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbDate           = new (DataFam.Date,           typeof(DateTime),      (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbTime           = new (DataFam.Time,           typeof(TimeSpan),      (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbDateTimeOffset = new (DataFam.DateTimeOffset, typeof(DateTimeOffset),(int?)null,     (int?)null, null, null);

		public static readonly DataTypeWord DbChar           = new (DataFam.Char,           typeof(string),      GetMaxLength,           null, null, null);
		public static readonly DataTypeWord DbVarChar        = new (DataFam.VarChar,        typeof(string),      GetMaxLength,           null, null, null);
		public static readonly DataTypeWord DbText           = new (DataFam.Text,           typeof(string),      GetMaxLength,           null, null, null);
		public static readonly DataTypeWord DbNChar          = new (DataFam.NChar,          typeof(string),      GetMaxLength,           null, null, null);
		public static readonly DataTypeWord DbNVarChar       = new (DataFam.NVarChar,       typeof(string),      GetMaxLength,           null, null, null);
		public static readonly DataTypeWord DbNText          = new (DataFam.NText,          typeof(string),      GetMaxLength,           null, null, null);
		public static readonly DataTypeWord DbJson           = new (DataFam.Json,           typeof(string),      GetMaxLength,           null, null, null);
		public static readonly DataTypeWord DbBinaryJson     = new (DataFam.BinaryJson,     typeof(string),      GetMaxLength,           null, null, null);

		public static readonly DataTypeWord DbBinary         = new (DataFam.Binary,         typeof(byte[]),      GetMaxLength,           null, null, null);
		public static readonly DataTypeWord DbVarBinary      = new (DataFam.VarBinary,      typeof(byte[]),      GetMaxLength,           null, null, null);
		public static readonly DataTypeWord DbImage          = new (DataFam.Image,          typeof(byte[]),      GetMaxLength,           null, null, null);

		public static readonly DataTypeWord DbTimestamp      = new (DataFam.Timestamp,      typeof(byte[]),        (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbGuid           = new (DataFam.Guid,           typeof(Guid),          (int?)null,     (int?)null, null, null);

		public static readonly DataTypeWord DbVariant        = new (DataFam.Variant,        typeof(object),        (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbXml            = new (DataFam.Xml,            typeof(SqlXml),        (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbBitArray       = new (DataFam.BitArray,       typeof(BitArray),      (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbUdt            = new (DataFam.Udt,            typeof(object),        (int?)null,     (int?)null, null, null);

		public static readonly DataTypeWord Boolean          = DbBoolean;
		public static readonly DataTypeWord Char             = new (DataFam.Char,           typeof(char),                   1,     (int?)null, null, null);
		public static readonly DataTypeWord SByte            = DbSByte;
		public static readonly DataTypeWord Byte             = DbByte;
		public static readonly DataTypeWord Int16            = DbInt16;
		public static readonly DataTypeWord UInt16           = new (DataFam.UInt16,         typeof(ushort),        (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord Int32            = DbInt32;
		public static readonly DataTypeWord UInt32           = new (DataFam.UInt32,         typeof(uint),          (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord UInt64           = new (DataFam.UInt64,         typeof(ulong),         (int?)null, ulong.MaxValue.ToString(NumberFormatInfo.InvariantInfo).Length, null, null);
		public static readonly DataTypeWord Single           = DbSingle;
		public static readonly DataTypeWord Double           = DbDouble;
		public static readonly DataTypeWord Decimal          = DbDecimal;
		public static readonly DataTypeWord DateTime         = DbDateTime2;
		public static readonly DataTypeWord String           = DbNVarChar;
		public static readonly DataTypeWord Guid             = DbGuid;
		public static readonly DataTypeWord ByteArray        = DbVarBinary;
		public static readonly DataTypeWord LinqBinary       = DbVarBinary;
		public static readonly DataTypeWord CharArray        = new (DataFam.NVarChar,       typeof(char[]),      GetMaxLength,           null, null, null);
		public static readonly DataTypeWord DateTimeOffset   = DbDateTimeOffset;
		public static readonly DataTypeWord TimeSpan         = DbTime;
		public static readonly DataTypeWord DbDictionary     = new (DataFam.Dictionary,     typeof(Dictionary<string, string>), (int?)null, (int?)null, null, null);

		public static readonly DataTypeWord SqlByte          = new (DataFam.Byte,           typeof(SqlByte),       (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord SqlInt16         = new (DataFam.Int16,          typeof(SqlInt16),      (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord SqlInt32         = new (DataFam.Int32,          typeof(SqlInt32),      (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord SqlInt64         = new (DataFam.Int64,          typeof(SqlInt64),      (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord SqlSingle        = new (DataFam.Single,         typeof(SqlSingle),     (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord SqlBoolean       = new (DataFam.Boolean,        typeof(SqlBoolean),    (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord SqlDouble        = new (DataFam.Double,         typeof(SqlDouble),     (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord SqlDateTime      = new (DataFam.DateTime,       typeof(SqlDateTime),   (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord SqlDecimal       = new (DataFam.Decimal,        typeof(SqlDecimal),          null, GetMaxPrecision,  10, null);
		public static readonly DataTypeWord SqlMoney         = new (DataFam.Money,          typeof(SqlMoney),            null, GetMaxPrecision,   4, null);
		public static readonly DataTypeWord SqlString        = new (DataFam.NVarChar,       typeof(SqlString),   GetMaxLength,           null, null, null);
		public static readonly DataTypeWord SqlBinary        = new (DataFam.Binary,         typeof(SqlBinary),   GetMaxLength,           null, null, null);
		public static readonly DataTypeWord SqlGuid          = new (DataFam.Guid,           typeof(SqlGuid),       (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord SqlBytes         = new (DataFam.Image,          typeof(SqlBytes),    GetMaxLength,           null, null, null);
		public static readonly DataTypeWord SqlChars         = new (DataFam.Text,           typeof(SqlChars),    GetMaxLength,           null, null, null);
		public static readonly DataTypeWord SqlXml           = new (DataFam.Xml,            typeof(SqlXml),        (int?)null,     (int?)null, null, null);

		// types without default .net type mapping
		public static readonly DataTypeWord DbDecFloat       = new (DataFam.DecFloat,       typeof(object),        (int?)null,     (int?)null, null, null);
		public static readonly DataTypeWord DbTimeTZ         = new (DataFam.TimeTZ,         typeof(object),        (int?)null,     (int?)null, null, null);
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
