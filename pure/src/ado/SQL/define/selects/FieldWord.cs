using System;

namespace mooSQL.data.model
{
	using Common;
    using mooSQL.linq.SqlQuery;


    /// <summary>
    /// 代表字段选择，*表示全部
    /// </summary>
    public class FieldWord : ExpWordBase,IField
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitFieldWord(this);
        }
        internal static FieldWord All(TableSourceWord table)
		{
			return new FieldWord(table, "*", "*");
		}
        /// <summary>SQL/CLR 类型信息。</summary>
        public DbDataType Type { get; set; }
        /// <summary>列别名（SELECT 列表）。</summary>
        public string? Alias { get; set; }
        /// <summary>逻辑列名（映射名或物理名）。</summary>
        public string Name { get; set; } = null!; // not always true, see ColumnDescriptor notes
        /// <summary>是否为主键列。</summary>
        public bool IsPrimaryKey { get; set; }
        /// <summary>复合主键中的顺序。</summary>
        public int PrimaryKeyOrder { get; set; }
        /// <summary>是否为标识列。</summary>
        public bool IsIdentity { get; set; }
        /// <summary>是否可插入。</summary>
        public bool IsInsertable { get; set; }
        /// <summary>是否可更新。</summary>
        public bool IsUpdatable { get; set; }
        /// <summary>是否为动态/计算列。</summary>
        public bool IsDynamic { get; set; }
        /// <summary>实体加载时是否跳过。</summary>
        public bool SkipOnEntityFetch { get; set; }
        /// <summary>DDL 中列定义的格式模板。</summary>
        public string? CreateFormat { get; set; }
        /// <summary>建表时列顺序。</summary>
        public int? CreateOrder { get; set; }

        /// <summary>所属表节点。</summary>
        public ITableNode? Table { get; set; }

        string? _physicalName;
        /// <summary>物理列名（若与 <see cref="Name"/> 不同）。</summary>
        public string PhysicalName
        {
            get => _physicalName ?? Name;
            set => _physicalName = value;
        }

        internal static FieldWord All(ITableNode table)
		{
			return new FieldWord(table, "*", "*");
		}

		/// <summary>子类使用的受保护构造。</summary>
		protected FieldWord(Type type = null) : base(ClauseType.SqlField, type)
        {

		}

		/// <summary>绑定表与列名。</summary>
		public FieldWord(ITableNode table, string name, Type type = null) : this(type)
		{
			Table     = table;
			Name      = name;
			CanBeNull = true;
		}

		/// <summary>显式数据类型与可空性。</summary>
		public FieldWord(DbDataType dbDataType, string? name, bool canBeNull, Type type = null) : this(type)
        {
			Type      = dbDataType;
			Name      = name!;
			CanBeNull = canBeNull;
		}

		/// <summary>表、逻辑名与物理名。</summary>
		FieldWord(ITableNode table, string name, string physicalName, Type type = null) : this(type)
        {
			Table        = table;
			Name         = name;
			PhysicalName = physicalName;
			CanBeNull    = true;
		}

		/// <summary>无表上下文的列名对。</summary>
		public FieldWord(string name, string physicalName, Type type = null) : this(type)
        {
			Name         = name;
			PhysicalName = physicalName;
			CanBeNull    = true;
		}
        /// <summary>自实体列元数据构造。</summary>
        public FieldWord(EntityColumn col) : this(col.UnderType)
        {
            Name = col.DbColumnName;
            CanBeNull = true;
        }

        /// <summary>复制另一字段的元数据。</summary>
        public FieldWord(FieldWord field, Type type = null) : this(type)
        {
			Type             = field.Type;
			Alias            = field.Alias;
			Name             = field.Name;
			PhysicalName     = field.PhysicalName;
			CanBeNull        = field.CanBeNull;
			IsPrimaryKey     = field.IsPrimaryKey;
			PrimaryKeyOrder  = field.PrimaryKeyOrder;
			IsIdentity       = field.IsIdentity;
			IsInsertable     = field.IsInsertable;
			IsUpdatable      = field.IsUpdatable;
			CreateFormat     = field.CreateFormat;
			CreateOrder      = field.CreateOrder;
			//ColumnDescriptor = field.ColumnDescriptor;
			IsDynamic        = field.IsDynamic;
		}




		/// <summary>映射层列描述子。</summary>
		public EntityColumn  ColumnDescriptor  { get; set; } = null!; // TODO: not true, we probably should introduce something else for non-column fields

		/// <summary>是否允许 NULL。</summary>
		public bool CanBeNull { get; set; }

		/// <inheritdoc />
		public override bool Equals(IExpWord other, Func<IExpWord, IExpWord, bool> comparer)
		{
			return this == other;
		}

		/// <inheritdoc />
		public override int Precedence => PrecedenceLv.Primary;

        /// <inheritdoc />
        public override Type SystemType => Type.SystemType;


        /// <inheritdoc />
        public override ClauseType NodeType => ClauseType.SqlField;

        /// <inheritdoc />
        public ITableNode BelongTable => Table;

        /// <inheritdoc />
        public IElementWriter ToString(IElementWriter writer)
		{
			// writer.DebugAppendUniqueId(this);

			if (Table != null)
				writer
					.Append('t')
					.Append(Table.Name)
					.Append('.');

			writer.Append(Name);
			if (CanBeNull)
				writer.Append("?");
			return writer;
		}



		/// <summary>构造无表绑定的占位字段（用于类型推断等）。</summary>
		public static FieldWord FakeField(DbDataType dataType, string fieldName, bool canBeNull)
		{
			var field = new FieldWord(fieldName, fieldName);
			field.Type = dataType;
			return field;
		}
	}
}
