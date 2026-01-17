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
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitFieldWord(this);
        }
        internal static FieldWord All(TableSourceWord table)
		{
			return new FieldWord(table, "*", "*");
		}
        public DbDataType Type { get; set; }
        public string? Alias { get; set; }
        public string Name { get; set; } = null!; // not always true, see ColumnDescriptor notes
        public bool IsPrimaryKey { get; set; }
        public int PrimaryKeyOrder { get; set; }
        public bool IsIdentity { get; set; }
        public bool IsInsertable { get; set; }
        public bool IsUpdatable { get; set; }
        public bool IsDynamic { get; set; }
        public bool SkipOnEntityFetch { get; set; }
        public string? CreateFormat { get; set; }
        public int? CreateOrder { get; set; }

        public ITableNode? Table { get; set; }

        string? _physicalName;
        public string PhysicalName
        {
            get => _physicalName ?? Name;
            set => _physicalName = value;
        }

        internal static FieldWord All(ITableNode table)
		{
			return new FieldWord(table, "*", "*");
		}

		protected FieldWord(Type type = null) : base(ClauseType.SqlField, type)
        {

		}

		public FieldWord(ITableNode table, string name, Type type = null) : this(type)
		{
			Table     = table;
			Name      = name;
			CanBeNull = true;
		}

		public FieldWord(DbDataType dbDataType, string? name, bool canBeNull, Type type = null) : this(type)
        {
			Type      = dbDataType;
			Name      = name!;
			CanBeNull = canBeNull;
		}

		FieldWord(ITableNode table, string name, string physicalName, Type type = null) : this(type)
        {
			Table        = table;
			Name         = name;
			PhysicalName = physicalName;
			CanBeNull    = true;
		}

		public FieldWord(string name, string physicalName, Type type = null) : this(type)
        {
			Name         = name;
			PhysicalName = physicalName;
			CanBeNull    = true;
		}
        public FieldWord(EntityColumn col) : this(col.UnderType)
        {
            Name = col.DbColumnName;
            CanBeNull = true;
        }

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




		public EntityColumn  ColumnDescriptor  { get; set; } = null!; // TODO: not true, we probably should introduce something else for non-column fields

		public bool CanBeNull { get; set; }

		public override bool Equals(IExpWord other, Func<IExpWord, IExpWord, bool> comparer)
		{
			return this == other;
		}

		public override int Precedence => PrecedenceLv.Primary;

        public override Type SystemType => Type.SystemType;


        public override ClauseType NodeType => ClauseType.SqlField;

        public ITableNode BelongTable => Table;

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



		public static FieldWord FakeField(DbDataType dataType, string fieldName, bool canBeNull)
		{
			var field = new FieldWord(fieldName, fieldName);
			field.Type = dataType;
			return field;
		}
	}
}
