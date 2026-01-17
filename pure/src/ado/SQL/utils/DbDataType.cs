using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace mooSQL.data.model
{


	/// <summary>
	/// 数据库字段类型
	/// </summary>
	[DebuggerDisplay("DbDataType: {ToString()}")]
	public struct DbDataType : IEquatable<DbDataType>
	{
		public static readonly DbDataType Undefined = new (typeof(object), DataFam.Undefined);

		[DebuggerStepThrough]
		public DbDataType(Type systemType) : this()
		{
			SystemType = systemType;
		}

		[DebuggerStepThrough]
		public DbDataType(Type systemType, DataFam dataType) : this(systemType)
		{
			DataType   = dataType;
		}

		[DebuggerStepThrough]
		public DbDataType(Type systemType, DataFam dataType, string? dbType) : this(systemType)
		{
			DataType   = dataType;
			DbType     = dbType;
		}

		[DebuggerStepThrough]
		public DbDataType(Type systemType, DataFam dataType, string? dbType, int? length) : this(systemType)
		{
			DataType   = dataType;
			DbType     = dbType;
			Length     = length;
		}

		[DebuggerStepThrough]
		public DbDataType(Type systemType, DataFam dataType, string? dbType, int? length, int? precision, int? scale) : this(systemType)
		{
			DataType  = dataType;
			DbType    = dbType;
			Length    = length;
			Precision = precision;
			Scale     = scale;
		}
        public DbDataType( DataFam dataType, int? length=0, int? precision = 0, int? scale = 0) 
        {
            DataType = dataType;
            Length = length;
            Precision = precision;
            Scale = scale;
        }

        [DebuggerStepThrough]
		public DbDataType(Type systemType, string dbType) : this(systemType)
		{
			DbType = dbType;
		}
		/// <summary>
		/// 系统类型
		/// </summary>
		public Type     SystemType { get; set; }
        /// <summary>
        /// 对应字段类型
        /// </summary>
        public DataFam DataType { get; set; }
        /// <summary>
        /// 字段类型
        /// </summary>
        public string?  DbType { get; set; }
        /// <summary>
        /// 长度
        /// </summary>
        public int?     Length { get; set; }
        /// <summary>
        /// 精度
        /// </summary>
        public int?     Precision  { get; set; }
		/// <summary>
		/// 小数位
		/// </summary>
		public int?     Scale { get; set; }



        public readonly DbDataType WithSetValues(DbDataType from)
		{
			return new DbDataType(
				from.SystemType != typeof(object)   ? from.SystemType : SystemType,
				from.DataType != DataFam.Undefined ? from.DataType   : DataType,
				!string.IsNullOrEmpty(from.DbType)  ? from.DbType     : DbType,
				from.Length    ?? Length,
				from.Precision ?? Precision,
				from.Scale     ?? Scale);
		}
		/// <summary>
		/// 是否需要长度
		/// </summary>
		public bool NeedLength;
		/// <summary>
		/// 是否需要精度
		/// </summary>
		public bool NeedPrecise;
		/// <summary>
		/// 是否需要小数位
		/// </summary>
		public bool NeedScale;

		public readonly DbDataType WithoutSystemType(DbDataType       from) => new (SystemType, from.DataType, from.DbType, from.Length, from.Precision, from.Scale);
		//public readonly DbDataType WithoutSystemType(ColumnDescriptor from) => new (SystemType, from.DataType, from.DbType, from.Length, from.Precision, from.Scale);

		public readonly DbDataType WithSystemType    (Type     systemType           ) => new (systemType, DataType, DbType, Length, Precision, Scale);
		public readonly DbDataType WithDataType      (DataFam dataType             ) => new (SystemType, dataType, DbType, Length, Precision, Scale);
		public readonly DbDataType WithDbType        (string?  dbName               ) => new (SystemType, DataType, dbName, Length, Precision, Scale);
		public readonly DbDataType WithLength        (int?     length               ) => new (SystemType, DataType, DbType, length, Precision, Scale);
		public readonly DbDataType WithPrecision     (int?     precision            ) => new (SystemType, DataType, DbType, Length, precision, Scale);
		public readonly DbDataType WithScale         (int?     scale                ) => new (SystemType, DataType, DbType, Length, Precision, scale);
		public readonly DbDataType WithPrecisionScale(int?     precision, int? scale) => new (SystemType, DataType, DbType, Length, precision, scale);

		public readonly override string ToString()
		{
			var dataTypeStr  = DataType == DataFam.Undefined ? string.Empty : $", {DataType}";
			var dbTypeStr    = string.IsNullOrEmpty(DbType)   ? string.Empty : $", \"{DbType}\"";
			var lengthStr    = Length == null                 ? string.Empty : $", \"{Length}\"";
			var precisionStr = Precision == null              ? string.Empty : $", \"{Precision}\"";
			var scaleStr     = Scale == null                  ? string.Empty : $", \"{Scale}\"";
			return $"({SystemType}{dataTypeStr}{dbTypeStr}{lengthStr}{precisionStr}{scaleStr})";
		}
		/// <summary>
		/// 类型声明部分的转换，不包含默认值、可空性、注释等类型
		/// </summary>
		/// <returns></returns>
		public string ToDBString() {
			var res = new StringBuilder();
			res.Append(this.DbType);
			if(this.NeedLength)
			{
				res.Append('(');
				res.Append(this.Length);
				res.Append(")");
			}
			if(this.NeedPrecise)
			{
				res.Append("(");
				res.Append(Precision);
				if (this.NeedScale) { 
					res.Append(',');
					res.Append(Scale);
				}
				res.Append(")");
			}
			return res.ToString();
		}
		/// <summary>
		/// 检查是否相同
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool IsSame(DbDataType other) { 
			if(other == null) return false;
			if(this.DbType != other.DbType) return false;
			if (this.NeedLength) { 
				if(this.Length != other.Length) return false;
			}
			if (this.NeedPrecise) {
                if (this.Precision != other.Precision) return false;
            }
            if (this.NeedScale)
            {
                if (this.Scale != other.Scale) return false;
            }
			return true;
        }

		#region Equality members

		public readonly bool Equals(DbDataType other)
		{
			return SystemType == other.SystemType
				&& DataType   == other.DataType
				&& Length     == other.Length
				&& Precision  == other.Precision
				&& Scale      == other.Scale
				&& string.Equals(DbType, other.DbType, StringComparison.Ordinal);
		}

		public readonly bool EqualsDbOnly(DbDataType other)
		{
			return DataType   == other.DataType
				&& Length     == other.Length
				&& Precision  == other.Precision
				&& Scale      == other.Scale
				&& string.Equals(DbType, other.DbType, StringComparison.Ordinal);
		}

		public override bool Equals(object? obj)
		{
			if (obj is null) return false;
			return obj is DbDataType type && Equals(type);
		}

		int? _hashCode;

		[SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
		public override int GetHashCode()
		{
			if (_hashCode != null)
				return _hashCode.Value;

			unchecked
			{
				var hashCode = (SystemType != null ? SystemType.GetHashCode() : 0);
				hashCode     = (hashCode * 397) ^ (int) DataType;
				hashCode     = (hashCode * 397) ^ (DbType    != null ? DbType.GetHashCode()          : 0);
				hashCode     = (hashCode * 397) ^ (Length    != null ? Length.Value.GetHashCode()    : 0);
				hashCode     = (hashCode * 397) ^ (Precision != null ? Precision.Value.GetHashCode() : 0);
				hashCode     = (hashCode * 397) ^ (Scale     != null ? Scale.Value.GetHashCode()     : 0);
				_hashCode    = hashCode;
			}

			return _hashCode.Value;
		}

#endregion

		#region Operators

		public static bool operator ==(DbDataType t1, DbDataType t2)
		{
			return t1.Equals(t2);
		}

		public static bool operator !=(DbDataType t1, DbDataType t2)
		{
			return !(t1 == t2);
		}

		#endregion
	}
}
