using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using mooSQL.linq.SqlQuery;



namespace mooSQL.linq
{
	using mooSQL.linq.utils;
	using mooSQL.linq.expressions;
	using mooSQL.linq.mapping;
	using mooSQL.data;
	using mooSQL.data.model;
    using mooSQL.utils;
    using mooSQL.linq.provider;
	using mooSQL.linq.clause;
	public static partial class SooFunctionExtension
	{
		private sealed class FieldsExprBuilderDirect : DbFunc.IExtensionCallBuilder
		{
			public void Build(DbFunc.ISqExtensionBuilder builder)
			{
				var fieldsExpr = (LambdaExpression)builder.Arguments[1].Unwrap();
				var qualified = builder.Arguments.Length <= 2 || builder.GetValue<bool>(2);

				var columns = GetColumnsFromExpression(((MethodInfo)builder.Member).GetGenericArguments()[0], fieldsExpr, builder.DBLive);

				var columnExpressions = new IExpWord[columns.Length];

				for (var i = 0; i < columns.Length; i++)
					columnExpressions[i] = new global::mooSQL.data.model.ExpressionWord(columns[i].DbColumnName, global::mooSQL.data.model.PrecedenceLv.Primary);

				if (columns.Length == 1)
					builder.ResultExpression = columnExpressions[0];
				else
					builder.ResultExpression = new ExpressionWord(
						string.Join(", ", Enumerable.Range(0, columns.Length).Select(i => $"{{{i}}}")),
                        PrecedenceLv.Primary,
						columnExpressions);
			}
		}

		private sealed class FieldNameBuilderDirect : DbFunc.IExtensionCallBuilder
		{
			public void Build(DbFunc.ISqExtensionBuilder builder)
			{
				var fieldExpr    = (LambdaExpression) builder.Arguments[1].Unwrap();
				var qualified    = builder.Arguments.Length <= 2 || builder.GetValue<bool>(2);
				var isExpression = builder.Member.Name == "FieldExpr";

				var column = GetColumnFromExpression(((MethodInfo)builder.Member).GetGenericArguments()[0], fieldExpr, builder.DBLive);

				if (isExpression)
				{
					builder.ResultExpression = new ExpressionWord(typeof(string), column.DbColumnName, PrecedenceLv.Primary);
				}
				else
				{
					var name = column.DbColumnName;

					if (qualified)
						name = builder.DBLive.dialect.clauseTranslator.TranslateValue(name, ConvertType.NameToQueryField);

					builder.ResultExpression = new ValueWord(name);
				}
			}
		}

		sealed class FieldNameBuilder : DbFunc.IExtensionCallBuilder
		{
			public void Build(DbFunc.ISqExtensionBuilder builder)
			{
				var fieldExpr    = builder.GetExpression(0)!;
				var qualified    = builder.Arguments.Length <= 1 || builder.GetValue<bool>(1);
				var isExpression = builder.Member.Name == "FieldExpr";

				var field = QueryHelper.ExtractField(fieldExpr);
				if (field == null)
					throw new SooQueryException($"Cannot convert expression {builder.Arguments[0]} to field.");

				if (isExpression)
				{
					builder.ResultExpression = qualified
						? new ExpressionWord(typeof(string), "{0}", PrecedenceLv.Primary, new FieldWord(field))
						: new ExpressionWord(typeof(string), field.PhysicalName, PrecedenceLv.Primary);
				}
				else
				{
					var name = field.PhysicalName;

					if (qualified)
						name = builder.DBLive.dialect.clauseTranslator.TranslateValue(name, ConvertType.NameToQueryField);

					builder.ResultExpression = new ValueWord(name);
				}
			}
		}
		/// <summary>
		/// FieldName&lt;T&gt;
		/// </summary>

		
		[Extension("", BuilderType = typeof(FieldNameBuilderDirect), ServerSideOnly = true)]
		public static string FieldName<T>( IDbQuery<T> table, Expression<Func<T, object>> fieldExpr)
			where T : notnull
		{
			return FieldName(table, fieldExpr, true);
		}
		/// <summary>
		/// FieldName&lt;T&gt;
		/// </summary>

		
		[Extension("", BuilderType = typeof(FieldNameBuilderDirect), ServerSideOnly = true)]
		public static string FieldName<T>( IDbQuery<T> table, Expression<Func<T, object>> fieldExpr, [SqlQueryDependent] bool qualified)
			where T : notnull
		{
			var column = GetColumnFromExpression(typeof(T), fieldExpr, table.DBLive);

			var result = column.DbColumnName;
			if (qualified)
			{
				result         = table.DBLive.dialect.clauseTranslator.TranslateValue(result, ConvertType.NameToQueryField);
			}

			return result;
		}
		/// <summary>
		/// TableQualification
		/// </summary>

		[Flags]
		public enum TableQualification
		{
			/// <summary>
			/// None
			/// </summary>
			/// <summary>
			/// None
			/// </summary>
			None         = 0b00000000,
			/// <summary>
			/// TableName
			/// </summary>
			/// <summary>
			/// TableName
			/// </summary>
			TableName    = 0b00000001,
			/// <summary>
			/// DatabaseName
			/// </summary>
			/// <summary>
			/// DatabaseName
			/// </summary>
			DatabaseName = 0b00000010,
			/// <summary>
			/// SchemaName
			/// </summary>
			/// <summary>
			/// SchemaName
			/// </summary>
			SchemaName   = 0b00000100,
			/// <summary>
			/// ServerName
			/// </summary>
			/// <summary>
			/// ServerName
			/// </summary>
			ServerName   = 0b00001000,
			/// <summary>
			/// TableOptions
			/// </summary>
			/// <summary>
			/// TableOptions
			/// </summary>
			TableOptions = 0b00010000,
			/// <summary>
			/// Full
			/// </summary>

			Full         = TableName | DatabaseName | SchemaName | ServerName | TableOptions
		}
		/// <summary>
		/// FieldExpr&lt;T, TV&gt;
		/// </summary>

		[Extension("", BuilderType = typeof(FieldNameBuilderDirect), ServerSideOnly = true)]
		public static IExpWord FieldExpr<T, TV>( IDbQuery<T> table, Expression<Func<T, TV>> fieldExpr)
			where T : notnull
		{
			return FieldExpr(table, fieldExpr, true);
		}
		/// <summary>
		/// FieldExpr&lt;T, TV&gt;
		/// </summary>

		[Extension("", BuilderType = typeof(FieldNameBuilderDirect), ServerSideOnly = true)]
		public static IExpWord FieldExpr<T, TV>( IDbQuery<T> table, Expression<Func<T, TV>> fieldExpr, bool qualified)
			where T : notnull
		{
			var column = GetColumnFromExpression(typeof(T), fieldExpr, table.DBLive);


			return new ExpressionWord(column.DbColumnName, PrecedenceLv.Primary);
		}

		[Extension("", BuilderType = typeof(FieldsExprBuilderDirect), ServerSideOnly = false)]
		internal static IExpWord FieldsExpr<T>( IDbQuery<T> table, Expression<Func<T, object?>> fieldsExpr)
			where T : notnull
		{
			return FieldsExpr(table, fieldsExpr, true);
		}

		[Extension("", BuilderType = typeof(FieldsExprBuilderDirect), ServerSideOnly = false)]
		internal static IExpWord FieldsExpr<T>( IDbQuery<T> table, Expression<Func<T, object?>> fieldsExpr, bool qualified)
			where T : notnull
		{
			var columns = GetColumnsFromExpression(typeof(T), fieldsExpr, table.DBLive);

			var columnExpressions = new IExpWord[columns.Length];

			for (var i = 0; i < columns.Length; i++)
				columnExpressions[i] = new global::mooSQL.data.model.ExpressionWord(columns[i].DbColumnName, global::mooSQL.data.model.PrecedenceLv.Primary);

			if (columns.Length == 1)
				return columnExpressions[0];

			return new ExpressionWord(
				string.Join(", ", Enumerable.Range(0, columns.Length).Select(i => $"{{{i}}}")),
                PrecedenceLv.Primary,
				columnExpressions);
		}

		private static EntityColumn[] GetColumnsFromExpression(Type entityType, LambdaExpression fieldExpr, DBInstance DB)
		{
			if (!(fieldExpr.Body is NewExpression init))
				return new[] { GetColumnFromExpression(entityType, fieldExpr, DB) };

			if (init.Arguments == null || init.Arguments.Count == 0)
				throw new SooQueryException($"Cannot extract columns info from expression {fieldExpr.Body}");

			var ed = DB.client.EntityCash.getEntityInfo(entityType); 
			var columns = new EntityColumn[init.Arguments.Count];
			for (var i = 0; i < init.Arguments.Count; i++)
			{
				var memberInfo = MemberHelper.GetMemberInfo(init.Arguments[i]);
				if (memberInfo == null)
					throw new SooQueryException($"Cannot extract member info from expression {init.Arguments[i]}");

				var column = ed.GetColumn(memberInfo);

				columns[i] = column ?? throw new Exception($"找不到类的属性字段 {entityType.Name}.{memberInfo.Name}");
			}

			return columns;
		}

		private static EntityColumn GetColumnFromExpression(Type entityType, LambdaExpression fieldExpr, DBInstance DB)
		{
			var memberInfo = MemberHelper.GetMemberInfo(fieldExpr.Body);
			if (memberInfo == null)
				throw new SooQueryException($"Cannot extract member info from expression {fieldExpr.Body}");

			var ed     = DB.client.EntityCash.getEntityInfo(entityType);
			var column = ed.GetColumn(memberInfo);

			if (column == null)
				throw new SooQueryException($"Cannot find column for member {entityType.Name}.{memberInfo.Name}");
			return column;
		}
		/// <summary>
		/// FieldName
		/// </summary>

		
		[Extension("", BuilderType = typeof(FieldNameBuilder), ServerSideOnly = true)]
		public static string FieldName(object fieldExpr)
		{
			throw new SooQueryException("'DbFunc.FieldName' is server side only method and used only for generating custom SQL parts");
		}
		/// <summary>
		/// FieldName
		/// </summary>

		
		[Extension("", BuilderType = typeof(FieldNameBuilder), ServerSideOnly = true)]
		public static string FieldName(object fieldExpr, bool qualified)
		{
			throw new SooQueryException("'DbFunc.FieldName' is server side only method and used only for generating custom SQL parts");
		}
		/// <summary>
		/// FieldExpr
		/// </summary>

		
		[Extension("", BuilderType = typeof(FieldNameBuilder), ServerSideOnly = true)]
		public static IExpWord FieldExpr(object fieldExpr)
		{
			throw new SooQueryException("'DbFunc.FieldExpr' is server side only method and used only for generating custom SQL parts");
		}
		/// <summary>
		/// FieldExpr
		/// </summary>

		
		[Extension("", BuilderType = typeof(FieldNameBuilder), ServerSideOnly = true)]
		public static IExpWord FieldExpr(object fieldExpr, bool qualified)
		{
			throw new SooQueryException("'DbFunc.FieldExpr' is server side only method and used only for generating custom SQL parts");
		}

		private abstract class TableHelper
		{
			public abstract string?      ServerName   { get; }
			public abstract string?      DatabaseName { get; }
			public abstract string?      SchemaName   { get; }
			public abstract string       TableName    { get; }
			public abstract TableOptions TableOptions { get; }
		}

		private sealed class TableHelper<T> : TableHelper
			where T : notnull
		{
			private readonly IDbQuery<T> _table;

			public TableHelper(IDbQuery<T> table)
			{
				_table = table;
			}

			public override string?      ServerName   => _table.ServerName;
			public override string?      DatabaseName => _table.DatabaseName;
			public override string?      SchemaName   => _table.SchemaName;
			public override string       TableName    => _table.TableName;
			public override TableOptions TableOptions => _table.TableOptions;
		}

		private sealed class TableNameBuilderDirect : DbFunc.IExtensionCallBuilder
		{
			public void Build(DbFunc.ISqExtensionBuilder builder)
			{
				var tableExpr    = builder.EvaluateExpression(builder.Arguments[0]);
				var tableType    = ((MethodInfo)builder.Member).GetGenericArguments()[0];
				var helperType   = typeof(TableHelper<>).MakeGenericType(tableType);
				var tableHelper  = (TableHelper)Activator.CreateInstance(helperType, tableExpr)!;
				var qualified    = builder.Arguments.Length <= 1 ? TableQualification.Full : builder.GetValue<TableQualification>(1);
				var isExpression = builder.Member.Name == "TableExpr";

				if (isExpression)
				{
					if (qualified == TableQualification.None)
						builder.ResultExpression = new ExpressionWord(typeof(string), tableHelper.TableName, PrecedenceLv.Primary);
					else
					{
						var tableName = new SqlObjectName(
							tableHelper.TableName,
							Server  : (qualified & TableQualification.ServerName)   != 0 ? tableHelper.ServerName   : null,
							Database: (qualified & TableQualification.DatabaseName) != 0 ? tableHelper.DatabaseName : null,
							Schema  : (qualified & TableQualification.SchemaName)   != 0 ? tableHelper.SchemaName   : null);
						var tbEn = builder.DBLive.client.EntityCash.getEntityInfo(tableType);
						var table = new TableWord(tbEn)
						{
							TableName    = tableName,
							TableOptions = (qualified & TableQualification.TableOptions) != 0 ? tableHelper.TableOptions : TableOptions.NotSet,
						};

						builder.ResultExpression = table;
					}
				}
				else
				{
					var name = tableHelper.TableName;

					if (qualified != TableQualification.None)
					{
						using var sb = Pools.StringBuilder.Allocate();
						var t= builder.DBLive.dialect.clauseTranslator.TranslateObjectName(
							
							new SqlObjectName(
								name,
								Server  : (qualified & TableQualification.ServerName)   != 0 ? tableHelper.ServerName   : null,
								Database: (qualified & TableQualification.DatabaseName) != 0 ? tableHelper.DatabaseName : null,
								Schema  : (qualified & TableQualification.SchemaName)   != 0 ? tableHelper.SchemaName   : null),
							ConvertType.NameToQueryTable,
							true,
							(qualified & TableQualification.TableOptions) != 0 ? tableHelper.TableOptions : TableOptions.NotSet);
						sb.Value.Append(t);
						name = sb.Value.ToString();
					}

					builder.ResultExpression = new ValueWord(name);
				}
			}
		}

		private sealed class TableNameBuilder : DbFunc.IExtensionCallBuilder
		{
			public void Build(DbFunc.ISqExtensionBuilder builder)
			{
				var tableExpr = builder.GetExpression(0);
				var sqlTable  = QueryHelper.ExtractSqlTable(tableExpr);

				//TODO: review, maybe we need here TableSource
				if (sqlTable == null)
					throw new SooQueryException("Cannot find Table associated with expression");

				var qualified    = builder.Arguments.Length <= 1 ? TableQualification.Full : builder.GetValue<TableQualification>(1);
				var isExpression = builder.Member.Name == "TableExpr";

				var name = sqlTable.TableName.Name;

				if (qualified != TableQualification.None)
				{
					using var sb = Pools.StringBuilder.Allocate();

					var t= builder.DBLive.dialect.clauseTranslator.TranslateObjectName(
						new SqlObjectName(
							sqlTable.TableName.Name,
							Server  : (qualified & TableQualification.ServerName)   != 0 ? sqlTable.TableName.Server       : null,
							Database: (qualified & TableQualification.DatabaseName) != 0 ? sqlTable.TableName.Database     : null,
							Schema  : (qualified & TableQualification.SchemaName)   != 0 ? sqlTable.TableName.Schema       : null),
						sqlTable.SqlTableType == SqlTableType.Function ? ConvertType.NameToProcedure : ConvertType.NameToQueryTable,
						true,
						(qualified & TableQualification.TableOptions) != 0 ? sqlTable.TableOptions : TableOptions.NotSet);
					sb.Value.Append(t);
					name = sb.Value.ToString();
				}

				builder.ResultExpression = isExpression
					? new global::mooSQL.data.model.ExpressionWord(name, global::mooSQL.data.model.PrecedenceLv.Primary)
					: new global::mooSQL.data.model.ValueWord(name);
			}
		}

		private sealed class TableSourceBuilder : DbFunc.IExtensionCallBuilder
		{
			public void Build(DbFunc.ISqExtensionBuilder builder)
			{
				builder.ResultExpression = AliasPlaceholderWord.Instance;
			}
		}

		private sealed class TableOrColumnAsFieldBuilder : DbFunc.IExtensionCallBuilder
		{
			public void Build(DbFunc.ISqExtensionBuilder builder)
			{
				var tableOrColumnExpr = builder.GetExpression(0)!;

				var anchor = new AnchorWord(tableOrColumnExpr, AnchorWord.AnchorKindEnum.TableAsSelfColumnOrField);

				builder.ResultExpression = anchor;
			}
		}

		private sealed class TableAsFieldBuilder : DbFunc.IExtensionCallBuilder
		{
			public void Build(DbFunc.ISqExtensionBuilder builder)
			{
				var tableExpr = builder.GetExpression(0)!;

				var anchor = new AnchorWord(tableExpr, AnchorWord.AnchorKindEnum.TableAsSelfColumn);

				builder.ResultExpression = anchor;
			}
		}
		/// <summary>
		/// TableField&lt;TEntity, TColumn&gt;
		/// </summary>

		[ExpressionMethod(nameof(TableFieldIml))]
		public static TColumn TableField<TEntity, TColumn>( TEntity entity, string fieldName)
		{
			throw new SooQueryException("'DbFunc.TableField' is server side only method and used only for generating custom SQL parts");
		}

		static Expression<Func<TEntity, string, TColumn>> TableFieldIml<TEntity, TColumn>()
		{
			return (entity, fieldName) => DbFunc.Property<TColumn>(entity, fieldName);
		}

		[Extension("", BuilderType = typeof(TableOrColumnAsFieldBuilder))]
		internal static TColumn TableOrColumnAsField<TColumn>( object? entityOrColumn)
		{
			throw new SooQueryException("'DbFunc.TableOrColumnAsField' is server side only method and used only for generating custom SQL parts");
		}

		[Extension("", BuilderType = typeof(TableAsFieldBuilder))]
		internal static TColumn TableAsField<TEntity, TColumn>( TEntity entity)
		{
			throw new SooQueryException("'DbFunc.TableAsField' is server side only method and used only for generating custom SQL parts");
		}
		/// <summary>
		/// TableName&lt;T&gt;
		/// </summary>

		[Extension("", BuilderType = typeof(TableNameBuilderDirect))]
		public static string TableName<T>( IDbQuery<T> table)
			where T : notnull
		{
			return TableName(table, TableQualification.Full);
		}
		/// <summary>
		/// TableName&lt;T&gt;
		/// </summary>

		[Extension("", BuilderType = typeof(TableNameBuilderDirect))]
		public static string TableName<T>( IDbQuery<T> table, [SqlQueryDependent] TableQualification qualification)
			where T : notnull
		{
			var result = table.TableName;

			if (qualification != TableQualification.None)
			{
				//var sqlBuilder = table.DataContext.CreateSqlProvider();
				using var sb   = Pools.StringBuilder.Allocate();
				var t= table.DBLive.dialect.clauseTranslator.TranslateObjectName(
					new SqlObjectName(
						table.TableName,
						Server  : (qualification & TableQualification.ServerName)   != 0 ? table.ServerName   : null,
						Database: (qualification & TableQualification.DatabaseName) != 0 ? table.DatabaseName : null,
						Schema  : (qualification & TableQualification.SchemaName)   != 0 ? table.SchemaName   : null),
					ConvertType.NameToQueryTable,
					true,
					(qualification & TableQualification.TableOptions) != 0 ? table.TableOptions : TableOptions.NotSet);
				sb.Value.Append(t);
				result = sb.Value.ToString();
			}

			return result;
		}
		/// <summary>
		/// TableName
		/// </summary>

		[Extension("", BuilderType = typeof(TableNameBuilder), ServerSideOnly = true)]
		public static string TableName(object tableExpr)
		{
			throw new SooQueryException("'DbFunc.TableName' is server side only method and used only for generating custom SQL parts");
		}
		/// <summary>
		/// TableName
		/// </summary>

		[Extension("", BuilderType = typeof(TableNameBuilder), ServerSideOnly = true)]
		public static string TableName(object tableExpr, [SqlQueryDependent] TableQualification qualification)
		{
			throw new SooQueryException("'DbFunc.TableName' is server side only method and used only for generating custom SQL parts");
		}
		/// <summary>
		/// TableExpr&lt;T&gt;
		/// </summary>

		[Extension("", BuilderType = typeof(TableNameBuilderDirect))]
		public static IExpWord TableExpr<T>( IDbQuery<T> table)
			where T : notnull
		{
			return TableExpr(table, TableQualification.Full);
		}
		/// <summary>
		/// TableExpr&lt;T&gt;
		/// </summary>

		[Extension("", BuilderType = typeof(TableNameBuilderDirect))]
		public static IExpWord TableExpr<T>( IDbQuery<T> table, [SqlQueryDependent] TableQualification qualification)
			where T : notnull
		{
			var name = table.TableName;

			if (qualification != TableQualification.None)
			{
				var sqlBuilder = table.DBLive.dialect.clauseTranslator;
				using var sb   = Pools.StringBuilder.Allocate();

			    var t=sqlBuilder.TranslateObjectName(
					new SqlObjectName(
						table.TableName,
						Server  : (qualification & TableQualification.ServerName)   != 0 ? table.ServerName   : null,
						Database: (qualification & TableQualification.DatabaseName) != 0 ? table.DatabaseName : null,
						Schema  : (qualification & TableQualification.SchemaName)   != 0 ? table.SchemaName   : null),
					ConvertType.NameToQueryTable,
					true,
					(qualification & TableQualification.TableOptions) != 0 ? table.TableOptions : TableOptions.NotSet);
				sb.Value.Append(t);
				name = sb.Value.ToString();
			}

			return new ExpressionWord(name, PrecedenceLv.Primary);
		}
		/// <summary>
		/// TableExpr
		/// </summary>

		[Extension("", BuilderType = typeof(TableNameBuilder), ServerSideOnly = true)]
		public static IExpWord TableExpr(object tableExpr)
		{
			throw new SooQueryException("'DbFunc.TableExpr' is server side only method and used only for generating custom SQL parts");
		}
		/// <summary>
		/// TableExpr
		/// </summary>

		[Extension("", BuilderType = typeof(TableNameBuilder), ServerSideOnly = true)]
		public static IExpWord TableExpr(object tableExpr, [SqlQueryDependent] TableQualification qualification)
		{
			throw new SooQueryException("'DbFunc.TableExpr' is server side only method and used only for generating custom SQL parts");
		}

		class AliasExprBuilder : DbFunc.IExtensionCallBuilder
		{
			public void Build(DbFunc.ISqExtensionBuilder builder)
			{
				builder.ResultExpression = AliasPlaceholderWord.Instance;
			}
		}

		/// <summary>
		/// 在 FromSql 中指定别名占位位置。
		/// </summary>
		/// <remarks>
		/// 若 FromSql 中至少包含一个 <see cref="AliasExpr"/>，则不会自动生成查询别名。
		/// </remarks>
		/// <returns>别名占位的 SQL 表达式。</returns>
		/// <example>
		/// 以下 FromSql 调用等价：
		/// <code>
		/// db.FromSql&lt;int&gt;($"select 1 as value from TableA {DbFunc.AliasExpr()}")
		/// db.FromSql&lt;int&gt;($"select 1 as value from TableA")
		/// </code>
		/// </example>
		[Extension(builderType: typeof(AliasExprBuilder), ServerSideOnly = true)]
		public static IExpWord AliasExpr() => AliasPlaceholderWord.Instance;

		sealed class ExprBuilder : DbFunc.IExtensionCallBuilder
		{
			public void Build(DbFunc.ISqExtensionBuilder builder)
			{
				mooSQL.linq.builder.TableRawSqlHelper.PrepareRawSqlArguments(builder.Arguments[0],
					builder.Arguments.Length > 1 ? builder.Arguments[1] : null,
					out var format, out var arguments);

				var memberType = builder.Member.GetMemberType();

				var sqlArguments = arguments.Select(e => builder.ConvertExpressionToSql(e)).ToArray();

				if (sqlArguments.Any(a => a == null))
					builder.IsConvertible = false;
				else
				{
					builder.ResultExpression = new ExpressionWord(
						memberType,
						format,
                        PrecedenceLv.Primary,
						memberType == typeof(bool) ? SqlFlags.IsPredicate | SqlFlags.IsPure : SqlFlags.IsPure,
                        DbFunc.ExpressionAttribute.ToParametersNullabilityType(builder.IsNullable),
						builder.CanBeNull,
						sqlArguments!);
				}
			}
		}
		/// <summary>
		/// Expr&lt;T&gt;
		/// </summary>

		[Extension("", BuilderType = typeof(ExprBuilder), ServerSideOnly = true)]
		
		public static T Expr<T>(FormattableString sql)
		{
			throw new SooQueryException("'DbFunc.Expr' is server side only method and used only for generating custom SQL parts");
		}
		/// <summary>
		/// Expr&lt;T&gt;
		/// </summary>

		[Extension("", BuilderType = typeof(ExprBuilder), ServerSideOnly = true)]
		
		public static T Expr<T>(
			[SqlQueryDependent]              RawSqlString sql,
			[SqlQueryDependentParams] params object[]     parameters
			)
		{
			throw new SooQueryException("'DbFunc.Expr' is server side only method and used only for generating custom SQL parts");
		}

	}
}
