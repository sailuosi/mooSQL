using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;



namespace mooSQL.linq
{
	using Common;
	using Common.Internal;
	using Expressions;
	using Extensions;
	using Linq;
	using Mapping;

	using mooSQL.data;
	using mooSQL.data.model;
    using mooSQL.utils;
    using SqlProvider;
	using SqlQuery;

	public partial class Sql
	{
		private sealed class FieldsExprBuilderDirect : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
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

		private sealed class FieldNameBuilderDirect : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
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

		sealed class FieldNameBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var fieldExpr    = builder.GetExpression(0)!;
				var qualified    = builder.Arguments.Length <= 1 || builder.GetValue<bool>(1);
				var isExpression = builder.Member.Name == "FieldExpr";

				var field = QueryHelper.ExtractField(fieldExpr);
				if (field == null)
					throw new LinqToDBException($"Cannot convert expression {builder.Arguments[0]} to field.");

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

		
		[Extension("", BuilderType = typeof(FieldNameBuilderDirect), ServerSideOnly = true)]
		public static string FieldName<T>( ITable<T> table, Expression<Func<T, object>> fieldExpr)
			where T : notnull
		{
			return FieldName(table, fieldExpr, true);
		}

		
		[Extension("", BuilderType = typeof(FieldNameBuilderDirect), ServerSideOnly = true)]
		public static string FieldName<T>( ITable<T> table, Expression<Func<T, object>> fieldExpr, [SqlQueryDependent] bool qualified)
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

		[Flags]
		public enum TableQualification
		{
			None         = 0b00000000,
			TableName    = 0b00000001,
			DatabaseName = 0b00000010,
			SchemaName   = 0b00000100,
			ServerName   = 0b00001000,
			TableOptions = 0b00010000,

			Full         = TableName | DatabaseName | SchemaName | ServerName | TableOptions
		}

		[Extension("", BuilderType = typeof(FieldNameBuilderDirect), ServerSideOnly = true)]
		public static IExpWord FieldExpr<T, TV>( ITable<T> table, Expression<Func<T, TV>> fieldExpr)
			where T : notnull
		{
			return FieldExpr(table, fieldExpr, true);
		}

		[Extension("", BuilderType = typeof(FieldNameBuilderDirect), ServerSideOnly = true)]
		public static IExpWord FieldExpr<T, TV>( ITable<T> table, Expression<Func<T, TV>> fieldExpr, bool qualified)
			where T : notnull
		{
			var column = GetColumnFromExpression(typeof(T), fieldExpr, table.DBLive);


			return new ExpressionWord(column.DbColumnName, PrecedenceLv.Primary);
		}

		[Extension("", BuilderType = typeof(FieldsExprBuilderDirect), ServerSideOnly = false)]
		internal static IExpWord FieldsExpr<T>( ITable<T> table, Expression<Func<T, object?>> fieldsExpr)
			where T : notnull
		{
			return FieldsExpr(table, fieldsExpr, true);
		}

		[Extension("", BuilderType = typeof(FieldsExprBuilderDirect), ServerSideOnly = false)]
		internal static IExpWord FieldsExpr<T>( ITable<T> table, Expression<Func<T, object?>> fieldsExpr, bool qualified)
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
				throw new LinqToDBException($"Cannot extract columns info from expression {fieldExpr.Body}");

			var ed = DB.client.EntityCash.getEntityInfo(entityType); 
			var columns = new EntityColumn[init.Arguments.Count];
			for (var i = 0; i < init.Arguments.Count; i++)
			{
				var memberInfo = MemberHelper.GetMemberInfo(init.Arguments[i]);
				if (memberInfo == null)
					throw new LinqToDBException($"Cannot extract member info from expression {init.Arguments[i]}");

				var column = ed.GetColumn(memberInfo);

				columns[i] = column ?? throw new Exception($"找不到类的属性字段 {entityType.Name}.{memberInfo.Name}");
			}

			return columns;
		}

		private static EntityColumn GetColumnFromExpression(Type entityType, LambdaExpression fieldExpr, DBInstance DB)
		{
			var memberInfo = MemberHelper.GetMemberInfo(fieldExpr.Body);
			if (memberInfo == null)
				throw new LinqToDBException($"Cannot extract member info from expression {fieldExpr.Body}");

			var ed     = DB.client.EntityCash.getEntityInfo(entityType);
			var column = ed.GetColumn(memberInfo);

			if (column == null)
				throw new LinqToDBException($"Cannot find column for member {entityType.Name}.{memberInfo.Name}");
			return column;
		}

		
		[Extension("", BuilderType = typeof(FieldNameBuilder), ServerSideOnly = true)]
		public static string FieldName(object fieldExpr)
		{
			throw new LinqToDBException("'Sql.FieldName' is server side only method and used only for generating custom SQL parts");
		}

		
		[Extension("", BuilderType = typeof(FieldNameBuilder), ServerSideOnly = true)]
		public static string FieldName(object fieldExpr, bool qualified)
		{
			throw new LinqToDBException("'Sql.FieldName' is server side only method and used only for generating custom SQL parts");
		}

		
		[Extension("", BuilderType = typeof(FieldNameBuilder), ServerSideOnly = true)]
		public static IExpWord FieldExpr(object fieldExpr)
		{
			throw new LinqToDBException("'Sql.FieldExpr' is server side only method and used only for generating custom SQL parts");
		}

		
		[Extension("", BuilderType = typeof(FieldNameBuilder), ServerSideOnly = true)]
		public static IExpWord FieldExpr(object fieldExpr, bool qualified)
		{
			throw new LinqToDBException("'Sql.FieldExpr' is server side only method and used only for generating custom SQL parts");
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
			private readonly ITable<T> _table;

			public TableHelper(ITable<T> table)
			{
				_table = table;
			}

			public override string?      ServerName   => _table.ServerName;
			public override string?      DatabaseName => _table.DatabaseName;
			public override string?      SchemaName   => _table.SchemaName;
			public override string       TableName    => _table.TableName;
			public override TableOptions TableOptions => _table.TableOptions;
		}

		private sealed class TableNameBuilderDirect : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
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

		private sealed class TableNameBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var tableExpr = builder.GetExpression(0);
				var sqlTable  = QueryHelper.ExtractSqlTable(tableExpr);

				//TODO: review, maybe we need here TableSource
				if (sqlTable == null)
					throw new LinqToDBException("Cannot find Table associated with expression");

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

		private sealed class TableSourceBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				builder.ResultExpression = AliasPlaceholderWord.Instance;
			}
		}

		private sealed class TableOrColumnAsFieldBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var tableOrColumnExpr = builder.GetExpression(0)!;

				var anchor = new AnchorWord(tableOrColumnExpr, AnchorWord.AnchorKindEnum.TableAsSelfColumnOrField);

				builder.ResultExpression = anchor;
			}
		}

		private sealed class TableAsFieldBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var tableExpr = builder.GetExpression(0)!;

				var anchor = new AnchorWord(tableExpr, AnchorWord.AnchorKindEnum.TableAsSelfColumn);

				builder.ResultExpression = anchor;
			}
		}

		[ExpressionMethod(nameof(TableFieldIml))]
		public static TColumn TableField<TEntity, TColumn>( TEntity entity, string fieldName)
		{
			throw new LinqToDBException("'Sql.TableField' is server side only method and used only for generating custom SQL parts");
		}

		static Expression<Func<TEntity, string, TColumn>> TableFieldIml<TEntity, TColumn>()
		{
			return (entity, fieldName) => Property<TColumn>(entity, fieldName);
		}

		[Extension("", BuilderType = typeof(TableOrColumnAsFieldBuilder))]
		internal static TColumn TableOrColumnAsField<TColumn>( object? entityOrColumn)
		{
			throw new LinqToDBException("'Sql.TableOrColumnAsField' is server side only method and used only for generating custom SQL parts");
		}

		[Extension("", BuilderType = typeof(TableAsFieldBuilder))]
		internal static TColumn TableAsField<TEntity, TColumn>( TEntity entity)
		{
			throw new LinqToDBException("'Sql.TableAsField' is server side only method and used only for generating custom SQL parts");
		}

		[Extension("", BuilderType = typeof(TableNameBuilderDirect))]
		public static string TableName<T>( ITable<T> table)
			where T : notnull
		{
			return TableName(table, TableQualification.Full);
		}

		[Extension("", BuilderType = typeof(TableNameBuilderDirect))]
		public static string TableName<T>( ITable<T> table, [SqlQueryDependent] TableQualification qualification)
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

		[Extension("", BuilderType = typeof(TableNameBuilder), ServerSideOnly = true)]
		public static string TableName(object tableExpr)
		{
			throw new LinqToDBException("'Sql.TableName' is server side only method and used only for generating custom SQL parts");
		}

		[Extension("", BuilderType = typeof(TableNameBuilder), ServerSideOnly = true)]
		public static string TableName(object tableExpr, [SqlQueryDependent] TableQualification qualification)
		{
			throw new LinqToDBException("'Sql.TableName' is server side only method and used only for generating custom SQL parts");
		}

		[Extension("", BuilderType = typeof(TableNameBuilderDirect))]
		public static IExpWord TableExpr<T>( ITable<T> table)
			where T : notnull
		{
			return TableExpr(table, TableQualification.Full);
		}

		[Extension("", BuilderType = typeof(TableNameBuilderDirect))]
		public static IExpWord TableExpr<T>( ITable<T> table, [SqlQueryDependent] TableQualification qualification)
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

		[Extension("", BuilderType = typeof(TableNameBuilder), ServerSideOnly = true)]
		public static IExpWord TableExpr(object tableExpr)
		{
			throw new LinqToDBException("'Sql.TableExpr' is server side only method and used only for generating custom SQL parts");
		}

		[Extension("", BuilderType = typeof(TableNameBuilder), ServerSideOnly = true)]
		public static IExpWord TableExpr(object tableExpr, [SqlQueryDependent] TableQualification qualification)
		{
			throw new LinqToDBException("'Sql.TableExpr' is server side only method and used only for generating custom SQL parts");
		}

		class AliasExprBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				builder.ResultExpression = AliasPlaceholderWord.Instance;
			}
		}

		/// <summary>
		/// Useful for specifying place of alias when using <see ="DataExtensions.FromSql{TEntity}(IDataContext, RawSqlString, object?[])"/> method.
		/// </summary>
		/// <remarks>
		///		If <see ="DataExtensions.FromSql{TEntity}(IDataContext, RawSqlString, object?[])"/> contains at least one <see cref="AliasExpr"/>,
		///		automatic alias for the query will be not generated.
		/// </remarks>
		/// <returns>ISqlExpression which is Alias Placeholder.</returns>
		/// <example>
		/// The following <see ="DataExtensions.FromSql{TEntity}(IDataContext, RawSqlString, object?[])"/> calls are equivalent.
		/// <code>
		/// db.FromSql&lt;int&gt;($"select 1 as value from TableA {Sql.AliasExpr()}")
		/// db.FromSql&lt;int&gt;($"select 1 as value from TableA")
		/// </code>
		/// </example>
		[Extension(builderType: typeof(AliasExprBuilder), ServerSideOnly = true)]
		public static IExpWord AliasExpr() => AliasPlaceholderWord.Instance;

		sealed class ExprBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				Linq.Builder.TableBuilder.PrepareRawSqlArguments(builder.Arguments[0],
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
                        ExpressionAttribute.ToParametersNullabilityType(builder.IsNullable),
						builder.CanBeNull,
						sqlArguments!);
				}
			}
		}

		[Extension("", BuilderType = typeof(ExprBuilder), ServerSideOnly = true)]
		
		public static T Expr<T>(FormattableString sql)
		{
			throw new LinqToDBException("'Sql.Expr' is server side only method and used only for generating custom SQL parts");
		}

		[Extension("", BuilderType = typeof(ExprBuilder), ServerSideOnly = true)]
		
		public static T Expr<T>(
			[SqlQueryDependent]              RawSqlString sql,
			[SqlQueryDependentParams] params object[]     parameters
			)
		{
			throw new LinqToDBException("'Sql.Expr' is server side only method and used only for generating custom SQL parts");
		}

	}
}
