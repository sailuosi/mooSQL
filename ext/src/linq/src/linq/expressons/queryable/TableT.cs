using System.Linq.Expressions;
using System.Reflection;

namespace mooSQL.linq.Linq
{
	using Extensions;
	using mooSQL.linq.Expressions;
	using Mapping;
	using Reflection;
	using SqlQuery;
	using mooSQL.data;
	using mooSQL.data.model;

	public sealed class Table<T> : ExpressionQuery<T>, ITable<T>, ITableMutable<T>, ITable
		where T : notnull
	{
		public Table(DBInstance dataContext)
		{
			//var expression = typeof(T).IsScalar()
			//	? null
			//	: Expression.Call(Methods.LinqToDB.GetTable.MakeGenericMethod(typeof(T)),
			//		SqlQueryRootExpression.Create(dataContext, dataContext.GetType()));

			//InitTable(dataContext, expression, null);
		}

		internal Table(DBInstance dataContext, Table<T> basedOn)
		{
			Init(dataContext, basedOn.Expression);
			_tableOptions = basedOn.TableOptions;
			_tableID = basedOn.TableID;
			_name = basedOn._name;
		}


		public Table(DBInstance dataContext, Expression expression)
		{
			InitTable(dataContext, expression, null);
		}

		void InitTable(DBInstance dataContext, Expression? expression, EntityInfo? tableDescriptor)
		{


			Init(dataContext, expression);
			if (tableDescriptor == null) {
				tableDescriptor = DBLive.client.EntityCash.getEntityInfo<T>();
			}
			if (tableDescriptor != null) { 
				//_name         = tableDescriptor.DbTableName;
				//_tableOptions = ed.TableOptions;			
			}

		}

		// ReSharper disable StaticMemberInGenericType
		static MethodInfo? _serverNameMethodInfo;
		static MethodInfo? _databaseNameMethodInfo;
		static MethodInfo? _schemaNameMethodInfo;
		static MethodInfo? _tableNameMethodInfo;
		static MethodInfo? _tableOptionsMethodInfo;
		static MethodInfo? _tableIDMethodInfo;
		// ReSharper restore StaticMemberInGenericType

		static Expression ApplyTableOptions(Expression expression, TableOptions tableOptions)
		{
			expression = Expression.Call(
				null,
				//_tableOptionsMethodInfo ??= Methods.LinqToDB.Table.TableOptions.MakeGenericMethod(typeof(T)),
				expression, Expression.Constant(tableOptions));
			return expression;
		}

		static Expression ApplyTableName(Expression expression, string? tableName)
		{
			expression = Expression.Call(
				null,
				//_tableNameMethodInfo ??= Methods.LinqToDB.Table.TableName.MakeGenericMethod(typeof(T)),
				expression, Expression.Constant(tableName));
			return expression;
		}

		static Expression ApplyDatabaseName(Expression expression, string? databaseName)
		{
			expression = Expression.Call(
				null,
				//_databaseNameMethodInfo ??= Methods.LinqToDB.Table.DatabaseName.MakeGenericMethod(typeof(T)),
				expression, Expression.Constant(databaseName));
			return expression;
		}

		static Expression ApplySchemaName(Expression expression, string? schemaName)
		{
			expression = Expression.Call(
				null,
				//_schemaNameMethodInfo ??= Methods.LinqToDB.Table.SchemaName.MakeGenericMethod(typeof(T)),
				expression, Expression.Constant(schemaName));
			return expression;
		}

		static Expression ApplyServerName(Expression expression, string? serverName)
		{
			expression = Expression.Call(
				null,
				//_serverNameMethodInfo ??= Methods.LinqToDB.Table.ServerName.MakeGenericMethod(typeof(T)),
				expression, Expression.Constant(serverName));
			return expression;
		}

		static Expression ApplyTaleId(Expression expression, string? id)
		{
			expression = Expression.Call(
				null,
				//_tableIDMethodInfo ??= Methods.LinqToDB.Table.TableID.MakeGenericMethod(typeof(T)),
				expression, Expression.Constant(id, typeof(string)));
			return expression;
		}

		private SqlObjectName _name;

		public  string?  ServerName
		{
			get => _name.Server;
			set
			{
				if (_name.Server != value)
				{
					Expression = ApplyServerName(Expression, value);

					_name .Server = value ;
				}
			}
		}

		public  string?  DatabaseName
		{
			get => _name.Database;
			set
			{
				if (_name.Database != value)
				{
					Expression = ApplyDatabaseName(Expression, value);

					_name . Database = value ;
				}
			}
		}

		public  string?  SchemaName
		{
			get => _name.Schema;
			set
			{
				if (_name.Schema != value)
				{
					Expression = ApplySchemaName(Expression, value);

					_name . Schema = value ;
				}
			}
		}

		private TableOptions _tableOptions;
		public  TableOptions  TableOptions
		{
			get => _tableOptions;
			set
			{
				if (_tableOptions != value)
				{
					Expression = ApplyTableOptions(Expression, value);

					_tableOptions = value;
				}
			}
		}

		public  string  TableName
		{
			get => _name.Name;
			set
			{
				if (_name.Name != value)
				{
					Expression = ApplyTableName(Expression, value);

					_name . Name = value ;
				}
			}
		}

		private string? _tableID;
		public string?   TableID
		{
			get => _tableID;
			set
			{
				if (_tableID != value)
				{
					Expression = ApplyTaleId(Expression, value);

					_tableID = value;
				}
			}
		}

		public ITable<T> ChangeServerName(string? serverName)
		{
			return new Table<T>(DBLive, this)
			{
				ServerName = serverName
			};
		}

		public ITable<T> ChangeDatabaseName(string? databaseName)
		{
			return new Table<T>(DBLive, this)
			{
				DatabaseName = databaseName
			};
		}

		public ITable<T> ChangeSchemaName(string? schemaName)
		{
			return new Table<T>(DBLive, this)
			{
				SchemaName = schemaName
			};
		}

		public ITable<T> ChangeTableName(string tableName)
		{
			return new Table<T>(DBLive, this)
			{
				TableName = tableName
			};
		}

		public ITable<T> ChangeTableOptions(TableOptions options)
		{
			return new Table<T>(DBLive, this)
			{
				TableOptions = options
			};
		}

		public ITable<T> ChangeTableID(string? tableID)
		{
			return new Table<T>(DBLive)
			{
				SchemaName   = SchemaName,
				ServerName   = ServerName,
				DatabaseName = DatabaseName,
				Expression   = Expression,
				TableName    = TableName,
				TableOptions = TableOptions,
				TableID      = tableID,
			};
		}

		#region Overrides


		#endregion
	}
}
