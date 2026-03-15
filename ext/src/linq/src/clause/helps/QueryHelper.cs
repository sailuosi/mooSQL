using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace mooSQL.linq.SqlQuery
{
	using SqlProvider;
	using Common;
	using Mapping;
	using Common.Internal;
	using mooSQL.data.model;
	using mooSQL.data;
    using mooSQL.data.model.affirms;

    public static partial class QueryHelper
	{
		internal static ObjectPool<SelectQueryOptimizerVisitor> SelectOptimizer =
			new(() => new SelectQueryOptimizerVisitor(), v => v.Cleanup(), 100);

		sealed class IsDependsOnSourcesContext
		{
			public IsDependsOnSourcesContext(IReadOnlyCollection<ITableNode> onSources, IReadOnlyCollection<ISQLNode>? elementsToIgnore)
			{
				OnSources = onSources;
				ElementsToIgnore = elementsToIgnore;
			}

			public readonly IReadOnlyCollection<ITableNode> OnSources;
			public readonly IReadOnlyCollection<ISQLNode>?  ElementsToIgnore;

			public          bool                     DependencyFound;
		}

		public static bool IsDependsOnSource(ISQLNode testedRoot, ITableNode onSource, IReadOnlyCollection<ISQLNode>? elementsToIgnore = null)
		{
			return IsDependsOnSources(testedRoot, new [] { onSource }, elementsToIgnore);
		}

		public static bool IsDependsOnSources(ISQLNode testedRoot, IReadOnlyCollection<ITableNode> onSources, IReadOnlyCollection<ISQLNode>? elementsToIgnore = null)
		{
			var ctx = new IsDependsOnSourcesContext(onSources, elementsToIgnore);

			testedRoot.VisitParentFirst(ctx, (Func<IsDependsOnSourcesContext, ISQLNode, bool>)(static (context, e) =>
			{
				if (context.DependencyFound)
					return false;

				if (context.ElementsToIgnore != null && context.ElementsToIgnore.Contains(e, SQLElement.ReferenceComparer))
					return false;

				if (e is ITableNode source && context.OnSources.Contains(source, SQLElement.ReferenceComparer))
				{
					context.DependencyFound = true;
					return false;
				}

				switch (e.NodeType)
				{
					case data.model.ClauseType.Column:
					{
						var c = (ColumnWord) e;
						if (context.OnSources.Contains(c.Parent!, SQLElement.ReferenceComparer))
							context.DependencyFound = true;
						break;
					}
					case data.model.ClauseType.SqlField:
					{
						var f = (FieldWord) e;
						if (context.OnSources.Contains(f.Table!, SQLElement.ReferenceComparer))
							context.DependencyFound = true;
						break;
					}
				}

				return !context.DependencyFound;
			}));

			return ctx.DependencyFound;
		}

		public static bool IsDependsOnOuterSources(
			ISQLNode                         testedRoot,
			ICollection<ISQLNode>?   elementsToIgnore = null,
			ICollection<ITableNode>? currentSources   = null)
		{
			var dependedOnSources = new List<ITableNode>();
			var foundSources = new List<ITableNode>();

			testedRoot.VisitParentFirst((elementsToIgnore, currentSources, dependedOnSources, foundSources), static (context, e) =>
			{
				if (context.elementsToIgnore?.Contains(e) == true)
					return false;

				switch (e)
				{
					case TableSourceWord ts:
						context.foundSources.Add(ts.Source);
						break;

					case SelectQueryClause sc:
						context.foundSources.Add(sc);
						break;

					case FieldWord field when field.Table != null:
						context.dependedOnSources.Add(field.Table);
						break;

					case ColumnWord column when column.Parent != null:
						context.dependedOnSources.Add(column.Parent);
						break;
				}

				return true;
			});

			var excepted = dependedOnSources.Except(foundSources);
			if (currentSources != null)
				excepted = excepted.Except(currentSources);

			var result = excepted.Any();
			return result;
		}

		public static bool HasTableInQuery(SelectQueryClause query, TableWord table)
		{
			return EnumerateAccessibleTables(query).Any(t => t == table);
		}

		public static bool IsSingleTableInQuery(SelectQueryClause query, TableWord table)
		{
            //[{ Joins.Count: 0, Source: var s }]
            if (query.From.Tables.Count==1 && query.From.Tables[0].GetJoins().Count==0
				&& query.From.Tables[0].FindISrc() == table)
			{
				return true;
			}
			return false;
		}

		sealed class IsDependsOnElementContext
		{
			public IsDependsOnElementContext(ISQLNode onElement, HashSet<ISQLNode>? elementsToIgnore)
			{
				OnElement = onElement;
				ElementsToIgnore = elementsToIgnore;
			}

			public readonly ISQLNode           OnElement;
			public readonly HashSet<ISQLNode>? ElementsToIgnore;

			public          bool                    DependencyFound;
		}





		/// <summary>
		/// Returns <see ="ColumnDescriptor"/> for <paramref name="expr"/>.
		/// </summary>
		/// <param name="expr">Tested SQL Expression.</param>
		/// <returns>Associated column descriptor or <c>null</c>.</returns>
		public static EntityColumn? GetColumnDescriptor(IExpWord? expr)
		{
			if (expr == null)
				return null;

			switch (expr.NodeType)
			{
				case ClauseType.Column:
				{
					return GetColumnDescriptor(((ColumnWord)expr).Expression);
				}
				case ClauseType.SqlField:
				{
					return ((FieldWord)expr).ColumnDescriptor;
				}
				case ClauseType.SqlExpression:
				{
					var sqlExpr = (ExpressionWord)expr;
					if (sqlExpr.Parameters.Length == 1 && sqlExpr.Expr == "{0}")
						return GetColumnDescriptor(sqlExpr.Parameters[0]);
					break;
				}
				case ClauseType.SqlQuery:
				{
					var query = (SelectQueryClause)expr;
					if (query.Select.Columns.Count == 1)
						return GetColumnDescriptor(query.Select.Columns[0]);
					break;
				}
				case ClauseType.SqlBinaryExpression:
				{
					var binary = (BinaryWord)expr;
					var found = GetColumnDescriptor(binary.Expr1) ?? GetColumnDescriptor(binary.Expr2);
					if (found?.UnderType != binary.SystemType)
						return null;
					return found;
				}
				case ClauseType.SqlNullabilityExpression:
				{
					var nullability = (NullabilityWord)expr;
					return GetColumnDescriptor(nullability.SqlExpression);
				}
				case ClauseType.SqlFunction:
				{
					var function = (FunctionWord)expr;

					//TODO: unify function names and put in common constant storage
					//For example it should be "$COALESCE$" and "$CASE$" do do not mix with user defined extension

					if (function.Name is "Coalesce" or PseudoFunctions.COALESCE && function.Parameters.Length == 2)
					{
						return GetColumnDescriptor(function.Parameters[0]);
					}
					break;
				}
				case ClauseType.SqlCondition:
				{
					var condition = (ConditionWord)expr;

					return GetColumnDescriptor(condition.TrueValue) ??
					       GetColumnDescriptor(condition.FalseValue);
				}
				case ClauseType.SqlCase:
				{
					var caseExpression = (CaseWord)expr;

					foreach (var caseItem in caseExpression.Cases)
					{
						var descriptor = GetColumnDescriptor(caseItem.ResultExpression);
						if (descriptor != null)
							return descriptor;
					}

					return GetColumnDescriptor(caseExpression.ElseExpression);
				}
			}
			return null;
		}

		public static DbDataType? SuggestDbDataType(IExpWord expr)
		{
			switch (expr.NodeType)
			{
				case ClauseType.Column:
				{
					var column = (ColumnWord)expr;

					var suggested = SuggestDbDataType(column.Expression);
					if (suggested != null)
						return suggested;

					if (column.Parent?.HasSetOperators == true)
					{
						var idx = column.Parent.Select.Columns.content.IndexOf(column);
						if (idx >= 0)
						{
							foreach (var setOperator in column.Parent.SetOperators)
							{
								suggested = SuggestDbDataType(setOperator.SelectQuery.Select.Columns[idx].Expression);
								if (suggested != null)
									return suggested;
							}
						}
					}

					break;
				}
				case ClauseType.SqlField:
				{
					return ((FieldWord)expr).ColumnDescriptor?.DbType;
				}
				case ClauseType.SqlExpression:
				{
					var sqlExpr = (ExpressionWord)expr;
					if (sqlExpr.Parameters.Length == 1 && sqlExpr.Expr == "{0}")
						return SuggestDbDataType(sqlExpr.Parameters[0]);
					break;
				}
				case ClauseType.SqlQuery:
				{
					var query = (SelectQueryClause)expr;
					if (query.Select.Columns.Count == 1)
						return SuggestDbDataType(query.Select.Columns[0]);
					break;
				}
				case ClauseType.SqlValue:
				{
					var sqlValue = (ValueWord)expr;
					if (sqlValue.ValueType.DbType != null || sqlValue.ValueType.DataType != DataFam.Undefined)
						return sqlValue.ValueType;
					break;
				}
			}

			return null;
		}

		public static DbDataType GetDbDataType(IExpWord expr, DBInstance mappingSchema)
		{
			var result = GetDbDataType(expr);
			if (result.DataType == DataFam.Undefined)
			{
				result = mappingSchema.dialect.mapping.GetDbDataType(expr.SystemType ?? typeof(object));
			}

			return result;
		}

		static DbDataType GetDbDataType(IExpWord? expr)
		{
			switch (expr)
			{
				case null: return DbDataType.Undefined;
				case ValueWord { ValueType: var vt }: return vt;

				case FieldWord            { Type: var t }: return t;
				case DataTypeWord         { Type: var t }: return t;
				case CastWord   { Type: var t }: return t;
				case BinaryWord { Type: var t }: return t;
				case FunctionWord         { Type: var t }: return t;        

				case ColumnWord                { Expression:    var e }: return GetDbDataType(e);
				case NullabilityWord { SqlExpression: var e }: return GetDbDataType(e);

				case SelectQueryClause selectQuery:
				{
                        //is { Select.Columns: [{ Expression: var e }]
                        return selectQuery.Select.Columns.Count==1  
						? GetDbDataType(selectQuery.Select.Columns[0].Expression)
						: DbDataType.Undefined;
				}

				case ExpressionWord sqlExpression:
				{
                        // Parameters: [var e],
                        return sqlExpression is { Expr: "{0}" } && sqlExpression.Parameters.Length==1
						? GetDbDataType(sqlExpression.Parameters[0])
						: DbDataType.Undefined;
				}

				case CaseWord caseExpression          : return GetCaseExpressionType(caseExpression);
				case ConditionWord conditionExpression: return GetConditionExpressionType(conditionExpression);

				case { SystemType: null } : return DbDataType.Undefined;
				case { SystemType: var t }: return new(t);
			};

			static DbDataType GetCaseExpressionType(CaseWord caseExpression)
			{
				foreach (var caseItem in caseExpression.Cases)
				{
					var caseType = GetDbDataType(caseItem.ResultExpression);
					if (caseType.DataType != DataFam.Undefined)
						return caseType;
				}

				return GetDbDataType(caseExpression.ElseExpression);
			}

			static DbDataType GetConditionExpressionType(ConditionWord sqlCondition)
			{
				var trueType = GetDbDataType(sqlCondition.TrueValue);
				if (trueType.DataType != DataFam.Undefined)
					return trueType;

				return GetDbDataType(sqlCondition.FalseValue);
			}
		}

		public static void CollectDependencies(ISQLNode root, IEnumerable<ITableNode> sources, HashSet<IExpWord> found, IEnumerable<ISQLNode>? ignore = null, bool singleColumnLevel = false)
		{
			var hash       = new HashSet<ITableNode>(sources);
			var hashIgnore = new HashSet<ISQLNode>(ignore ?? Enumerable.Empty<ISQLNode>());

			root.VisitParentFirst((hash, hashIgnore, found, singleColumnLevel), static (context, e) =>
			{
				if (e is ITableNode source && context.hash.Contains(source) || context.hashIgnore.Contains(e))
					return false;

				switch (e.NodeType)
				{
					case ClauseType.Column:
					{
						var c = (ColumnWord) e;
						if (c.Parent != null && context.hash.Contains(c.Parent))
							context.found.Add(c);
						if (context.singleColumnLevel)
							return false;
						break;
					}
					case ClauseType.SqlField:
					{
						var f = (FieldWord) e;
						if (f.Table != null && context.hash.Contains(f.Table))
							context.found.Add(f);
						break;
					}
				}
				return true;
			});
		}

		public static void CollectUsedSources(ISQLNode root, HashSet<ITableNode> found, IEnumerable<ISQLNode>? ignore = null)
		{
			var hashIgnore = new HashSet<ISQLNode>(ignore ?? Enumerable.Empty<ISQLNode>());

			root.VisitParentFirst((hashIgnore, found), static (context, e) =>
			{
				if (e is TableSourceWord source)
				{
					if (context.hashIgnore.Contains(e))
						return false;
					context.found.Add(source.Source);
				}

				switch (e.NodeType)
				{
					case ClauseType.Column:
					{
						var c = (ColumnWord) e;
						context.found.Add(c.Parent!);
						return false;
					}
					case ClauseType.SqlField:
					{
						var f = (FieldWord) e;
						context.found.Add(f.Table!);
						return false;
					}
				}
				return true;
			});
		}

		static bool IsTransitiveExpression(ExpressionWord sqlExpression, bool checkNullability)
		{
			if (sqlExpression.Parameters.Length==1 
				&& sqlExpression.Expr.Trim() == "{0}" 
				&& (!checkNullability || sqlExpression.CanBeNull == sqlExpression.Parameters[0].CanBeNullable(NullabilityContext.NonQuery)))
			{
				var p = sqlExpression.Parameters[0];
                if (p is ExpressionWord argExpression)
					return IsTransitiveExpression(argExpression, checkNullability);
				return true;
			}

			return false;
		}

		public static bool IsTransitivePredicate(ExpressionWord sqlExpression)
		{
			if (sqlExpression.Parameters.Length==1 && sqlExpression.Expr.Trim() == "{0}")
			{
                var p = sqlExpression.Parameters[0];
                if (p is ExpressionWord argExpression)
					return IsTransitivePredicate(argExpression);
				return p is IAffirmWord;
			}

			return false;
		}

		public static IExpWord UnwrapExpression(IExpWord expr, bool checkNullability)
		{
			if (expr.NodeType == ClauseType.SqlExpression)
			{
				var underlying = GetUnderlyingExpressionValue((ExpressionWord)expr, checkNullability);
				if (!ReferenceEquals(expr, underlying))
					return UnwrapExpression(underlying, checkNullability);
			}
			else if (!checkNullability && expr.NodeType == ClauseType.SqlNullabilityExpression)
				return UnwrapExpression(((NullabilityWord)expr).SqlExpression, checkNullability);

			return expr;
		}

		static IExpWord GetUnderlyingExpressionValue(ExpressionWord sqlExpression, bool checkNullability)
		{
			if (!IsTransitiveExpression(sqlExpression, checkNullability))
				return sqlExpression;

			if (sqlExpression.Parameters[0] is ExpressionWord subExpr)
				return GetUnderlyingExpressionValue(subExpr, checkNullability);

			return sqlExpression.Parameters[0];
		}

		public static bool IsConstantFast(IExpWord expr)
		{
			if (expr.NodeType == ClauseType.SqlValue || expr.NodeType == ClauseType.SqlParameter)
				return true;

			if (expr.NodeType == ClauseType.SqlNullabilityExpression)
				return IsConstantFast(((NullabilityWord)expr).SqlExpression);

			return false;
		}

		/// <summary>
		/// Returns <c>true</c> if tested expression is constant during query execution (e.g. value or parameter).
		/// </summary>
		/// <param name="expr">Tested expression.</param>
		/// <returns></returns>
		public static bool IsConstant(IExpWord expr)
		{
			switch (expr.NodeType)
			{
				case ClauseType.SqlValue:
				case ClauseType.SqlParameter:
					return true;

				case ClauseType.Column:
				{
					var sqlColumn = (ColumnWord) expr;

					// we can not guarantee order here
					// set operation contains at least two expressions for column
					// (in theory we can test that they are equal, but it is not worth it)
					if (sqlColumn.Parent != null && sqlColumn.Parent.HasSetOperators)
						return false;

					// column can be generated from subquery which can reference to constant expression
					return IsConstant(sqlColumn.Expression);
				}

				case ClauseType.SqlExpression:
				{
					var sqlExpr = (ExpressionWord) expr;
					if (!sqlExpr.IsPure || (sqlExpr.Flags & (SqlFlags.IsAggregate | SqlFlags.IsWindowFunction)) != 0)
						return false;
					return sqlExpr.Parameters.All(static p => IsConstant(p));
				}

				case ClauseType.SqlFunction:
				{
					var sqlFunc = (FunctionWord) expr;
					if (!sqlFunc.IsPure || sqlFunc.IsAggregate)
						return false;
					return sqlFunc.Parameters.All(static p => IsConstant(p));
				}
			}

			return false;
		}

		public static bool IsNullValue(this IExpWord expr)
		{
			if (expr is ValueWord { Value: null })
				return true;
			return false;
		}

		public static void ConcatSearchCondition(this WhereClause where, SearchConditionWord search)
		{
			var sc = where.EnsureConjunction();

			if (search.IsOr)
			{
				sc.Predicates.Add(search);
			}
			else
			{
				sc.Predicates.AddRange(search.Predicates);
			}
		}

		public static void ConcatSearchCondition(this HavingClause where, SearchConditionWord search)
		{
			var sc = where.EnsureConjunction();

			if (search.IsOr)
			{
				sc.Predicates.Add(search);
			}
			else
			{
				sc.Predicates.AddRange(search.Predicates);
			}
		}

		/// <summary>
		/// Ensures that expression is not A OR B but (A OR B)
		/// Function makes all needed manipulations for that
		/// </summary>
		/// <param name="whereClause"></param>
		public static SearchConditionWord EnsureConjunction(this WhereClause whereClause)
		{
			if (whereClause.SearchCondition.IsOr)
			{
				var old = whereClause.SearchCondition;
				whereClause.SearchCondition = new SearchConditionWord(false, old);
			}
			return whereClause.SearchCondition;
		}

		/// <summary>
		/// Ensures that expression is not A OR B but (A OR B)
		/// Function makes all needed manipulations for that
		/// </summary>
		/// <param name="whereClause"></param>
		public static SearchConditionWord EnsureConjunction(this HavingClause whereClause)
		{
			if (whereClause.SearchCondition.IsOr)
			{
				var old = whereClause.SearchCondition;
				whereClause.SearchCondition = new SearchConditionWord(false, old);
			}
			return whereClause.SearchCondition;
		}



		public static bool IsEqualTables([NotNullWhen(true)] TableWord? table1, [NotNullWhen(true)] TableWord? table2, bool withExtensions = true)
		{
			if (table1 == null || table2 == null)
				return false;

			var result =
				table1.ObjectType   == table2.ObjectType &&
				table1.TableName    == table2.TableName  &&
				table1.Expression   == table2.Expression;

			if (result && withExtensions)
			{
				result =
					(table1.SqlQueryExtensions == null || table1.SqlQueryExtensions.Count == 0) &&
					(table2.SqlQueryExtensions == null || table2.SqlQueryExtensions.Count == 0);
			}

			return result;
		}

		public static IEnumerable<ITableNode> EnumerateAccessibleSources(TableSourceWord tableSource)
		{
			if (tableSource.Source is SelectQueryClause q)
			{
				foreach (var ts in EnumerateAccessibleSources(q))
					yield return ts;
			}
			else
				yield return tableSource.Source;

			foreach (var join in tableSource.Joins)
			{
				foreach (var source in EnumerateAccessibleSources(join.Table as TableSourceWord))
					yield return source;
			}

		}

		/// <summary>
		/// Enumerates table sources recursively based on joins
		/// </summary>
		/// <param name="selectQuery"></param>
		/// <returns></returns>
		public static IEnumerable<TableSourceWord> EnumerateAccessibleTableSources(SelectQueryClause selectQuery)
		{
			foreach (var tableSource in selectQuery.Select.From.Tables)
			{
				foreach (var source in EnumerateAccessibleTableSources(tableSource as SelectQueryClause))
					yield return source;
			}
		}



		/// <summary>
		/// Enumerates table sources recursively based on joins
		/// </summary>
		/// <param name="selectQuery"></param>
		/// <returns></returns>
		public static IEnumerable<ITableNode> EnumerateAccessibleSources(SelectQueryClause selectQuery)
		{
			yield return selectQuery;

			foreach (var tableSource in selectQuery.Select.From.Tables)
			{
				foreach (var source in EnumerateAccessibleSources(tableSource as TableSourceWord))
					yield return source;
			}
		}

		public static IEnumerable<TableWord> EnumerateAccessibleTables(SelectQueryClause selectQuery)
		{
			return EnumerateAccessibleSources(selectQuery)
				.OfType<TableWord>();
		}

		static IEnumerable<TableSourceWord> EnumerateLevelSources(TableSourceWord tableSource)
		{
			foreach (var j in tableSource.Joins)
			{
				yield return j.Table as TableSourceWord;

				foreach (var js in EnumerateLevelSources(j.Table as TableSourceWord))
				{
					yield return js;
				}
			}
		}

		public static IEnumerable<TableSourceWord> EnumerateLevelSources(SelectQueryClause selectQuery)
		{
			foreach (var tableSource in selectQuery.Select.From.Tables)
			{
				yield return tableSource as TableSourceWord;

				foreach (var js in EnumerateLevelSources(tableSource as TableSourceWord))
				{
					yield return js;
				}
			}
		}

		public static IEnumerable<JoinTableWord> EnumerateJoins(SelectQueryClause selectQuery)
		{
			return selectQuery.Select.From.Tables.SelectMany( t => EnumerateJoins(t as SelectQueryClause));
		}



		public static string SuggestTableSourceAlias(SelectQueryClause selectQuery, string alias)
		{
			var aliases = new[] { alias };
			var currentAliases = EnumerateAccessibleTableSources(selectQuery).Where(ts => ts.Alias != null).Select(ts => ts.Alias!);
			Utils.MakeUniqueNames(aliases, currentAliases, s => s, (_, n, _) => aliases[0] = n);

			return aliases[0];
		}







		/// <summary>
		/// Unwraps SqlColumn and returns underlying expression.
		/// </summary>
		/// <param name="expression"></param>
		/// <returns>Underlying expression.</returns>
		static IExpWord? GetUnderlyingExpression(IExpWord? expression)
		{
			var current = expression;
			HashSet<IExpWord>? visited = null;
			while (current?.NodeType == ClauseType.Column)
			{
				visited ??= new HashSet<IExpWord>();
				if (!visited.Add(current))
					return null;

				var column = (ColumnWord)current;
				if (column.Parent != null && !column.Parent.HasSetOperators)
					current = column.Expression;
				else
					return null;
			}

			return current;
		}

		/// <summary>
		/// Returns SqlField from specific expression. Usually from SqlColumn.
		/// Complex expressions ignored.
		/// </summary>
		/// <param name="expression"></param>
		/// <returns>Field instance associated with expression</returns>
		public static FieldWord? GetUnderlyingField(IExpWord expression)
		{
			return GetUnderlyingExpression(expression) as FieldWord;
		}

		/// <summary>
		/// Returns SqlField from specific expression. Usually from SqlColumn.
		/// Conversion is ignored.
		/// </summary>
		/// <param name="expression"></param>
		/// <returns>Field instance associated with expression</returns>
		public static FieldWord? ExtractField(IExpWord expression)
		{
			var                      current = expression;
			HashSet<IExpWord>? visited = null;
			while (true)
			{
				visited ??= new HashSet<IExpWord>();
				if (!visited.Add(current))
					return null;

				if (current is ColumnWord column)
					current = column.Expression;
				else if (current is CastWord cast)
					current = cast.Expression;
				else if (current is ExpressionWord expr)
				{
					if (IsTransitiveExpression(expr, true))
						current = expr.Parameters[0];
					else
						break;
				}
				else
					break;
			}

			return current as FieldWord;
		}

		/// <summary>
		/// Returns SqlTable from specific expression. Usually from SqlColumn.
		/// Conversion is ignored.
		/// </summary>
		/// <param name="expression"></param>
		/// <returns>SqlTable instance associated with expression</returns>
		public static TableWord? ExtractSqlTable(IExpWord? expression)
		{
			if (expression is TableWord table) {
				return table;
			}
			if (expression is ColumnWord column) {
				return ExtractSqlTable(ExtractField(column));

            }

			if (expression is FieldWord f) {
				if (f.Table is TableWord tb) { 
					return tb;
				}
				if (f.Table is SelectQueryClause q && q.From.Tables.Count==1) { 
					var s=q.From.Tables[0].FindISrc();
					return ExtractSqlTable(s as IExpWord);
				}
			}

			return null;

		}

		/// <summary>
		/// Retrieves which sources are used in the <paramref name="root"/>expression
		/// </summary>
		/// <param name="root">Expression to analyze.</param>
		/// <param name="foundSources">Output container for detected sources/</param>
		public static void GetUsedSources(IExpWord root, HashSet<ITableNode> foundSources)
		{
			if (foundSources == null) throw new ArgumentNullException(nameof(foundSources));
			var rootCla= root as Clause;
            rootCla.Visit(foundSources, static (foundSources, e) =>
			{
				if (e is ITableNode source)
					foundSources.Add(source);
				else
					switch (e.NodeType)
					{
						case ClauseType.Column:
						{
							var c = (ColumnWord) e;
							foundSources.Add(c.Parent!);
							break;
						}
						case ClauseType.SqlField:
						{
							var f = (FieldWord) e;
							foundSources.Add(f.Table!);
							break;
						}
					}
			});
		}

		/// <summary>
		/// Returns correct column or field according to nesting.
		/// </summary>
		/// <param name="selectQuery">Analyzed query.</param>
		/// <param name="forExpression">Expression that has to be enveloped by column.</param>
		/// <param name="inProjection">If 'true', function ensures that column is created. If 'false' it may return Field if it fits to nesting level.</param>
		/// <returns>Returns Column of Field according to its nesting level. May return null if expression is not valid for <paramref name="selectQuery"/></returns>
		public static IExpWord? NeedColumnForExpression(SelectQueryClause selectQuery, IExpWord forExpression, bool inProjection)
		{
			var field = GetUnderlyingField(forExpression);

			ColumnWord? column = null;

			if (inProjection)
			{
				foreach (var c in selectQuery.Select.Columns.content)
				{
					if (c.Expression.Equals(forExpression) || (field != null && field.Equals(GetUnderlyingField(c.Expression))))
					{
						column = c;
						break;
					}
				}
			}

			if (column != null)
				return column;

			var tableToCompare = field?.Table;

			var tableSources = EnumerateLevelSources(selectQuery).OfType<TableSourceWord>().Select(static s => s.Source).ToArray();

			// enumerate tables first

			foreach (var table in tableSources.OfType<TableWord>())
			{
				if (tableToCompare != null && tableToCompare == table)
				{
					if (inProjection)
						return selectQuery.Select.AddNewColumn(field!);
					return field;
				}
			}

			foreach (var subQuery in tableSources.OfType<SelectQueryClause>())
			{
				column = NeedColumnForExpression(subQuery, forExpression, true) as ColumnWord;
				if (column != null && inProjection)
				{
					column = selectQuery.Select.AddNewColumn(column);
				}

				if (column != null)
					break;
			}

			return column;
		}

		/// <summary>
		/// Helper function for moving Ordering up in select tree.
		/// </summary>
		/// <param name="queries">Array of queries</param>
		public static void MoveOrderByUp(params SelectQueryClause[] queries)
		{
			// move order up if possible
			for (int qi = queries.Length - 2; qi >= 0; qi--)
			{
				var prevQuery = queries[qi + 1];
				if (prevQuery.Select.OrderBy.IsEmpty || prevQuery.Select.TakeValue != null || prevQuery.Select.SkipValue != null)
					continue;

				var currentQuery = queries[qi];

				for (var index = 0; index < prevQuery.Select.OrderBy.Items.Count; index++)
				{
					var item = prevQuery.Select.OrderBy.Items[index];
					foreach (var c in prevQuery.Select.Columns.content)
					{
						if (c.Expression.Equals(item.Expression))
						{
							currentQuery.OrderBy.Items.Add(new OrderByWord(c, item.IsDescending, item.IsPositioned));
							prevQuery.OrderBy.Items.RemoveAt(index--);
							break;
						}
					}
				}
			}
		}

#if NET8_0_OR_GREATER
		[GeneratedRegex(@"(?<open>{+)(?<key>\w+)(?<format>:[^}]+)?(?<close>}+)")]
		private static partial Regex ParamsRegex();
#else
		static Regex _paramsRegex = new (@"(?<open>{+)(?<key>\w+)(?<format>:[^}]+)?(?<close>}+)", RegexOptions.Compiled);
		static Regex ParamsRegex() => _paramsRegex;
#endif

		public static string TransformExpressionIndexes<TContext>(TContext context, string expression, Func<TContext, int, int> transformFunc)
		{
			if (expression    == null) throw new ArgumentNullException(nameof(expression));
			if (transformFunc == null) throw new ArgumentNullException(nameof(transformFunc));

			var str = ParamsRegex().Replace(expression, match =>
			{
				string open   = match.Groups["open"].Value;
				string key    = match.Groups["key"].Value;

				//string close  = match.Groups["close"].Value;
				//string format = match.Groups["format"].Value;

				if (open.Length % 2 == 0)
					return match.Value;

				if (!int.TryParse(key, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out var idx))
					return match.Value;

				var newIndex = transformFunc(context, idx);

				return $"{{{newIndex}}}";
			});

			return str;
		}

		public static IExpWord ConvertFormatToConcatenation(string format, IList<IExpWord> parameters)
		{
			if (format     == null) throw new ArgumentNullException(nameof(format));
			if (parameters == null) throw new ArgumentNullException(nameof(parameters));

			string StripDoubleQuotes(string str)
			{
				str = str.Replace("{{", "{");
				str = str.Replace("}}", "}");
				return str;
			}

			var matches = ParamsRegex().Matches(format);

			IExpWord? result = null;
			var lastMatchPosition = 0;

			foreach (Match? match in matches)
			{
				if (match == null)
					continue;

				var open = match.Groups["open"].Value;
				var key  = match.Groups["key"].Value;

				if (open.Length % 2 == 0)
					continue;

				if (!int.TryParse(key, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out var idx))
					continue;

				var current = parameters[idx];

				var brackets = open.Length / 2;
				if (match.Index > lastMatchPosition)
				{
					var value = StripDoubleQuotes(format.Substring(lastMatchPosition, match.Index - lastMatchPosition + brackets));
					current = new BinaryWord(typeof(string),
						new ValueWord(typeof(string), value),
						"+", current,
						PrecedenceLv.Additive);
				}

				result = result == null ? current : new BinaryWord(typeof(string), result, "+", current);

				lastMatchPosition = match.Index + match.Length - brackets;
			}

			if (result != null && lastMatchPosition < format.Length)
			{
				var value = StripDoubleQuotes(format.Substring(lastMatchPosition));
				result = new BinaryWord(typeof(string),
					result, "+", new ValueWord(typeof(string), value), PrecedenceLv.Additive);
			}

			result ??= new ValueWord(typeof(string), format);

			return result;
		}

		public static bool IsAggregationOrWindowFunction(ISQLNode expr)
		{
			return IsAggregationFunction(expr) || IsWindowFunction(expr);
		}

		public static bool IsAggregationFunction(ISQLNode expr)
		{
			return expr switch
			{
				FunctionWord func         => func.IsAggregate,
				ExpressionWord expression => (expression.Flags & SqlFlags.IsAggregate) != 0,
				_                        => false,
			};
		}

		public static bool IsWindowFunction(ISQLNode expr)
		{
			if (expr is ExpressionWord expression)
				return (expression.Flags & SqlFlags.IsWindowFunction) != 0;

			return false;
		}

		public static bool ContainsAggregationOrWindowFunction(ISQLNode expr)
		{
			return ContainsExpressionInSameLevel(expr, e => IsWindowFunction(e) || IsAggregationFunction(e));
		}

		public static bool ContainsWindowFunction(ISQLNode expr)
		{
			return ContainsExpressionInSameLevel(expr, IsWindowFunction);
		}

		public static bool ContainsAggregationFunction(ISQLNode expr)
		{
			return ContainsExpressionInSameLevel(expr, IsAggregationFunction);
		}

		static bool ContainsExpressionInSameLevel(ISQLNode expr, Func<IExpWord, bool> matchFunc)
		{
			var found = false;

			expr.VisitParentFirst((e) =>
			{
				if (found)
					return false;

				if (e is SelectQueryClause)
					return false;

				if (e is IExpWord sqlExpr && matchFunc(sqlExpr))
				{
					found = true;
					return false;
				}

				return true;
			});

			return found;
		}

		/// <summary>
		/// Collects unique keys from different sources.
		/// </summary>
		/// <param name="tableSource"></param>
		/// <param name="knownKeys">List with found keys.</param>
		public static void CollectUniqueKeys(TableSourceWord tableSource, List<IList<IExpWord>> knownKeys)
		{
			if (tableSource.HasUniqueKeys)
				knownKeys.AddRange(tableSource.UniqueKeys);

			CollectUniqueKeys(tableSource.Source, true, knownKeys);
		}

		/// <summary>
		/// Collects unique keys from different sources.
		/// </summary>
		/// <param name="tableSource"></param>
		/// <param name="includeDistinct">Flag to include Distinct as unique key.</param>
		/// <param name="knownKeys">List with found keys.</param>
		public static void CollectUniqueKeys(ITableNode tableSource, bool includeDistinct, List<IList<IExpWord>> knownKeys)
		{
			switch (tableSource)
			{
				case TableWord table:
				{
					var keys = table.GetKeys(false);
					if (keys != null && keys.Count > 0)
						knownKeys.Add(keys);

					break;
				}
				case SelectQueryClause selectQuery:
				{
					if (selectQuery.HasUniqueKeys)
						knownKeys.AddRange(selectQuery.UniqueKeys);

					if (includeDistinct && selectQuery.Select.IsDistinct)
						knownKeys.Add(selectQuery.Select.Columns.content.Select(c => c.Expression).ToList());

					if (!selectQuery.Select.GroupBy.IsEmpty)
					{
						knownKeys.Add(selectQuery.Select.GroupBy.Items);
					}

					if (selectQuery.From.Tables.Count == 1)
					{
						var table = selectQuery.From.Tables[0];
						if (table.HasUniqueKeys() && table.GetJoins().Count == 0)
						{
							knownKeys.AddRange(table.FindUniqueKeys());
						}
					}

					break;
				}
			}
		}

		sealed class NeedParameterInliningContext
		{
			public bool HasParameter;
			public bool IsQueryParameter;
		}

		public static bool NeedParameterInlining(IExpWord expression)
		{
			var ctx = new NeedParameterInliningContext();
			var clau= expression as Clause;
            clau.Visit(ctx, (Action<NeedParameterInliningContext, ISQLNode>)(static (context, e) =>
			{
				if (e.NodeType == data.model.ClauseType.SqlParameter)
				{
					context.HasParameter     = true;
					context.IsQueryParameter = context.IsQueryParameter || ((ParameterWord)e).IsQueryParameter;
				}
			}));

			if (ctx.HasParameter && ctx.IsQueryParameter)
				return false;

			return ctx.HasParameter;
		}

		public static bool ShouldCheckForNull(this IExpWord expr, NullabilityContext nullability)
		{
			if (!expr.CanBeNullable(nullability))
				return false;

			if (expr.NodeType == ClauseType.SqlBinaryExpression)
				return false;

			if (expr.NodeType == ClauseType.SqlField ||
				expr.NodeType == ClauseType.Column   ||
				expr.NodeType == ClauseType.SqlValue ||
				expr.NodeType == ClauseType.SqlParameter)
				return true;

			if ((expr.NodeType == ClauseType.SqlFunction) && ((FunctionWord)expr).Parameters.Length == 1)
				return true;

			if (null != expr.Find(ClauseType.SqlQuery))
				return false;

			return true;
		}

		public static SelectQueryClause GetInnerQuery(this SelectQueryClause selectQuery)
		{
			if (selectQuery.IsSimple() && selectQuery.From.Tables.Count==1)
			{
				var sub=selectQuery.From.Tables[0].FindISrc() as SelectQueryClause;
				var inner = sub.GetInnerQuery();
				if (inner.From.Tables.Count == 0)
				{
					return inner;
				}
			}

			return selectQuery;
		}


		[return: NotNullIfNotNull(nameof(sqlExpression))]
		public static IExpWord? SimplifyColumnExpression(IExpWord? sqlExpression)
		{
			if (sqlExpression is SelectQueryClause sel && sel.From.Tables.Count == 0 && sel.Select.Columns.Count == 1) {
                return SimplifyColumnExpression(sel.Select.Columns[0].Expression);
            }

			return sqlExpression;

		}

		public static SearchConditionWord CorrectComparisonForJoin(SearchConditionWord sc)
		{
			var newSc = new SearchConditionWord(false);
			for (var index = 0; index < sc.Predicates.Count; index++)
			{
				var predicate = sc.Predicates[index];
				if (predicate is ExprExpr exprExpr)
				{
					if ((exprExpr.Operator == AffirmWord.Operator.Equal ||
						 exprExpr.Operator == AffirmWord.Operator.NotEqual)
						&& exprExpr.WithNull != null)
					{
						predicate = new ExprExpr(exprExpr.Expr1, exprExpr.Operator, exprExpr.Expr2, null);
					}
				}
				else if (predicate is SearchConditionWord { IsOr: false } subSc)
				{
					predicate = CorrectComparisonForJoin(subSc);
				}

				newSc.Predicates.Add(predicate);
			}

			return newSc;
		}



		public static IExpWord CreateSqlValue(object? value, BinaryWord be, DBInstance mappingSchema)
		{
			return CreateSqlValue(value, GetDbDataType(be, mappingSchema), be.Expr1, be.Expr2);
		}

		public static IExpWord CreateSqlValue(object? value, DbDataType dbDataType, params IExpWord[] basedOn)
		{
			ParameterWord? foundParam = null;

			foreach (var element in basedOn)
			{
				if (element.NodeType == ClauseType.SqlParameter)
				{
					var param = (ParameterWord)element;
					if (param.IsQueryParameter)
					{
						foundParam = param;
					}
					else
						foundParam ??= param;
				}
			}

			if (foundParam != null)
			{
				var newParam = new ParameterWord(dbDataType, foundParam.Name, value)
				{
					IsQueryParameter = foundParam.IsQueryParameter,
					NeedsCast = foundParam.NeedsCast
				};

				return newParam;
			}

			return new ValueWord(dbDataType, value);
		}

		public static IExpWord UnwrapNullablity(IExpWord expr)
		{
			while (expr is NullabilityWord nullability)
				expr = nullability.SqlExpression;

			return expr;
		}

		public static IExpWord UnwrapCastAndNullability(IExpWord expr)
		{
			do
			{
				if (expr is NullabilityWord nullability)
				{
					expr = nullability.SqlExpression;
				}
				else if (expr is CastWord sqlCast)
				{
					expr = sqlCast.Expression;
				}
				else
					break;
			} while (true);

			return expr;
		}

		public static bool SameWithoutNullablity(IExpWord expr1, IExpWord expr2)
		{
			if (ReferenceEquals(expr1, expr2))
				return true;

			if (ReferenceEquals(UnwrapNullablity(expr1), UnwrapNullablity(expr2)))
				return true;

			return false;
		}


		public static bool HasElement(this ISQLNode root, ISQLNode element)
		{
			return null != root.Find(element, static (tf, e) => ReferenceEquals(tf, e));
		}

		public static bool HasQueryParameter(this ISQLNode root)
		{
			return null != root.Find((Func<ISQLNode, bool>)(e =>
			{
				if (e.NodeType == data.model.ClauseType.SqlParameter)
				{
					var param = (ParameterWord)e;
					return param.IsQueryParameter;
				}

				return false;
			}));
		}



		public static void DebugCheckNesting(BaseSentence statement, bool isSubQuery)
		{
			// TODO: temporary disabled

			// var checkVisitor = new SqlQueryNestingValidationVisitor(isSubQuery, statement);
			// checkVisitor.Visit(statement);
		}

		public static bool? GetBoolValue(ISQLNode element, EvaluateContext evaluationContext)
		{
			if (element.TryEvaluateExpression(evaluationContext, out var value))
				return value as bool?;

			return null;
		}

	}
}
