using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace mooSQL.data.model
{
	using Common;


	using mooSQL.data;
    using mooSQL.data.Mapping;
    using mooSQL.data.model;
	/// <summary>
	/// 代表某个实体类对应的表，含有实体映射信息。
	/// </summary>
	public class TableWord : ExpWordBase,ITableNode
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitTableWord(this);
        }
        #region Init

        public TableWord(Type objectType, int? sourceId, SqlObjectName tableName, Type type = null) : base(ClauseType.SqlTable, type)
        {
			SourceID   = sourceId ?? Interlocked.Increment(ref SelectQueryClause.SourceIDCounter);
			ObjectType = objectType;
			TableName  = tableName;
			_all       = FieldWord.All(this);
		}

		public TableWord(
			int                      id,
			string?                  expression,
			string                   alias,
			SqlObjectName            tableName,
			Type                     objectType,
			SequenceNameAttribute[]? sequenceAttributes,
			IEnumerable<FieldWord>    fields,
			SqlTableType             sqlTableType,
			IExpWord[]?        tableArguments,
			TableOptions             tableOptions,
			string?                  tableID)
			: this(objectType, id, tableName)
		{
			Expression         = expression;
			Alias              = alias;
			SequenceAttributes = sequenceAttributes;
			ID                 = tableID;

			AddRange(fields);

			SqlTableType   = sqlTableType;
			TableArguments = tableArguments;
			TableOptions   = tableOptions;

			_all ??= FieldWord.All(this);
		}

		#endregion

		#region Init from type

		public TableWord(EntityInfo entityDescriptor, string? physicalName = null)
			: this(entityDescriptor.Type, (int?)null, new(string.Empty))
		{
			var bl=physicalName != null && entityDescriptor.DbTableName != physicalName;
			TableName = new SqlObjectName(entityDescriptor.DbTableName,entityDescriptor.ServerName,entityDescriptor.DatabaseName,entityDescriptor.SchemaName);

            if (bl)
			{

				entityDescriptor.DbTableName = physicalName;

			}


			//TableOptions = entityDescriptor.TableOptions;

			foreach (var column in entityDescriptor.Columns)
			{
				var field = new FieldWord(this,column.DbColumnName);
				field.ColumnDescriptor = column;
				Add(field);

				if (field.Type.DataType == DataFam.Undefined)
				{
					//var dataType = entityDescriptor.MappingSchema.GetDataType(field.Type.SystemType);

					//if (dataType.Type.DataType == DataType.Undefined)
					//{
					//	dataType = entityDescriptor.MappingSchema.GetUnderlyingDataType(field.Type.SystemType, out var canBeNull);

					//	if (canBeNull)
					//		field.CanBeNull = true;
					//}

					//field.Type = field.Type.WithDataType(dataType.Type.DataType);

					//// try to get type from converter
					//if (field.Type.DataType == DataType.Undefined)
					//{
					//	try
					//	{
					//		var converter = entityDescriptor.MappingSchema.GetConverter(
					//			field.Type,
					//			new DbDataType(typeof(DataParameter)),
					//			true,
					//			ConversionType.ToDatabase);

					//		var parameter = converter?.ConvertValueToParameter(DefaultValue.GetValue(field.Type.SystemType, entityDescriptor.MappingSchema));
					//		if (parameter != null)
					//			field.Type = field.Type.WithDataType(parameter.DataType);
					//	}
					//	catch
					//	{
					//		// converter cannot handle default value?
					//	}
					//}

					if (field.Type.Length    == null) field.Type = field.Type.WithLength   (column.Length);
					if (field.Type.Precision == null) field.Type = field.Type.WithPrecision(column.Precision);
					if (field.Type.Scale     == null) field.Type = field.Type.WithScale    (column.Scale);
				}
			}

			var identityField = GetIdentityField();

			if (identityField != null)
			{
				var cd = entityDescriptor.FieldMap[identityField.Name]!;
				//SequenceAttributes = cd.SequenceName == null ? null : new[] { cd.SequenceName };
			}

			_all ??= FieldWord.All(this);
		}

		#endregion

		#region Init from Table

		public TableWord(TableWord table)
			: this(table.ObjectType, null, table.TableName)
		{
			Alias              = table.Alias;
			SequenceAttributes = table.SequenceAttributes;

			foreach (var field in table.Fields)
				Add(new FieldWord(field));

			SqlTableType       = table.SqlTableType;
			SqlQueryExtensions = table.SqlQueryExtensions;

			Expression         = table.Expression;
			TableArguments     = table.TableArguments;

			_all ??= FieldWord.All(this);
		}

		public TableWord(TableWord table, IEnumerable<FieldWord> fields, IExpWord[] tableArguments)
			: this(table.ObjectType, null, table.TableName)
		{
			Alias              = table.Alias;
			Expression         = table.Expression;
			SequenceAttributes = table.SequenceAttributes;
			TableOptions       = table.TableOptions;

			AddRange(fields);

			SqlTableType       = table.SqlTableType;
			TableArguments     = tableArguments;
			SqlQueryExtensions = table.SqlQueryExtensions;

			_all ??= FieldWord.All(this);
		}

		#endregion

		#region Overrides

		public override ClauseType NodeType => ClauseType.SqlTable;



		public override bool Equals(IExpWord? other, Func<IExpWord, IExpWord, bool> comparer)
		{
			if (ReferenceEquals(this, other))
				return true;

			if (other is not TableWord otherTable)
				return false;

			return ObjectType == otherTable.ObjectType &&
			       TableName  == otherTable.TableName  &&
			       Alias      == otherTable.Alias;
		}

		//public override bool CanBeNullable(NullabilityContext nullability) => CanBeNull;

		public override int Precedence => PrecedenceLv.Primary;
		//public override Type SystemType => ObjectType;
		
		#endregion

		#region Public Members

		/// <summary>
		/// Search for table field by mapping class member name.
		/// </summary>
		/// <param name="memberName">Mapping class member name.</param>
		public FieldWord? FindFieldByMemberName(string memberName)
		{
			_fieldsLookup.TryGetValue(memberName, out var field);
			return field;
		}

		public         string?           Alias          { get; set; }
		public virtual SqlObjectName     TableName      { get; set; }
		public         Type              ObjectType     { get; protected internal set; }
		public virtual SqlTableType      SqlTableType   { get; set; }
		public         TableOptions      TableOptions   { get; set; }
		public virtual string?           ID             { get; set; }

		public bool CanBeNull { get; set; } = true;

		/// <summary>
		/// Custom SQL expression format string (used together with <see cref="TableArguments"/>) to
		/// transform <see cref="TableWord"/> to custom table expression.
		/// Arguments:
		/// <list type="bullet">
		/// <item>{0}: <see cref="TableName"/></item>
		/// <item>{1}: <see cref="Alias"/></item>
		/// <item>{2+}: arguments from <see cref="TableArguments"/> (with index adjusted by 2)</item>
		/// </list>
		/// </summary>
		public string?           Expression     { get; set; }
		public IExpWord[]? TableArguments { get; set; }

        public string NameForLogging => Expression ?? TableName.Name;

		// list user to preserve order of fields in queries
		internal readonly List<FieldWord>              _orderedFields = new();
		readonly          Dictionary<string,FieldWord> _fieldsLookup  = new();

		public           List<FieldWord> Fields => _orderedFields;
		public List<QueryExtension>? SqlQueryExtensions { get; set; }

		// identity fields cached, as it is most used fields filter
		private readonly List<FieldWord>                  _identityFields = new ();
		public IReadOnlyList<FieldWord> IdentityFields => _identityFields;

		internal void ClearFields()
		{
			_fieldsLookup  .Clear();
			_orderedFields .Clear();
			_identityFields.Clear();
		}

		public SequenceNameAttribute[]? SequenceAttributes { get; protected set; }

		FieldWord?       _all;
		public FieldWord All => _all!;

		public FieldWord? GetIdentityField()
		{
			foreach (var field in Fields)
				if (field.IsIdentity)
					return field;

			var keys = GetKeys(true);

			if (keys?.Count == 1)
				return (FieldWord)keys[0];

			return null;
		}

		public void Add(FieldWord field)
		{
			//if (field.Table != null) throw new InvalidOperationException("Invalid parent table.");

			field.Table = this;

			if (field.Name == "*")
				_all = field;
			else if(field.Name != null)
			{
				_fieldsLookup.Add(field.Name, field);
				_orderedFields.Add(field);

				if (field.IsIdentity)
					_identityFields.Add(field);
			}
		}

		public void AddRange(IEnumerable<FieldWord> collection)
		{
			foreach (var item in collection)
				Add(item);
		}

		#endregion

		#region ISqlTableSource Members

		public int SourceID { get; }

        public string Name => TableName.Name;

        List<IExpWord>? _keyFields;
        public override Type SystemType => ObjectType;
        public virtual IList<IExpWord>? GetKeys(bool allIfEmpty)
		{
			_keyFields ??=
			(
				from f in Fields
				where f.IsPrimaryKey
				orderby f.PrimaryKeyOrder
				select f as IExpWord
			)
			.ToList();

			if (_keyFields.Count == 0 && allIfEmpty)
				return Fields.Select(f => f as IExpWord).ToList();

			return _keyFields;
		}

		#endregion

		#region System tables

		public static TableWord Inserted(EntityInfo entityDescriptor)
			=> new (entityDescriptor)
			{
				TableName    = new ("INSERTED"),
				SqlTableType = SqlTableType.SystemTable,
			};

        public static TableWord Deleted(EntityInfo entityDescriptor)
			=> new (entityDescriptor)
			{
				TableName    = new ("DELETED"),
				SqlTableType = SqlTableType.SystemTable,
			};

		#endregion



	}
}
