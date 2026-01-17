using System;

namespace mooSQL.data
{
	using mooSQL.data;
    using mooSQL.data.Mapping;
    using mooSQL.data.model;



    /// <summary>
    /// 配置类的属性到数据库字段
    /// </summary>
    [AttributeUsage(
		AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Interface,
		AllowMultiple = true, Inherited = true)]
	public class SooColumnAttribute : MappingAttribute
	{
		/// <summary>
		/// Creates attribute instance.
		/// </summary>
		public SooColumnAttribute()
		{
			IsColumn = true;
		}

		/// <summary>
		/// Creates attribute instance.
		/// </summary>
		/// <param name="columnName">Database column name.</param>
		public SooColumnAttribute(string columnName) : this()
		{
			Name = columnName;
		}

		/// <summary>
		/// Creates attribute instance.
		/// </summary>
		/// <param name="columnName">Database column name.</param>
		/// <param name="memberName">Name of mapped member. See <see cref="MemberName"/> for more details.</param>
		public SooColumnAttribute(string columnName, string memberName) : this()
		{
			Name       = columnName;
			MemberName = memberName;
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="memberName">Name of mapped member. See <see cref="MemberName"/> for more details.</param>
		/// <param name="ca">Attribute to clone.</param>
		public SooColumnAttribute(string memberName, SooColumnAttribute ca)
			: this(ca)
		{
			MemberName = memberName + "." + ca.MemberName!.TrimStart('.');
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="ca">Attribute to clone.</param>
		public SooColumnAttribute(SooColumnAttribute ca)
		{
			MemberName        = ca.MemberName;
			Configuration     = ca.Configuration;
			Name              = ca.Name;
			DataType          = ca.DataType;
			DbType            = ca.DbType;
			Storage           = ca.Storage;
			IsDiscriminator   = ca.IsDiscriminator;
			SkipOnEntityFetch = ca.SkipOnEntityFetch;
			PrimaryKeyOrder   = ca.PrimaryKeyOrder;
			IsColumn          = ca.IsColumn;
			CreateFormat      = ca.CreateFormat;

			if (ca.HasSkipOnInsert()) SkipOnInsert = ca.SkipOnInsert;
			if (ca.HasSkipOnUpdate()) SkipOnUpdate = ca.SkipOnUpdate;
			if (ca.HasCanBeNull())    CanBeNull    = ca.CanBeNull;
			if (ca.HasIsIdentity())   IsIdentity   = ca.IsIdentity;
			if (ca.HasIsPrimaryKey()) IsPrimaryKey = ca.IsPrimaryKey;
			if (ca.HasLength())       Length       = ca.Length;
			if (ca.HasPrecision())    Precision    = ca.Precision;
			if (ca.HasScale())        Scale        = ca.Scale;
			if (ca.HasOrder())        Order        = ca.Order;
		}

		/// <summary>
		/// 数据库字段名，缺省为属性名
		/// </summary>
		public string? Name { get; set; }

		/// <summary>
		/// 设置映射成员名称
		/// When applied to class or interface, should contain name of property of field.
		///
		/// If column mapped to a property or field of composite object, <see cref="MemberName"/> should contain a path to that
		/// member using dot as separator.
		/// <example>
		/// <code>
		/// public class Address
		/// {
		///     public string City     { get; set; }
		///     public string Street   { get; set; }
		///     public int    Building { get; set; }
		/// }
		///
		/// [Column("city", "Residence.City")]
		/// [Column("user_name", "Name")]
		/// public class User
		/// {
		///     public string Name;
		///
		///     [Column("street", ".Street")]
		///     [Column("building_number", MemberName = ".Building")]
		///     public Address Residence { get; set; }
		/// }
		/// </code>
		/// </example>
		/// </summary>
		public string? MemberName { get; set; }
		/// <summary>
		/// 中文名
		/// </summary>
		public string Caption { get; set; }

		/// <summary>
		/// 数据类型
		/// 默认为字段的类型.
		/// </summary>
		public DataFam DataType { get; set; }

		/// <summary>
		/// 字段类型
		/// </summary>
		public string? DbType { get; set; }

		/// <summary>
		/// 是否是字段
		/// </summary>
		public bool IsColumn { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public string? Storage { get; set; }

		public object DefaultValue { get; set; }

		/// <summary>
		/// 是否是区分列，用于多表继承。
		/// </summary>
		public bool IsDiscriminator { get; set; }

		/// <summary>
		/// 查询时不包含此列
		/// </summary>
		public bool SkipOnEntityFetch { get; set; }

		private bool? _skipOnInsert;
		/// <summary>
		/// 忽略插入
		/// </summary>
		public bool   SkipOnInsert
		{
			get => _skipOnInsert ?? false;
			set => _skipOnInsert = value;
		}

        /// <summary>
        /// Returns <c>true</c>, if <see cref="SkipOnInsert"/> was configured for current attribute.
        /// </summary>
        /// <returns><c>true</c> if <see cref="SkipOnInsert"/> property was set in attribute.</returns>
        public bool HasSkipOnInsert() => _skipOnInsert.HasValue;

		private bool? _skipOnUpdate;
		/// <summary>
		/// 忽略更新
		/// </summary>
		public bool   SkipOnUpdate
		{
			get => _skipOnUpdate ?? false;
			set => _skipOnUpdate = value;
		}

        /// <summary>
        /// 是否忽略更新
        public bool HasSkipOnUpdate() => _skipOnUpdate.HasValue;

		private bool? _isIdentity;
		/// <summary>
		/// 是否自增列。
		/// </summary>
		public  bool   IsIdentity
		{
			get => _isIdentity ?? false;
			set => _isIdentity = value;
		}

        /// <summary>
        /// 是否自增列
        public bool HasIsIdentity() => _isIdentity.HasValue;

		private bool? _isPrimaryKey;
		/// <summary>
		/// 是否主键列。
		/// </summary>
		public bool   IsPrimaryKey
		{
			get => _isPrimaryKey ?? false;
			set => _isPrimaryKey = value;
		}

        /// <summary>
        /// 是否主键列
        public bool HasIsPrimaryKey() => _isPrimaryKey.HasValue;

		/// <summary>
		/// 主键顺序，用于复合主键。
		/// </summary>
		public int PrimaryKeyOrder { get; set; }

		private bool? _canBeNull;
		/// <summary>
		/// 可空
		/// </summary>
		public  bool   CanBeNull
		{
			get => _canBeNull ?? true;
			set => _canBeNull = value;
		}
		/// <summary>
		/// 适用的业务版本，多个用逗号分隔。
		/// </summary>
		public string Edition { get; set; }
        /// <summary>
        /// 是否可空
        /// </summary>rns>
        public bool HasCanBeNull() => _canBeNull.HasValue;

		private int? _length;
		/// <summary>
		/// 长度
		/// </summary>
		public  int   Length
		{
			get => _length ?? 0;
			set => _length = value;
		}

        /// <summary>
        /// 是否设置了长度
        /// </summary>
        public bool HasLength() => _length.HasValue;

		private int? _precision;
		/// <summary>
		/// 精度
		/// </summary>
		public int   Precision
		{
			get => _precision ?? 0;
			set => _precision = value;
		}

        /// <summary>
        /// 是否设置了精度
        /// </summary>
        public bool HasPrecision() => _precision.HasValue;

		private int? _scale;
		/// <summary>
		/// 小数位长度。
		/// </summary>
		public int   Scale
		{
			get => _scale ?? 0;
			set => _scale = value;
		}

        /// <summary>
        /// 是否设置了小数位长度
        /// </summary>
        public bool HasScale() => _scale.HasValue;

		/// <summary>
		/// 自定义建表SQL格式，例如：varchar(50) not null。
		/// </summary>
		public string? CreateFormat { get; set; }

		private int? _order;
		/// <summary>
		/// 排序，用于生成SQL语句的字段顺序。例如：100表示排在前面，200表示排在后面。默认值为int.MaxValue，排在最后面。
		/// </summary>
		public int Order
		{
			get => _order ?? int.MaxValue;
			set => _order = value;
		}

        /// <summary>
        /// 是否设置了排序字段顺序
        /// </summary>
        public bool HasOrder() => _order.HasValue;

		public override string GetObjectID()
		{
#if NETFRAMEWORK
            return GetHashCode().ToString();

#endif
#if NET5_0_OR_GREATER
            return FormattableString.Invariant($".{Configuration}.{Name}.{MemberName}.{(int)DataType}.{DbType}.{(IsColumn?'1':'0')}.{Storage}.{(IsDiscriminator?'1':'0')}.{(SkipOnEntityFetch?'1':'0')}.{_skipOnInsert}.{_skipOnUpdate}.{_isIdentity}.{_isPrimaryKey}.{PrimaryKeyOrder}.{_canBeNull}.{_length}.{_precision}.{_scale}.{CreateFormat}.{_order}.");
#endif

        }
		/// <summary>
		/// 字段种类
		/// </summary>
		public FieldKind Kind { get; set; }
		/// <summary>
		/// 字段SQL别名，即as部分名称，例如：as 别名
		/// </summary>
		public string Alias { get; set; }
		/// <summary>
		/// 自定义字段SQL表达式，例如：自定义字段SQL表达式
		/// </summary>
		public string FreeSQL { get; set; }
		/// <summary>
		/// 来源的join中表名称，例如：来源的join中表名称.字段名 as 别名
		/// </summary>
		public string SrcTable { get; set; }
		/// <summary>
		/// 来源的join中字段名称，例如：来源的join中表名称.字段名 as 别名
		/// </summary>
		public string SrcField { get; set; }
		/// <summary>
		/// 绑定的代码表名称，为空时不使用，需要预定义代码表加载方式。
		/// </summary>
		public string Dict { get; set; }
		/// <summary>
		/// 排序字段索引
		/// </summary>
		public int OrderIdx { get; set; }
		/// <summary>
		/// 是否升序排序，默认为false。
		/// </summary>
		public bool Asc { get; set; }
		/// <summary>
		/// 是否降序排序，默认为false。
		/// </summary>
		public bool Desc { get; set; }
    }
}
