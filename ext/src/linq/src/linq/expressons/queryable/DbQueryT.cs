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

	public sealed class DbQuery<T> : ExpressionQuery<T>, IDbQuery<T>, IDbQueryMutable<T>, IDbQuery
		where T : notnull
	{
		public DbQuery(DBInstance dataContext)
		{
			var expression = Expression.Call(
				Methods.SooQuery.useQueryable.MakeGenericMethod(typeof(T)),
				SqlQueryRootExpression.Create(dataContext, dataContext.GetType()));

			InitTable(dataContext, expression, null);
		}

		internal DbQuery(DBInstance dataContext, DbQuery<T> basedOn)
		{
			Init(dataContext, basedOn.Expression);
			_tableOptions = basedOn.TableOptions;
			_tableID = basedOn.TableID;
			_name = basedOn._name;
		}


		public DbQuery(DBInstance dataContext, Expression expression)
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
				expression, Expression.Constant(tableOptions));
			return expression;
		}

		static Expression ApplyTableName(Expression expression, string? tableName)
		{
			expression = Expression.Call(
				null,
				expression, Expression.Constant(tableName));
			return expression;
		}

		static Expression ApplyDatabaseName(Expression expression, string? databaseName)
		{
			expression = Expression.Call(
				null,
				expression, Expression.Constant(databaseName));
			return expression;
		}

		static Expression ApplySchemaName(Expression expression, string? schemaName)
		{
			expression = Expression.Call(
				null,
				expression, Expression.Constant(schemaName));
			return expression;
		}

		static Expression ApplyServerName(Expression expression, string? serverName)
		{
			expression = Expression.Call(
				null,
				expression, Expression.Constant(serverName));
			return expression;
		}

		static Expression ApplyTaleId(Expression expression, string? id)
		{
			expression = Expression.Call(
				null,
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

		public IDbQuery<T> ChangeServerName(string? serverName)
		{
			return new DbQuery<T>(DBLive, this)
			{
				ServerName = serverName
			};
		}

		public IDbQuery<T> ChangeDatabaseName(string? databaseName)
		{
			return new DbQuery<T>(DBLive, this)
			{
				DatabaseName = databaseName
			};
		}

		public IDbQuery<T> ChangeSchemaName(string? schemaName)
		{
			return new DbQuery<T>(DBLive, this)
			{
				SchemaName = schemaName
			};
		}

		public IDbQuery<T> ChangeTableName(string tableName)
		{
			return new DbQuery<T>(DBLive, this)
			{
				TableName = tableName
			};
		}

		public IDbQuery<T> ChangeTableOptions(TableOptions options)
		{
			return new DbQuery<T>(DBLive, this)
			{
				TableOptions = options
			};
		}

		public IDbQuery<T> ChangeTableID(string? tableID)
		{
			return new DbQuery<T>(DBLive)
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
