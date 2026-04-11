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
		/// <summary>未指定具体映射时的占位类型。</summary>
		public static readonly DbDataType Undefined = new (typeof(object), DataFam.Undefined);

		/// <summary>仅指定 CLR 类型。</summary>
		[DebuggerStepThrough]
		public DbDataType(Type systemType) : this()
		{
			SystemType = systemType;
		}

		/// <summary>指定 CLR 类型与逻辑数据族。</summary>
		[DebuggerStepThrough]
		public DbDataType(Type systemType, DataFam dataType) : this(systemType)
		{
			DataType   = dataType;
		}

		/// <summary>附加数据库类型名字符串（方言相关）。</summary>
		[DebuggerStepThrough]
		public DbDataType(Type systemType, DataFam dataType, string? dbType) : this(systemType)
		{
			DataType   = dataType;
			DbType     = dbType;
		}

		/// <summary>含最大长度（如 varchar(n)）。</summary>
		[DebuggerStepThrough]
		public DbDataType(Type systemType, DataFam dataType, string? dbType, int? length) : this(systemType)
		{
			DataType   = dataType;
			DbType     = dbType;
			Length     = length;
		}

		/// <summary>含长度、精度、标度（decimal 等）。</summary>
		[DebuggerStepThrough]
		public DbDataType(Type systemType, DataFam dataType, string? dbType, int? length, int? precision, int? scale) : this(systemType)
		{
			DataType  = dataType;
			DbType    = dbType;
			Length    = length;
			Precision = precision;
			Scale     = scale;
		}
		/// <summary>无 CLR 类型时仅用数据族与尺度信息构造（如仅从元数据反推）。</summary>
        public DbDataType( DataFam dataType, int? length=0, int? precision = 0, int? scale = 0) 
        {
            DataType = dataType;
            Length = length;
            Precision = precision;
            Scale = scale;
        }

		/// <summary>用方言类型名覆盖（如 <c>nvarchar</c>）。</summary>
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



		/// <summary>用 <paramref name="from"/> 中非默认值覆盖本实例对应字段。</summary>
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

		/// <summary>保留本实例 <see cref="SystemType"/>，其余尺度字段取自 <paramref name="from"/>。</summary>
		public readonly DbDataType WithoutSystemType(DbDataType       from) => new (SystemType, from.DataType, from.DbType, from.Length, from.Precision, from.Scale);
		//public readonly DbDataType WithoutSystemType(ColumnDescriptor from) => new (SystemType, from.DataType, from.DbType, from.Length, from.Precision, from.Scale);

		/// <summary>复制并替换 CLR 类型。</summary>
		public readonly DbDataType WithSystemType    (Type     systemType           ) => new (systemType, DataType, DbType, Length, Precision, Scale);
		/// <summary>复制并替换逻辑数据族。</summary>
		public readonly DbDataType WithDataType      (DataFam dataType             ) => new (SystemType, dataType, DbType, Length, Precision, Scale);
		/// <summary>复制并替换数据库类型名。</summary>
		public readonly DbDataType WithDbType        (string?  dbName               ) => new (SystemType, DataType, dbName, Length, Precision, Scale);
		/// <summary>复制并替换长度。</summary>
		public readonly DbDataType WithLength        (int?     length               ) => new (SystemType, DataType, DbType, length, Precision, Scale);
		/// <summary>复制并替换精度。</summary>
		public readonly DbDataType WithPrecision     (int?     precision            ) => new (SystemType, DataType, DbType, Length, precision, Scale);
		/// <summary>复制并替换标度。</summary>
		public readonly DbDataType WithScale         (int?     scale                ) => new (SystemType, DataType, DbType, Length, Precision, scale);
		/// <summary>同时设置精度与标度。</summary>
		public readonly DbDataType WithPrecisionScale(int?     precision, int? scale) => new (SystemType, DataType, DbType, Length, precision, scale);

		/// <inheritdoc />
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

		/// <inheritdoc />
		public readonly bool Equals(DbDataType other)
		{
			return SystemType == other.SystemType
				&& DataType   == other.DataType
				&& Length     == other.Length
				&& Precision  == other.Precision
				&& Scale      == other.Scale
				&& string.Equals(DbType, other.DbType, StringComparison.Ordinal);
		}

		/// <summary>忽略 <see cref="SystemType"/>，仅比较数据库侧类型与尺度。</summary>
		public readonly bool EqualsDbOnly(DbDataType other)
		{
			return DataType   == other.DataType
				&& Length     == other.Length
				&& Precision  == other.Precision
				&& Scale      == other.Scale
				&& string.Equals(DbType, other.DbType, StringComparison.Ordinal);
		}

		/// <inheritdoc />
		public override bool Equals(object? obj)
		{
			if (obj is null) return false;
			return obj is DbDataType type && Equals(type);
		}

		int? _hashCode;

		/// <inheritdoc />
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

		/// <summary>值相等比较。</summary>
		public static bool operator ==(DbDataType t1, DbDataType t2)
		{
			return t1.Equals(t2);
		}

		/// <summary>值不等比较。</summary>
		public static bool operator !=(DbDataType t1, DbDataType t2)
		{
			return !(t1 == t2);
		}

		#endregion
	}
}
