using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

// ReSharper disable InconsistentNaming

namespace mooSQL.linq.SqlProvider
{
	using Common;
	using Expressions;
	using Mapping;
    using mooSQL.data;
    using mooSQL.data.model;
    using mooSQL.data.model.affirms;
    using SqlQuery;
	using SqlQuery.Visitors;

	public class BasicSqlOptimizer : ISqlOptimizer
	{
		#region Init

		public DBInstance DBLive {  get; set; }

		protected BasicSqlOptimizer(SQLProviderFlags sqlProviderFlags)
		{
			SqlProviderFlags = sqlProviderFlags;
		}

		protected SQLProviderFlags SqlProviderFlags { get; }

		#endregion

		#region ISqlOptimizer Members


		public virtual SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new SqlExpressionConvertVisitor(allowModify);
		}

		public virtual BaseSentence Finalize(DBInstance mappingSchema, BaseSentence statement)
		{
			FixEmptySelect(statement);
			FinalizeCte   (statement);

			var evaluationContext = new EvaluateContext(null);

			statement = (BaseSentence)OptimizeQueries(statement, statement,  evaluationContext);

			if (DBLive.dialect.Option.OptimizeJoins)
			{
				statement = new JoinsOptimizer().Optimize(statement, evaluationContext);

				// Do it again after JOIN Optimization
				FinalizeCte(statement);
			}

			statement = FinalizeInsert(statement);
			statement = FinalizeSelect(statement);
			statement = CorrectUnionOrderBy(statement);
			statement = FixSetOperationNulls(statement);

			// provider specific query correction
			statement = FinalizeStatement(statement, evaluationContext);

//statement.EnsureFindTables();

			return statement;
		}

		#endregion

		protected virtual BaseSentence FinalizeInsert(BaseSentence statement)
		{
			if (statement is InsertSentence insertStatement)
			{
				var tables = insertStatement.SelectQuery.From.Tables;
				var isSelfInsert =
					tables.Count     == 0 ||
					tables.Count     == 1 &&
					tables[0].FindISrc() == insertStatement.Insert.Into;

				if (isSelfInsert)
				{
					if (insertStatement.SelectQuery.IsSimple() || insertStatement.SelectQuery.From.Tables.Count == 0)
					{
						// simplify insert
						//
						insertStatement.Insert.Items.ForEach(item =>
						{
							if (item.Expression is ColumnWord column)
								item.Expression = column.Expression;
						});
						insertStatement.SelectQuery.From.Tables.Clear();
					}
				}
			}

			return statement;
		}

		internal static (TableSourceWord? tableSource, List<Clause>? queryPath) FindTableSource(Stack<Clause> currentPath, TableSourceWord source, TableWord table)
		{
			if (source.Source == table)
				return (source, currentPath.ToList());

			if (source.Source is SelectQueryClause selectQuery)
			{
				var result = FindTableSource(currentPath, selectQuery, table);
				if (result.tableSource != null)
					return result;
			}

			foreach (var join in source.GetJoins())
			{
				currentPath.Push(join);
				var result = FindTableSource(currentPath, join.Table as TableSourceWord, table);
				currentPath.Pop();
				if (result.tableSource != null)
				{
					return result;
				}
			}

			return default;
		}

		internal static (TableSourceWord? tableSource, List<Clause>? queryPath) FindTableSource(Stack<Clause> currentPath, SelectQueryClause selectQuery, TableWord table)
		{
			currentPath.Push(selectQuery);
			foreach (var source in selectQuery.From.Tables)
			{
				var result = FindTableSource(currentPath, source as TableSourceWord, table);
				if (result.tableSource != null)
					return result;
			}
			currentPath.Pop();

			return default;
		}

		static bool IsCompatibleForUpdate(SelectQueryClause selectQuery)
		{
			return !selectQuery.Select.IsDistinct && selectQuery.Select.GroupBy.IsEmpty;
		}

		static bool IsCompatibleForUpdate(JoinTableWord joinedTable)
		{
			return joinedTable.JoinType is JoinKind.Inner or JoinKind.Left or JoinKind.Right;
		}

		static bool IsCompatibleForUpdate(List<Clause> path)
		{
			if (path.Count > 2)
				return false;

			var result = path.All(e =>
			{
				return e switch
				{
                    SelectQueryClause sc    => IsCompatibleForUpdate(sc),
                    JoinTableWord jt => IsCompatibleForUpdate(jt),
					_                 => true,
				};
			});

			return result;
		}

		protected static bool IsCompatibleForUpdate(SelectQueryClause query, TableWord updateTable, int level = 0)
		{
			if (!IsCompatibleForUpdate(query))
				return false;

			foreach (var ts in query.From.Tables)
			{
				if (ts.FindISrc() == updateTable)
					return true;

				foreach (var join in ts.GetJoins())
				{
					if (join.Table.FindISrc() == updateTable)
					{
						return IsCompatibleForUpdate(join);
					}

					if (IsCompatibleForUpdate(join) && join.Table.FindISrc() is SelectQueryClause sc)
					{
						if (IsCompatibleForUpdate(sc, updateTable))
							return true;
					}
				}
			}

			return false;
		}

		static IExpWord? PopulateNesting(List<SelectQueryClause> queryPath, IExpWord expression, int ignoreCount)
		{
			var current = expression;
			for (var index = 0; index < queryPath.Count - ignoreCount; index++)
			{
				var selectQuery = queryPath[index];
				var idx         = selectQuery.Select.Columns.content.FindIndex(c => c.Expression == current);
				if (idx < 0)
				{
					if (selectQuery.Select.IsDistinct || !selectQuery.GroupBy.IsEmpty)
						return null;

					current = selectQuery.Select.AddNewColumn(current);
				}
				else
					current = selectQuery.Select.Columns[idx];
			}

			return current;
		}

		protected void ApplyUpdateTableComparison(SearchConditionWord searchCondition, SelectQueryClause updateQuery,
            UpdateClause updateClause, TableWord inQueryTable)
		{
			var compareKeys = inQueryTable.GetKeys(true);
			var tableKeys   = updateClause.Table!.GetKeys(true);

			var found = false;

			if (tableKeys != null && compareKeys != null)
			{
				for (var i = 0; i < tableKeys.Count; i++)
				{
					var tableKey = tableKeys[i];

					found = true;
					searchCondition.AddEqual(tableKey, compareKeys[i],DBLive.dialect.Option.CompareNullsAsValues);
				}
			}

			if (!found)
				throw new LinqToDBException("Could not generate update statement.");
		}

		protected void ApplyUpdateTableComparison(SelectQueryClause updateQuery, UpdateClause updateClause, TableWord inQueryTable)
		{
			ApplyUpdateTableComparison(updateQuery.Where.EnsureConjunction(), updateQuery, updateClause, inQueryTable);
		}

		protected virtual UpdateSentence BasicCorrectUpdate(UpdateSentence statement,bool wrapForOutput)
		{
			if (statement.Update.Table != null)
			{
				var (tableSource, queryPath) = FindTableSource(new Stack<Clause>(), statement.SelectQuery, statement.Update.Table as TableWord);

				if (tableSource != null && queryPath != null)
				{
					statement.Update.TableSource = tableSource;

					var forceWrapping = wrapForOutput && statement.Output != null &&
										(statement.SelectQuery.From.Tables.Count != 1 ||
										 statement.SelectQuery.From.Tables.Count          == 1 &&
										 statement.SelectQuery.From.Tables[0].GetJoins().Count == 0);

					if (forceWrapping || !IsCompatibleForUpdate(queryPath))
					{
						// we have to create new Update table and join via Keys

						var queries = queryPath.OfType<SelectQueryClause>().ToList();
						var keys    = statement.Update.Table.GetKeys(true);

						if (!(keys?.Count > 0))
						{
							keys = queries[0].Select.Columns.content
								.Where(c => c.Expression is FieldWord field && field.Table == statement.Update.Table)
								.Select(c => c.Expression)
								.ToList();
						}

						if (keys.Count == 0)
						{
							throw new LinqToDBException("Invalid update query.");
						}

						var keysColumns = new List<IExpWord>(keys.Count);
						foreach(var key in keys)
						{
							var newColumn = PopulateNesting(queries, key, 1);
							if (newColumn == null)
							{
								throw new LinqToDBException("Invalid update query. Could not create comparision key. It can be GROUP BY or DISTINCT query modifier.");
							}

							keysColumns.Add(newColumn);
						}

						var originalTableForUpdate = statement.Update.Table;
						var newTable = CloneTable(originalTableForUpdate as TableWord, out var objectMap);

						var sc    = new SearchConditionWord();

						for (var index = 0; index < keys.Count; index++)
						{
							var originalField = keys[index];

							if (!objectMap.TryGetValue(originalField as Clause, out var newField))
							{
								throw new InvalidOperationException();
							}

							var originalColumn = keysColumns[index];

							sc.AddEqual((IExpWord)newField, originalColumn,DBLive.dialect.Option.CompareNullsAsValues);
						}

						if (!SqlProviderFlags.IsUpdateFromSupported)
						{
							// build join
							//

							var tsIndex = statement.SelectQuery.From.Tables.FindIndex(ts =>
								queries.Contains(ts.FindISrc()));

							if (tsIndex < 0)
								throw new InvalidOperationException();

							var ts   = statement.SelectQuery.From.Tables[tsIndex];
							var join = new JoinTableWord(JoinKind.Inner, ts, false, sc);

							statement.SelectQuery.From.Tables.RemoveAt(tsIndex);
							statement.SelectQuery.From.Tables.Insert(0, new TableSourceWord(newTable, "t", join));
						}
						else
						{
							statement.SelectQuery.Where.ConcatSearchCondition(sc);
						}

						for (var index = 0; index < statement.Update.Items.Count; index++)
						{
							var item = statement.Update.Items[index];
							if (item.Column is ColumnWord column)
								item.Column = QueryHelper.GetUnderlyingField(column.Expression) ?? column.Expression;

							item = item.ConvertAll(this, (v, e) =>
							{
								if (objectMap.TryGetValue(e, out var newValue))
								{
									return newValue;
								}

								return e;
							});

							statement.Update.Items[index] = item;
						}

						statement.Update.Table       = newTable;
						statement.Update.TableSource = null;
					}
					else
					{
						if (queryPath.Count > 0)
						{
							var ts = statement.SelectQuery.From.Tables.FirstOrDefault();
							if (ts != null)
							{
								if (ts.FindISrc() is SelectQueryClause)
									statement.Update.TableSource = ts;
							}
						}
					}

					CorrectUpdateSetters(statement);
				}
			}

			return statement;
		}

		protected virtual BaseSentence FinalizeUpdate(BaseSentence statement)
		{
			if (statement is UpdateSentence updateStatement)
			{
				// get from columns expression
				//
				updateStatement.Update.Items.ForEach(item =>
				{
					item.Expression = QueryHelper.SimplifyColumnExpression(item.Expression);
				});

			}

			return statement;
		}

		protected virtual BaseSentence FinalizeSelect(BaseSentence statement)
		{
			var expandVisitor = new SqlRowExpandVisitor();
			expandVisitor.ProcessElement(statement);

			return statement;
		}

		class SqlRowExpandVisitor : SqlQueryVisitor
		{
            SelectQueryClause? _updateSelect;

			public SqlRowExpandVisitor() : base(VisitMode.Modify, null)
			{
			}

			public override Clause VisitSelectClause(SelectClause element)
			{
				var newElement = base.VisitSelectClause(element);

				if (!ReferenceEquals(newElement, element))
					return Visit(newElement);

				if (_updateSelect == element.SelectQuery)
					return element;

				// When selecting a SqlRow, expand the row into individual columns.

				for (var i = 0; i < element.Columns.Count; i++)
				{
					var column    = element.Columns[i];
					var unwrapped = QueryHelper.UnwrapNullablity(column.Expression);
					if (unwrapped.NodeType == ClauseType.SqlRow)
					{
						var row = (RowWord)unwrapped;
						element.Columns.content.RemoveAt(i);
						element.Columns.content.InsertRange(i, row.Values.Select(v => new ColumnWord(element.SelectQuery, v)));
					}
				}

				return element;
			}

			public override Clause VisitAffirmExprExpr(mooSQL.data.model.affirms.ExprExpr predicate)
			{
				base.VisitAffirmExprExpr(predicate);

				// flip expressions when comparing a row to a query
				if (QueryHelper.UnwrapNullablity(predicate.Expr2).NodeType == ClauseType.SqlRow && QueryHelper.UnwrapNullablity(predicate.Expr1).NodeType == ClauseType.SqlQuery)
				{
					var newPredicate = new mooSQL.data.model.affirms.ExprExpr(predicate.Expr2, mooSQL.data.model.affirms.ExprExpr.SwapOperator(predicate.Operator), predicate.Expr1, predicate.WithNull);
					return newPredicate;
				}

				return predicate;
			}

			public override Clause VisitUpdateSentence(UpdateSentence element)
			{
				var saveUpdateSelect = _updateSelect;
				_updateSelect = element.SelectQuery;

				var result = base.VisitUpdateSentence(element);

				_updateSelect = saveUpdateSelect;
				return result;
			}
		}

		protected virtual BaseSentence CorrectUnionOrderBy(BaseSentence statement)
		{
			var queriesToWrap = new HashSet<SelectQueryClause>();

			statement.Visit(queriesToWrap, (wrap, e) =>
			{
				if (e is SelectQueryClause sc && sc.HasSetOperators)
				{
					var prevQuery = sc;

					for (int i = 0; i < sc.SetOperators.Count; i++)
					{
						var currentOperator = sc.SetOperators[i];
						var currentQuery    = currentOperator.SelectQuery;

						if (currentOperator.Operation == SetOperation.Union)
						{
							if (!prevQuery.Select.HasModifier && !prevQuery.OrderBy.IsEmpty)
							{
								prevQuery.OrderBy.Items.Clear();
							}

							if (!currentQuery.Select.HasModifier && !currentQuery.OrderBy.IsEmpty)
							{
								currentQuery.OrderBy.Items.Clear();
							}
						}
						else
						{
							if (!prevQuery.OrderBy.IsEmpty)
							{
								wrap.Add(prevQuery);
							}

							if (!currentQuery.OrderBy.IsEmpty)
							{
								wrap.Add(currentQuery);
							}
						}

						prevQuery = currentOperator.SelectQuery;
					}
				}
			});

			if (queriesToWrap.Count == 0)
				return statement;

			return QueryHelper.WrapQuery(
				queriesToWrap,
				statement,
				static (wrap, q, parentElement) => wrap.Contains(q),
				null,
				allowMutation: true,
				withStack: true);
		}

		static void CorrelateNullValueTypes(ref IExpWord toCorrect, IExpWord reference)
		{
			if (toCorrect.NodeType == ClauseType.Column)
			{
				var column     = (ColumnWord)toCorrect;
				var columnExpr = column.Expression;
				CorrelateNullValueTypes(ref columnExpr, reference);
				column.Expression = columnExpr;
			}
			else if (toCorrect.NodeType == ClauseType.SqlValue)
			{
				var value = (ValueWord)toCorrect;
				if (value.Value == null)
				{
					var suggested = QueryHelper.SuggestDbDataType(reference);
					if (suggested != null)
					{
						toCorrect = new ValueWord(suggested.Value, null);
					}
				}
			}
		}

		protected virtual BaseSentence FixSetOperationNulls(BaseSentence statement)
		{
			statement.VisitParentFirst(static e =>
			{
				if (e.NodeType == ClauseType.SqlQuery)
				{
					var query = (SelectQueryClause)e;
					if (query.HasSetOperators)
					{
						for (var i = 0; i < query.Select.Columns.content.Count; i++)
						{
							var column     = query.Select.Columns[i];
							var columnExpr = column.Expression;

							foreach (var setOperator in query.SetOperators)
							{
								var otherColumn = setOperator.SelectQuery.Select.Columns[i];
								var otherExpr   = otherColumn.Expression;

								CorrelateNullValueTypes(ref columnExpr, otherExpr);
								CorrelateNullValueTypes(ref otherExpr, columnExpr);

								otherColumn.Expression = otherExpr;
							}

							column.Expression = columnExpr;
						}
					}
				}

				return true;
			});

			return statement;
		}

		protected virtual void FixEmptySelect(BaseSentence statement)
		{
			// avoid SELECT * top level queries, as they could create a lot of unwanted traffic
			// and such queries are not supported by remote context
			if (statement.QueryType == QueryType.Select && statement.SelectQuery!.Select.Columns.content.Count == 0)
				statement.SelectQuery!.Select.Add(new ValueWord(1));
		}

		/// <summary>
		/// Used for correcting statement and should return new statement if changes were made.
		/// </summary>
		/// <param name="statement"></param>
		/// <param name="dataOptions"></param>
		/// <param name="mappingSchema"></param>
		/// <returns></returns>
		public virtual BaseSentence TransformStatement(BaseSentence statement)
		{
			return statement;
		}

		static void RegisterDependency(CTEClause cteClause, Dictionary<CTEClause, HashSet<CTEClause>> foundCte)
		{
			if (foundCte.ContainsKey(cteClause))
				return;

			var dependsOn = new HashSet<CTEClause>();
			cteClause.Body!.Visit(dependsOn, static (dependsOn, ce) =>
			{
				if (ce.NodeType == ClauseType.SqlCteTable)
				{
					var subCte = ((CteTableWord)ce).Cte!;
					dependsOn.Add(subCte);
				}

			});

			foundCte.Add(cteClause, dependsOn);

			foreach (var clause in dependsOn)
			{
				RegisterDependency(clause, foundCte);
			}
		}

		void FinalizeCte(BaseSentence statement)
		{
			if (statement is BaseSentenceWithQuery select)
			{
				// one-field class is cheaper than dictionary instance
				var cteHolder = new WritableContext<Dictionary<CTEClause, HashSet<CTEClause>>?>();

				if (select is MergeSentence merge)
				{
					var target = merge.Target as Clause;

                    target.Visit(cteHolder, static (foundCte, e) =>
						{
							if (e.NodeType == ClauseType.SqlCteTable)
							{
								var cte = ((CteTableWord)e).Cte!;
								RegisterDependency(cte, foundCte.WriteableValue ??= new());
							}
						}
					);
                    var Source = merge.Source as Clause;
                    Source.Visit(cteHolder, static (foundCte, e) =>
						{
							if (e.NodeType == ClauseType.SqlCteTable)
							{
								var cte = ((CteTableWord)e).Cte!;
								RegisterDependency(cte, foundCte.WriteableValue ??= new());
							}
						}
					);
				}
				else
				{
					//throw new NotImplementedException();
                    select.SelectQuery.Visit(cteHolder, ( foundCte,  e) =>
						{
							if (e.NodeType == ClauseType.SqlCteTable)
							{
								var cte = ((CteTableWord)e).Cte!;
								RegisterDependency(cte, foundCte.WriteableValue ??= new());
							}
						}
					);
				}

				if (cteHolder.WriteableValue == null || cteHolder.WriteableValue.Count == 0)
					select.With = null;
				else
				{
					// TODO: Ideally if there is no recursive CTEs we can convert them to SubQueries
					if (!SqlProviderFlags.IsCommonTableExpressionsSupported)
						throw new LinqToDBException("DataProvider do not supports Common Table Expressions.");

					// basic detection of non-recursive CTEs
					// for more complex cases we will need dependency cycles detection
					foreach (var kvp in cteHolder.WriteableValue)
					{
						if (kvp.Value.Count == 0)
							kvp.Key.IsRecursive = false;

						// remove self-reference for topo-sort
						kvp.Value.Remove(kvp.Key);
					}

					var ordered = TopoSorting.TopoSort<CTEClause, WritableContext<Dictionary<CTEClause, HashSet<CTEClause>>>>(cteHolder.WriteableValue.Keys, cteHolder, static (cteHolder, i) => cteHolder.WriteableValue![i]).ToList();

                    Utils.MakeUniqueNames(ordered, null, static (n, a) => !ReservedWords.IsReserved(n), c => c.Name, static (c, n, a) => c.Name = n,
c => string.IsNullOrEmpty(c.Name) ? "CTE_1" : c.Name, StringComparer.OrdinalIgnoreCase);

					select.With = new WithClause();
					select.With.Clauses.AddRange(ordered);
				}
			}
		}

		protected static bool HasParameters(IExpWord expr)
		{
			var hasParameters  = null != expr.Find(ClauseType.SqlParameter);

			return hasParameters;
		}

		static T NormalizeExpressions<T>(T expression, bool allowMutation)
			where T : Clause
		{
			var result = expression.ConvertAll(allowMutation: allowMutation, static (visitor, e) =>
			{
				if (e.NodeType == ClauseType.SqlExpression)
				{
					var expr = (ExpressionWord)e;
					var newExpression = expr;

					// we interested in modifying only expressions which have parameters
					if (HasParameters(expr))
					{
						if (string.IsNullOrEmpty(expr.Expr) || expr.Parameters.Length == 0)
							return expr;

						var newExpressions = new List<IExpWord>();

						var ctx = WritableContext.Create(false, (newExpressions, visitor, expr));

						var newExpr = QueryHelper.TransformExpressionIndexes(
							ctx,
							expr.Expr,
							(context, idx) =>
							{
								if (idx >= 0 && idx < context.StaticValue.expr.Parameters.Length)
								{
									var paramExpr  = context.StaticValue.expr.Parameters[idx];
									var normalized = NormalizeExpressions(paramExpr as Clause, context.StaticValue.visitor.AllowMutation) as IExpWord;

									if (!context.WriteableValue && !ReferenceEquals(normalized, paramExpr))
										context.WriteableValue = true;

									var newIndex   = context.StaticValue.newExpressions.Count;

									context.StaticValue.newExpressions.Add(normalized);
									return newIndex;
								}
								return idx;
							});

						var changed = ctx.WriteableValue || newExpr != expr.Expr;

						if (changed)
							newExpression = new ExpressionWord(expr.SystemType, newExpr, expr.Precedence, expr.Flags, expr.NullabilityType, null, newExpressions.ToArray());

						return newExpression;
					}
				}
				return e;
			});

			return result;
		}

		#region Alternative Builders

		protected DeleteSentence GetAlternativeDelete(DeleteSentence deleteStatement)
		{
			if ((deleteStatement.SelectQuery.From.Tables.Count > 1 || deleteStatement.SelectQuery.From.Tables[0].GetJoins().Count > 0))
			{
				var table = deleteStatement.Table ?? deleteStatement.SelectQuery.From.Tables[0].FindISrc() as TableWord;

				//TODO: probably we can improve this part
				if (table == null)
					throw new LinqToDBException("Could not deduce table for delete");

				if (deleteStatement.Output != null)
					throw new NotImplementedException("GetAlternativeDelete not implemented for delete with output");

				var sql = new SelectQueryClause { IsParameterDependent = deleteStatement.IsParameterDependent };

				var newDeleteStatement = new DeleteSentence(sql);

				var copy      = new TableWord(table as TableWord) { Alias = null };
				var tableKeys = table.GetKeys(true);
				var copyKeys  = copy. GetKeys(true);

				var wsc = deleteStatement.SelectQuery.Where.EnsureConjunction();

				if (copyKeys == null || tableKeys == null)
				{
					throw new LinqToDBException("Could not generate comparison between tables.");
				}

				for (var i = 0; i < tableKeys.Count; i++)
					wsc.AddEqual(copyKeys[i], tableKeys[i], false);

				newDeleteStatement.SelectQuery.From.Where.SearchCondition.AddExists(deleteStatement.SelectQuery);
				newDeleteStatement.With = deleteStatement.With;

				deleteStatement = newDeleteStatement;
			}

			return deleteStatement;
		}

		static bool IsAggregationFunction(Clause expr)
		{
			if (expr is FunctionWord func)
				return func.IsAggregate;

			if (expr is ExpressionWord expression)
				return expression.IsAggregate;

			return false;
		}

		protected bool NeedsEnvelopingForUpdate(SelectQueryClause query)
		{
			if (query.Select.HasModifier || !query.GroupBy.IsEmpty)
				return true;

			if (!query.Where.IsEmpty)
			{
				if (QueryHelper.ContainsAggregationFunction(query.Where))
					return true;
			}

			return false;
		}

		bool MoveConditions(TableWord table, 
			IReadOnlyCollection<ITableNode> currentSources,
            SearchConditionWord       source,
            SearchConditionWord       destination,
            SearchConditionWord       common)
		{
			if (source.IsOr)
				return false;

            List<IAffirmWord>? predicatesForDestination = null;
            List<IAffirmWord>? predicatesCommon         = null;

            ITableNode[] tableSources = { (ITableNode)table };

			foreach (var p in source.Predicates)
			{
                var para = new List<ITableNode>();
                foreach (var i in currentSources)
                {
                    para.Add(i);
                }
                if (QueryHelper.IsDependsOnOuterSources(p, currentSources : para) &&
				    QueryHelper.IsDependsOnSources(p, tableSources))
				{
					predicatesForDestination ??= new();
					predicatesForDestination.Add(p);
				}
				else
				{
					predicatesCommon ??= new();
					predicatesCommon.Add(p);
				}
			}

			if (predicatesForDestination != null)
			{
				if (destination.IsOr)
					return false;
			}

			if (predicatesCommon != null)
			{
				if (common.IsOr)
					return false;
			}

			if (predicatesForDestination != null)
			{
				destination.AddRange(predicatesForDestination);
				foreach(var p in predicatesForDestination)
					source.Predicates.Remove(p);
			}

			if (predicatesCommon != null)
			{
				common.AddRange(predicatesCommon);
				foreach(var p in predicatesCommon)
					source.Predicates.Remove(p);
			}

			return true;
		}

		protected bool RemoveUpdateTableIfPossible(SelectQueryClause query, TableWord table, out TableSourceWord? source)
		{
			source = null;

			
			if (query.Select.HasSomeModifiers(SqlProviderFlags.IsUpdateSkipTakeSupported, SqlProviderFlags.IsUpdateTakeSupported) ||
				!query.GroupBy.IsEmpty)
				return false;

			if (table.SqlQueryExtensions?.Count > 0)
				return false;

			for (var i = 0; i < query.From.Tables.Count; i++)
			{
				var ts = query.From.Tables[i];
				var joins=ts.GetJoins();
				if (joins.All(j => j.JoinType is JoinKind.Inner or JoinKind.Left or JoinKind.Cross))
				{
					if (ts.FindISrc() == table)
					{
						source = ts as TableSourceWord;

						query.From.Tables.RemoveAt(i);
						for (var j = 0; j < joins.Count; j++)
						{
							query.From.Tables.Insert(i + j, joins[j].Table);
							query.Where.ConcatSearchCondition(joins[j].Condition);
						}

						source.GetJoins().Clear();

						return true;
					}

					for (var j = 0; j < joins.Count; j++)
					{
						var join = joins[j];
						if (join.Table.FindISrc() == table)
						{
							if (joins.Skip(j + 1).Any(sj => QueryHelper.IsDependsOnSource(sj, table)))
								return false;

							source = join.Table as TableSourceWord;

                            joins.RemoveAt(j);
							query.Where.ConcatSearchCondition(join.Condition);

							var tjoins = join.Table.GetJoins();

                            for (var sj = 0; j < tjoins.Count; j++)
							{
                                joins.Insert(j + sj, tjoins[sj]);
							}

							source.GetJoins().Clear();

							return true;
						}
					}
				}
			}

			return false;
		}

		static SelectQueryClause CloneQuery(
            SelectQueryClause                                  query,
			TableWord?                                    exceptTable,
			out Dictionary<Clause, Clause> replaceTree)
		{
			replaceTree = new Dictionary<Clause, Clause>();
			var clonedQuery = query.Clone(exceptTable, replaceTree, static (ut, e) =>
			{
				return e switch
				{
                    TableWord table when table       == ut => false,
                    FieldWord field when field.Table == ut => false,
					_ => true,
				};
			});

			replaceTree = CorrectReplaceTree(replaceTree, exceptTable);

			return clonedQuery;
		}

		protected static TableWord CloneTable(
			TableWord                                     tableToClone,
			out Dictionary<Clause, Clause> replaceTree)
		{
			replaceTree = new Dictionary<Clause, Clause>();
			var clonedQuery = tableToClone.Clone(tableToClone, replaceTree,
				static (t, e) => (e is TableWord table && table == t) || (e is FieldWord field && field.Table == t));

			return clonedQuery;
		}

		static Dictionary<Clause, Clause> CorrectReplaceTree(Dictionary<Clause, Clause> replaceTree, TableWord? exceptTable)
		{
			replaceTree = replaceTree
				.Where(pair =>
				{
					if (pair.Key is TableWord table)
						return table != exceptTable;
					if (pair.Key is ColumnWord)
						return true;
					if (pair.Key is FieldWord field)
						return field.Table != exceptTable;
					return false;
				})
				.ToDictionary(pair => pair.Key, pair => pair.Value);

			return replaceTree;
		}

		protected static TElement RemapCloned<TElement>(
			TElement                                  element,
			Dictionary<Clause, Clause>? mainTree,
			Dictionary<Clause, Clause>? innerTree = null,
			bool insideColumns = true)
		where TElement : Clause
        {
			if (mainTree == null && innerTree == null)
				return element;

			var newElement = element.Convert((mainTree, innerTree, insideColumns), static (v, expr) =>
			{
				var converted = v.Context.mainTree?.TryGetValue(expr, out var newValue) == true
					? newValue
					: expr;

				if (v.Context.innerTree != null)
				{
					converted = v.Context.innerTree.TryGetValue(converted, out newValue)
						? newValue
						: converted;
				}

				return converted;
			}, !insideColumns);

			return newElement;
		}

		static IEnumerable<(IExpWord target, IExpWord source, SelectQueryClause? query)> GenerateRows(
            IExpWord                            target,
            IExpWord                            source)
		{
			if (target is RowWord targetRow)
			{
				if (source is RowWord sourceRow)
				{
					if (targetRow.Values.Length != sourceRow.Values.Length)
						throw new InvalidOperationException("Target and Source SqlRows are different");

					for (var i = 0; i < targetRow.Values.Length; i++)
					{
						var targetRowValue = targetRow.Values[i];
						var sourceRowValue = sourceRow.Values[i];

						foreach (var r in GenerateRows(targetRowValue, sourceRowValue))
							yield return r;
					}

					yield break;
				}
				else if (source is ColumnWord { Expression: SelectQueryClause selectQuery })
				{
					for (var i = 0; i < targetRow.Values.Length; i++)
					{
						var targetRowValue = targetRow.Values[i];
						var sourceRowValue = selectQuery.Select.Columns[i].Expression;

						foreach (var r in GenerateRows(targetRowValue, sourceRowValue))
							yield return (r.target, r.source, selectQuery);
					}

					yield break;
				}
			}

			yield return (target, source, null);
		}

		static IEnumerable<(IExpWord, IExpWord)> GenerateRows(
            IExpWord                            target,
            IExpWord                            source,
			Dictionary<Clause, Clause>? mainTree,
			Dictionary<Clause, Clause>? innerTree,
            SelectQueryClause                               selectQuery)
		{
			if (target is RowWord targetRow && source is RowWord sourceRow)
			{
				if (targetRow.Values.Length != sourceRow.Values.Length)
					throw new InvalidOperationException("Target and Source SqlRows are different");

				for (var i = 0; i < targetRow.Values.Length; i++)
				{
					var tagetRowValue  = targetRow.Values[i];
					var sourceRowValue = sourceRow.Values[i];

					foreach (var r in GenerateRows(tagetRowValue, sourceRowValue, mainTree, innerTree, selectQuery))
						yield return r;
				}
			}
			else
			{
				var ex         = RemapCloned(source as Clause, mainTree, innerTree) as IExpWord;
				var columnExpr = selectQuery.Select.AddNewColumn(ex);

				yield return (target, columnExpr);
			}
		}

		protected UpdateSentence GetAlternativeUpdate(UpdateSentence updateStatement)
		{
			if (updateStatement.Update.Table == null)
				throw new InvalidOperationException();

			if (!updateStatement.SelectQuery.Select.HasSomeModifiers(SqlProviderFlags.IsUpdateSkipTakeSupported, SqlProviderFlags.IsUpdateTakeSupported)
				&& updateStatement.SelectQuery.From.Tables.Count == 1)
			{
				var sqlTableSource = updateStatement.SelectQuery.From.Tables[0];
				if (sqlTableSource.FindISrc() == updateStatement.Update.Table && sqlTableSource.GetJoins().Count == 0)
				{
					// Simple variant
					CorrectUpdateSetters(updateStatement);
					updateStatement.Update.TableSource = null;
					return updateStatement;
				}
			}

            SelectQueryClause?                              clonedQuery = null;
            Dictionary<Clause, Clause>? replaceTree = null;

			var needsComparison = !updateStatement.Update.HasComparison;

			CorrectUpdateSetters(updateStatement);

			if (NeedsEnvelopingForUpdate(updateStatement.SelectQuery))
			{
				updateStatement = QueryHelper.WrapQuery(updateStatement, updateStatement.SelectQuery, allowMutation : true);
			}
			
			needsComparison = false;

			if (!needsComparison)
			{
				// clone earlier, we need table before remove
				clonedQuery = CloneQuery(updateStatement.SelectQuery, null, out replaceTree);

				// trying to simplify query
				RemoveUpdateTableIfPossible(updateStatement.SelectQuery, updateStatement.Update.Table as TableWord, out _);
			}

			// It covers subqueries also. Simple subquery will have sourcesCount == 2
			if (QueryHelper.EnumerateAccessibleTableSources(updateStatement.SelectQuery).Any())
			{
				var sql = new SelectQueryClause { IsParameterDependent = updateStatement.IsParameterDependent  };

				var newUpdateStatement = new UpdateSentence(sql);

				if (clonedQuery == null)
					clonedQuery = CloneQuery(updateStatement.SelectQuery, null, out replaceTree);

				TableWord? tableToCompare = null;
				if (replaceTree!.TryGetValue(updateStatement.Update.Table as Clause, out var newTable))
				{
					tableToCompare = (TableWord)newTable;
				}

				if (tableToCompare != null)
				{
					replaceTree = CorrectReplaceTree(replaceTree, updateStatement.Update.Table as TableWord);

					ApplyUpdateTableComparison(clonedQuery, updateStatement.Update, tableToCompare);
				}

				CorrectUpdateSetters(updateStatement);

				clonedQuery.Select.Columns.Clear();
				var processUniversalUpdate = true;

				if (updateStatement.Update.Items.Count > 1 && SqlProviderFlags.RowConstructorSupport.HasFlag(RowFeature.Update))
				{
					// check that items depends just on update table
					//
					var isComplex = false;
					foreach (var item in updateStatement.Update.Items)
					{
						if (item.Column is RowWord)
							continue;

						var usedSources = new HashSet<ITableNode>();
						QueryHelper.GetUsedSources(item.Expression!, usedSources);
						usedSources.Remove(updateStatement.Update.Table!);
						if (replaceTree?.TryGetValue(updateStatement.Update.Table as Clause, out var replaced) == true)
							usedSources.Remove((ITableNode)replaced);

						if (usedSources.Count > 0)
						{
							isComplex = true;
							break;
						}
					}

					if (isComplex)
					{
						// generating Row constructor update

						processUniversalUpdate = false;

						var innerQuery = CloneQuery(clonedQuery, updateStatement.Update.Table as TableWord, out var innerTree);
						innerQuery.Select.Columns.Clear();

						var rows = new List<(IExpWord, IExpWord)>(updateStatement.Update.Items.Count);
						foreach (var item in updateStatement.Update.Items)
						{
							if (item.Expression == null)
								continue;

							rows.AddRange(GenerateRows(item.Column, item.Expression, replaceTree, innerTree, innerQuery));
						}

						var sqlRow        = new RowWord(rows.Select(r => r.Item1).ToArray());
						var newUpdateItem = new SetWord(sqlRow, innerQuery);

						newUpdateStatement.Update.Items.Clear();
						newUpdateStatement.Update.Items.Add(newUpdateItem);
					}
				}

				if (processUniversalUpdate)
				{
					foreach (var item in updateStatement.Update.Items)
					{
						if (item.Expression == null)
							continue;

						var usedSources = new HashSet<ITableNode>();

						var ex = item.Expression;

						QueryHelper.GetUsedSources(ex, usedSources);
						usedSources.Remove(updateStatement.Update.Table!);

						if (usedSources.Count > 0)
						{
							// it means that update value column depends on other tables and we have to generate more complicated query

							var innerQuery = CloneQuery(clonedQuery, updateStatement.Update.Table as TableWord, out var iterationTree);

							ex = RemapCloned(ex as Clause, replaceTree, iterationTree) as IExpWord;

							innerQuery.Select.Columns.Clear();

							innerQuery.Select.AddNew(ex);

							ex = innerQuery;
						}
						else
						{
							ex = RemapCloned(ex as Clause, replaceTree, null) as IExpWord;
						}

						item.Expression = ex;
						newUpdateStatement.Update.Items.Add(item);
					}

					foreach (var setExpression in newUpdateStatement.Update.Items)
					{
						var column = setExpression.Column;
						if (column is RowWord)
							continue;

						var field = QueryHelper.GetUnderlyingField(column);
						if (field == null)
							throw new LinqToDBException($"Expression {column} cannot be used for update field");

						setExpression.Column = field;
					}
				}

				if (updateStatement.Output != null)
				{
					newUpdateStatement.Output = RemapCloned(updateStatement.Output, replaceTree, null);
				}

				newUpdateStatement.Update.Table = updateStatement.Update.Table;
				newUpdateStatement.With         = updateStatement.With;

				newUpdateStatement.SelectQuery.Where.SearchCondition.AddExists(clonedQuery);

				updateStatement.Update.Items.Clear();

				updateStatement = newUpdateStatement;

				OptimizeQueries(updateStatement, updateStatement,  new EvaluateContext());
			}

			var (tableSource, _) = FindTableSource(new Stack<Clause>(), updateStatement.SelectQuery, updateStatement.Update.Table as TableWord);

			if (tableSource == null)
			{
				CorrectUpdateSetters(updateStatement);
			}

			return updateStatement;
		}

		protected void CorrectUpdateSetters(UpdateSentence updateStatement)
		{
			// remove current column wrapping
			foreach (var item in updateStatement.Update.Items)
			{
				if (item.Expression == null)
					continue;

				var expClause = item.Expression as Clause;


                item.Expression = expClause.Convert(updateStatement.SelectQuery, (v, e) =>
				{
					if (e is ColumnWord column && column.Parent == v.Context)
					{
						if (QueryHelper.UnwrapNullablity(column.Expression) is RowWord rowExpression)
						{
							if (!SqlProviderFlags.RowConstructorSupport.HasFlag(RowFeature.UpdateLiteral))
							{
								var rowSubquery = new SelectQueryClause();

								foreach (var expr in rowExpression.Values)
								{
									rowSubquery.Select.AddNew(expr);
								}

								return rowSubquery;
							}
						}

						return column.Expression as Clause;
					}
					return e;
				}) as IExpWord;

				if (item.Column is RowWord && item.Expression is SelectQueryClause subQuery)
				{
					if (subQuery.Select.Columns.Count==1 )
					{
						var column= subQuery.Select.Columns[0];
                        //{ From.Tables: [] }
                        if (column.Expression is SelectQueryClause  columnQuery && columnQuery.From.Tables.Count==0)
						{
							subQuery.Select.Columns.Clear();
							foreach (var c in columnQuery.Select.Columns.content)
							{
								subQuery.Select.AddNew(c.Expression);
							}
						}
						else if (column.Expression is RowWord rowExpression)
						{
							subQuery.Select.Columns.Clear();
							foreach (var value in rowExpression.Values)
							{
								subQuery.Select.AddNew(value);
							}
						}
					}
				}
			}
		}

		protected UpdateSentence DetachUpdateTableFromUpdateQuery(UpdateSentence updateStatement,  bool moveToJoin, bool addNewSource, out TableSourceWord newSource)
		{
			var updateTable = updateStatement.Update.Table;
			var alias       = updateStatement.Update.TableSource?.FindAlias();
			if (updateTable == null)
				throw new InvalidOperationException();

			CorrectUpdateColumns(updateStatement);

			var replacements = new Dictionary<Clause, Clause>();
			var tbClass = updateTable as Clause;
			var clonedTable = tbClass.Clone(replacements);
			//replacements.Remove(updateTable);

			updateStatement.SelectQuery.Replace(replacements);

			newSource                          = new TableSourceWord(updateTable, alias ?? "u");
			updateStatement.Update.Table       = updateTable;
			updateStatement.Update.TableSource = newSource;

			if (moveToJoin)
			{
				var currentSource = updateStatement.SelectQuery.From.Tables[0];
				var join          = new JoinTableWord(JoinKind.Inner, currentSource, false);

				updateStatement.SelectQuery.From.Tables.Clear();
				updateStatement.SelectQuery.From.Tables.Add(newSource);

				newSource.GetJoins().Add(join);

				ApplyUpdateTableComparison(join.Condition, updateStatement.SelectQuery, updateStatement.Update,
                    clonedTable as TableWord);
			}
			else
			{
				if (addNewSource)
				{
					updateStatement.SelectQuery.From.Tables.Insert(0, newSource);
				}

				ApplyUpdateTableComparison(updateStatement.SelectQuery, updateStatement.Update, clonedTable as TableWord
				);
			}

			return updateStatement;
		}

		static void CorrectUpdateColumns(UpdateSentence updateStatement)
		{
			// correct columns
			foreach (var item in updateStatement.Update.Items)
			{
				if (item.Column is ColumnWord column)
				{
					var field = QueryHelper.GetUnderlyingField(column.Expression);
					if (field == null)
						throw new InvalidOperationException($"Expression {column.Expression} cannot be used for update field");
					item.Column = field;
				}
			}
		}

		protected BaseSentence GetAlternativeUpdatePostgreSqlite(UpdateSentence statement)
		{
			if (statement.SelectQuery.Select.HasSomeModifiers(SqlProviderFlags.IsUpdateSkipTakeSupported, SqlProviderFlags.IsUpdateTakeSupported))
			{
				statement = QueryHelper.WrapQuery(statement, statement.SelectQuery, allowMutation: true);
			}

			var tableToUpdate = statement.Update.Table!;
			var tableSource   = statement.Update.TableSource;

			var isModified            = false;
			var hasUpdateTableInQuery = QueryHelper.HasTableInQuery(statement.SelectQuery, tableToUpdate as TableWord);

			if (hasUpdateTableInQuery)
			{
				if (RemoveUpdateTableIfPossible(statement.SelectQuery, tableToUpdate as TableWord, out _))
				{
					isModified            = true;
					hasUpdateTableInQuery = false;
				}
			}

			CorrectUpdateSetters(statement);

			if (hasUpdateTableInQuery)
			{
				TableSourceWord tableSourceWord;
				statement     = DetachUpdateTableFromUpdateQuery(statement,  moveToJoin: false, addNewSource: false, out tableSourceWord);
				tableToUpdate = statement.Update.Table!;
				tableSource = null;

				isModified = true;
			}

			if (isModified)
				OptimizeQueries(statement, statement,  new EvaluateContext());

			statement.Update.Table       = tableToUpdate;
			statement.Update.TableSource = tableSource;

			return statement;
		}

		/// <summary>
		/// Corrects situation when update table is located in JOIN clause.
		/// Usually it is generated by associations.
		/// </summary>
		/// <param name="statement">Statement to examine.</param>
		/// <returns>Corrected statement.</returns>
		protected UpdateSentence CorrectUpdateTable(UpdateSentence statement, bool leaveUpdateTableInQuery)
		{
			statement = BasicCorrectUpdate(statement, false);

			var tableToUpdate = statement.Update.Table;
			if (tableToUpdate != null)
			{
				var firstTable = statement.SelectQuery.From.Tables[0];

				if (firstTable.FindISrc() != tableToUpdate)
				{
					TableSourceWord? removedTableSource = null;

					if (QueryHelper.HasTableInQuery(statement.SelectQuery, tableToUpdate as TableWord))
					{
						if (!RemoveUpdateTableIfPossible(statement.SelectQuery, tableToUpdate as TableWord, out removedTableSource))
						{
							statement = DetachUpdateTableFromUpdateQuery(statement,  moveToJoin: false, addNewSource: leaveUpdateTableInQuery, out var newTableSource);
							statement.Update.TableSource = newTableSource;
						}
						else
						{
							statement.Update.TableSource = removedTableSource;
							statement.SelectQuery.From.Tables.Insert(0, removedTableSource!);
						}

						OptimizeQueries(statement, statement,  new EvaluateContext());
					}
					else if (leaveUpdateTableInQuery)
					{
						var ts = new TableSourceWord(tableToUpdate, "u");
						statement.Update.TableSource = ts;
						statement.SelectQuery.From.Tables.Insert(0, ts);
					}
				}
				else
				{
					statement.Update.TableSource = firstTable;
				}
			}

			CorrectUpdateSetters(statement);

			return statement;
		}

		#endregion

		public virtual bool IsParameterDependedQuery(SelectQueryClause query)
		{
			var takeValue = query.Select.TakeValue;
			if (takeValue != null)
			{
				var supportsParameter = SqlProviderFlags.GetAcceptsTakeAsParameterFlag(query);

				if (!supportsParameter)
				{
					if (takeValue.NodeType != ClauseType.SqlValue && takeValue.CanBeEvaluated(true))
						return true;
				}
				else if (takeValue.NodeType != ClauseType.SqlParameter)
					return true;

			}

			var skipValue = query.Select.SkipValue;
			if (skipValue != null)
			{

				var supportsParameter = SqlProviderFlags.GetIsSkipSupportedFlag(query.Select.TakeValue, query.Select.SkipValue)
										&& SqlProviderFlags.AcceptsTakeAsParameter;

				if (!supportsParameter)
				{
					if (skipValue.NodeType != ClauseType.SqlValue && skipValue.CanBeEvaluated(true))
						return true;
				}
				else if (skipValue.NodeType != ClauseType.SqlParameter)
					return true;
			}

			return false;
		}

		public virtual bool IsParameterDependedElement(NullabilityContext nullability, Clause element)
		{
			switch (element.NodeType)
			{
				case ClauseType.SelectStatement:
				case ClauseType.InsertStatement:
				case ClauseType.InsertOrUpdateStatement:
				case ClauseType.UpdateStatement:
				case ClauseType.DeleteStatement:
				case ClauseType.CreateTableStatement:
				case ClauseType.DropTableStatement:
				case ClauseType.MergeStatement:
				case ClauseType.MultiInsertStatement:
				{
					var statement = (BaseSentence)element;
					return statement.IsParameterDependent;
				}
				case ClauseType.SqlValuesTable:
				{
					return ((ValuesTableWord)element).Rows == null;
				}
				case ClauseType.SqlParameter:
				{
					var param = (ParameterWord)element;
					if (!param.IsQueryParameter)
						return true;
					if (param.NeedsCast)
						return true;

					return false;
				}
				case ClauseType.SqlQuery:
				{
					if (((SelectQueryClause)element).IsParameterDependent)
						return true;
					return IsParameterDependedQuery((SelectQueryClause)element);
				}
				case ClauseType.SqlBinaryExpression:
				{
					return element.IsMutable();
				}
				case ClauseType.ExprPredicate:
				{
					var exprExpr = (mooSQL.data.model.affirms.Expr)element;

					if (exprExpr.Expr1.IsMutable())
						return true;
					return false;
				}
				case ClauseType.ExprExprPredicate:
				{
					var exprExpr = (mooSQL.data.model.affirms.ExprExpr)element;

					var isMutable1 = exprExpr.Expr1.IsMutable();
					var isMutable2 = exprExpr.Expr2.IsMutable();

					if (isMutable1 && isMutable2)
						return true;

					if (isMutable1 && exprExpr.Expr2.CanBeEvaluated(false))
						return true;

					if (isMutable2 && exprExpr.Expr1.CanBeEvaluated(false))
						return true;

					if (isMutable1 && exprExpr.Expr1.ShouldCheckForNull(nullability))
						return true;

					if (isMutable2 && exprExpr.Expr2.ShouldCheckForNull(nullability))
						return true;

					return false;
				}
				case ClauseType.IsDistinctPredicate:
				{
					var expr = (IsDistinct)element;
					return expr.Expr1.IsMutable() || expr.Expr2.IsMutable();
				}
				case ClauseType.IsTruePredicate:
				{
					var isTruePredicate = (IsTrue)element;

					if (isTruePredicate.Expr1.IsMutable())
						return true;
					return false;
				}
				case ClauseType.InListPredicate:
				{
					return true;
				}
				case ClauseType.SearchStringPredicate:
				{
					var searchString = (mooSQL.data.model.affirms.SearchString)element;
					if (searchString.Expr2.NodeType != ClauseType.SqlValue)
						return true;

					return IsParameterDependedElement(nullability, searchString.CaseSensitive as Clause);
				}
				case ClauseType.SqlCase:
				{
					var sqlCase = (CaseWord)element;

					if (sqlCase.Cases.Any(c => c.Condition.CanBeEvaluated(true)))
						return true;

					return false;
				}
				case ClauseType.SqlCondition:
				{
					var sqlCondition = (ConditionWord)element;

					if (sqlCondition.Condition.CanBeEvaluated(true))
						return true;

					return false;
				}
				case ClauseType.SqlFunction:
				{
					var sqlFunc = (FunctionWord)element;
					switch (sqlFunc.Name)
					{
						case "Length":
						{
							if (sqlFunc.Parameters[0].CanBeEvaluated(true))
								return true;
							break;
						}
					}
					break;
				}
				case ClauseType.SqlInlinedExpression:
				case ClauseType.SqlInlinedToSqlExpression:
					return true;
			}

			return false;
		}


		public virtual BaseSentence FinalizeStatement(BaseSentence statement, EvaluateContext context )
		{
			var newStatement = TransformStatement(statement);
			newStatement = FinalizeUpdate(newStatement);

			if (SqlProviderFlags.IsParameterOrderDependent)
			{
				// ensure that parameters in expressions are well sorted
				newStatement = NormalizeExpressions(newStatement, context.ParameterValues == null);
			}

			return newStatement;
		}


		/// <summary>
		/// Moves Distinct query into another subquery. Useful when preserving ordering is required, because some providers do not support DISTINCT ORDER BY.
		/// <code>
		/// -- before
		/// SELECT DISTINCT TAKE 10 c1, c2
		/// FROM A
		/// ORDER BY c1
		/// -- after
		/// SELECT TAKE 10 B.c1, B.c2
		/// FROM
		///   (
		///     SELECT DISTINCT c1, c2
		///     FROM A
		///   ) B
		/// ORDER BY B.c1
		/// </code>
		/// </summary>
		/// <param name="statement">Statement which may contain take/skip and Distinct modifiers.</param>
		/// <param name="queryFilter">Query filter predicate to determine if query needs processing.</param>
		/// <returns>The same <paramref name="statement"/> or modified statement when transformation has been performed.</returns>
		protected BaseSentence SeparateDistinctFromPagination(BaseSentence statement, Func<SelectQueryClause, bool> queryFilter)
		{
			return QueryHelper.WrapQuery(
				queryFilter,
				statement,
				static (queryFilter, q, _) => q.Select.IsDistinct && queryFilter(q),
				static (_, p, q) =>
				{
					p.Select.SkipValue = q.Select.SkipValue;
					p.Select.Take(q.Select.TakeValue, q.Select.TakeHints);

					q.Select.SkipValue = null;
					q.Select.Take(null, null);

					QueryHelper.MoveOrderByUp(p, q);
				},
				allowMutation: true,
				withStack: false);
		}

		/// <summary>
		/// Replaces pagination by Window function ROW_NUMBER().
		/// </summary>
		/// <param name="context"><paramref name="predicate"/> context object.</param>
		/// <param name="statement">Statement which may contain take/skip modifiers.</param>
		/// <param name="supportsEmptyOrderBy">Indicates that database supports OVER () syntax.</param>
		/// <param name="predicate">Indicates when the transformation is needed</param>
		/// <returns>The same <paramref name="statement"/> or modified statement when transformation has been performed.</returns>
		protected BaseSentence ReplaceTakeSkipWithRowNumber<TContext>(TContext context, BaseSentence statement, Func<TContext, SelectQueryClause, bool> predicate, bool supportsEmptyOrderBy)
		{
			return QueryHelper.WrapQuery(
				(predicate, context, supportsEmptyOrderBy),
				statement,
				static (context, query, _) =>
				{
					if ((query.Select.TakeValue == null || query.Select.TakeHints != null) && query.Select.SkipValue == null)
						return 0;
					return context.predicate(context.context, query) ? 1 : 0;
				},
				static (context, queries) =>
				{
					var query = queries[queries.Count - 1];
					var processingQuery = queries[queries.Count - 2];

                    IReadOnlyCollection<OrderByWord>? orderByItems = null;
					if (!query.OrderBy.IsEmpty)
						orderByItems = query.OrderBy.Items;
					//else if (query.Select.Columns.Count > 0)
					//{
					//	orderByItems = query.Select.Columns
					//		.Select(static c => QueryHelper.NeedColumnForExpression(query, c, false))
					//		.Where(static e => e != null)
					//		.Take(1)
					//		.Select(static e => new SqlOrderByItem(e, false))
					//		.ToArray();
					//}

					if (orderByItems == null || orderByItems.Count == 0)
						orderByItems = context.supportsEmptyOrderBy ? new OrderByWord[] { } : new[] { new OrderByWord(new ExpressionWord("SELECT NULL"), false, false) };

					var orderBy = string.Join(", ",
						orderByItems.Select(static (oi, i) => oi.IsDescending ? $"{{{i}}} DESC" : $"{{{i}}}"));

					var parameters = orderByItems.Select(static oi => oi.Expression).ToArray();

					// careful here - don't clear it before orderByItems used
					query.OrderBy.Items.Clear();

					var rowNumberExpression = parameters.Length == 0
						? new ExpressionWord(typeof(long), "ROW_NUMBER() OVER ()", PrecedenceLv.Primary, SqlFlags.IsWindowFunction, ParametersNullabilityType.NotNullable, null)
						: new ExpressionWord(typeof(long), $"ROW_NUMBER() OVER (ORDER BY {orderBy})", PrecedenceLv.Primary, SqlFlags.IsWindowFunction, ParametersNullabilityType.NotNullable, null, parameters);

					var rowNumberColumn = query.Select.AddNewColumn(rowNumberExpression);
					rowNumberColumn.Alias = "RN";

					if (query.Select.SkipValue != null)
					{
						processingQuery.Where.EnsureConjunction().AddGreater(rowNumberColumn, query.Select.SkipValue, false);

						if (query.Select.TakeValue != null)
							processingQuery.Where.SearchCondition.AddLessOrEqual(rowNumberColumn,
								new BinaryWord(query.Select.SkipValue.SystemType!,
									query.Select.SkipValue, "+", query.Select.TakeValue), false);
					}
					else
					{
						processingQuery.Where.EnsureConjunction().AddLessOrEqual(rowNumberColumn, query.Select.TakeValue!, false);
					}

					query.Select.SkipValue = null;
					query.Select.Take(null, null);

				},
				allowMutation: true,
				withStack: false);
		}

		protected Clause OptimizeQueries(Clause startFrom, Clause root, EvaluateContext evaluationContext)
		{
			using var visitor = QueryHelper.SelectOptimizer.Allocate();

#if DEBUG
			// ReSharper disable once NotAccessedVariable
			var sqlText = startFrom.ToString();

			if (startFrom is BaseSentence statementBefore)
                QueryHelper.DebugCheckNesting(statementBefore, false);
#endif

			var result = visitor.Value.Optimize(startFrom, root, SqlProviderFlags, true,  DBLive,evaluationContext);

#if DEBUG
			// ReSharper disable once NotAccessedVariable
			var newSqlText = result.ToString();

			if (startFrom is BaseSentence statementAfter)
                QueryHelper.DebugCheckNesting(statementAfter, false);
#endif
			return result;
		}

		/// <summary>
		/// Alternative mechanism how to prevent loosing sorting in Distinct queries.
		/// </summary>
		/// <param name="statement">Statement which may contain Distinct queries.</param>
		/// <param name="queryFilter">Query filter predicate to determine if query needs processing.</param>
		/// <returns>The same <paramref name="statement"/> or modified statement when transformation has been performed.</returns>
		protected BaseSentence ReplaceDistinctOrderByWithRowNumber(BaseSentence statement, Func<SelectQueryClause, bool> queryFilter)
		{
			return QueryHelper.WrapQuery(
				queryFilter,
				statement,
				static (queryFilter, q, _) => (q.Select.IsDistinct && !q.Select.OrderBy.IsEmpty && queryFilter(q)) /*|| q.Select.TakeValue != null || q.Select.SkipValue != null*/,
				static (_, p, q) =>
				{
					var columnItems  = q.Select.Columns.content.Select(static c => c.Expression).ToList();
					var orderItems   = q.Select.OrderBy.Items.Select(static o => o.Expression).ToList();

					var projectionItemsCount = columnItems.Union(orderItems).Count();
					if (projectionItemsCount < columnItems.Count)
					{
						// Sort columns not in projection, transforming to
						/*
							 SELECT {S.columnItems}, S.RN FROM
							 (
								  SELECT {columnItems + orderItems}, RN = ROW_NUMBER() OVER (PARTITION BY {columnItems} ORDER BY {orderItems}) FROM T
							 )
							 WHERE S.RN = 1
						*/

						var orderByItems = q.Select.OrderBy.Items;

						var partitionBy = string.Join(", ", columnItems.Select(static (oi, i) => $"{{{i}}}"));

						var columns = new string[orderByItems.Count];
						for (var i = 0; i < columns.Length; i++)
							columns[i] = orderByItems[i].IsDescending
								? $"{{{i + columnItems.Count}}} DESC"
								: $"{{{i + columnItems.Count}}}";
						var orderBy = string.Join(", ", columns);

						var parameters = columnItems.Concat(orderByItems.Select(static oi => oi.Expression)).ToArray();

						var rnExpr = new ExpressionWord(typeof(long),
							$"ROW_NUMBER() OVER (PARTITION BY {partitionBy} ORDER BY {orderBy})", PrecedenceLv.Primary,
                            SqlFlags.IsWindowFunction, ParametersNullabilityType.NotNullable, null, parameters);

						var additionalProjection = orderItems.Except(columnItems);
						foreach (var expr in additionalProjection)
						{
							q.Select.AddNew(expr);
						}

						var rnColumn = q.Select.AddNewColumn(rnExpr);
						rnColumn.Alias = "RN";

						q.Select.IsDistinct = false;
						q.OrderBy.Items.Clear();
						p.Select.Where.EnsureConjunction().AddEqual(rnColumn, new ValueWord(1), false);
					}
					else
					{
						// All sorting columns in projection, transforming to
						/*
							 SELECT {S.columnItems} FROM
							 (
								  SELECT DISTINCT {columnItems} FROM T
							 )
							 ORDER BY {orderItems}

						*/

						QueryHelper.MoveOrderByUp(p, q);
					}
				},
				allowMutation: true,
				withStack: false);
		}

		protected BaseSentence CorrectMultiTableQueries(BaseSentence statement)
		{
			var isModified = false;

			statement.Visit(e =>
			{
				if (e.NodeType == ClauseType.SqlQuery)
				{
					var sqlQuery = (SelectQueryClause)e;

					if (sqlQuery.From.Tables.Count > 1)
					{
						// if multitable query has joins, we need to move tables to subquery and left joins on the current level
						//
						if (sqlQuery.From.Tables.Any(t => t.GetJoins().Count > 0))
						{
							var sub = new SelectQueryClause { DoNotRemove = true };

							sub.From.Tables.AddRange(sqlQuery.From.Tables);

							var restJoins = sqlQuery.From.Tables.SelectMany(t => t.GetJoins()).ToArray();

							sqlQuery.From.Tables.Clear();

							sqlQuery.From.Tables.Add(new TableSourceWord(sub, "sub", restJoins));

							sub.From.Tables.ForEach(t => t.GetJoins().Clear());

							isModified = true;
						}
					}

					if (SqlProviderFlags.IsCrossJoinSupported)
					{
						var allJoins = sqlQuery.From.Tables.SelectMany(t => t.GetJoins()).ToList();

						if (allJoins.Any(j => j.JoinType == JoinKind.Cross) && allJoins.Any(j => j.JoinType != JoinKind.Cross))
						{
							var sub = new SelectQueryClause { DoNotRemove = true };

							sub.From.Tables.AddRange(sqlQuery.From.Tables);
							sub.From.Tables.AddRange(allJoins.Where(j => j.JoinType == JoinKind.Cross).Select(j => j.Table));

							sqlQuery.From.Tables.Clear();

							sqlQuery.From.Tables.Add(new TableSourceWord(sub, "sub", allJoins.Where(j => j.JoinType != JoinKind.Cross).ToArray()));

							sub.From.Tables.ForEach(t => t.GetJoins().Clear());

							isModified = true;
						}
					}
				}
			});

			if (isModified)
			{
				var corrector = new SqlQueryColumnNestingCorrector();
				corrector.CorrectColumnNesting(statement);
			}

			return statement;
		}

	}
}
