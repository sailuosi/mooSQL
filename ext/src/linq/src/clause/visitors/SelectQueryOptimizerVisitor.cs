using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace mooSQL.linq.SqlQuery
{
	using Common;
	using Mapping;
	using Linq.Builder;

	using SqlProvider;
	using Visitors;
	using DataProvider;
	using System.Globalization;
	using Extensions;
	using mooSQL.data.model;
    using mooSQL.data.model.affirms;
    using mooSQL.utils;
    using mooSQL.data;

    public class SelectQueryOptimizerVisitor : SqlQueryVisitor
	{
		SQLProviderFlags  _providerFlags     = default!;
		public DBInstance DBLive {  get; set; }

		EvaluateContext _evaluationContext = default!;
		Clause     _root              = default!;
        Clause _rootElement       = default!;
        Clause[]   _dependencies      = default!;

		SelectQueryClause?      _correcting;
		int               _version;
		bool              _removeWeakJoins;

		SelectQueryClause?    _parentSelect;
		SetOperatorWord? _currentSetOperator;
		SelectQueryClause?    _applySelect;
		SelectQueryClause?    _inSubquery;
		bool            _isInRecursiveCte;
		bool            _isInsideNot;
		SelectQueryClause?    _updateQuery;

		SqlQueryColumnNestingCorrector _columnNestingCorrector      = new();
		SqlQueryColumnUsageCollector   _columnUsageCollector        = new();
		SqlQueryOrderByOptimizer       _orderByOptimizer            = new();
		MovingComplexityVisitor        _movingComplexityVisitor     = new();
		SqlExpressionOptimizerVisitor  _expressionOptimizerVisitor  = new(true);
		MovingOuterPredicateVisitor    _movingOuterPredicateVisitor = new();

		public SelectQueryOptimizerVisitor() : base(VisitMode.Modify, null)
		{
		}

		public Clause Optimize(
			Clause          root,
            Clause rootElement,
			SQLProviderFlags       providerFlags,
			bool                   removeWeakJoins,
			DBInstance DB,
			EvaluateContext      evaluationContext,
			params Clause[] dependencies)
		{
#if DEBUG
			if (root.NodeType == ClauseType.SelectStatement)
			{

			}
#endif

			_providerFlags     = providerFlags;
			_removeWeakJoins   = removeWeakJoins;
			DBLive = DB;
			_evaluationContext = evaluationContext;
			_root              = root;
			_rootElement       = rootElement;
			_isInsideNot       = false;
			_dependencies      = dependencies;
			_parentSelect      = default!;
			_applySelect       = default!;
			_inSubquery        = default!;
			_updateQuery       = default!;

			// OUTER APPLY Queries usually may have wrong nesting in WHERE clause.
			// Making it consistent in LINQ Translator is bad for performance and it is hard to implement task.
			// Function also detects that optimizations is needed
			//
			if (CorrectColumnsNesting())
			{
				do
				{
					ProcessElement(_root);

					_orderByOptimizer.OptimizeOrderBy(_root, _providerFlags);
					if (!_orderByOptimizer.IsOptimized)
						break;

				} while (true);

				if (removeWeakJoins)
				{
					// It means that we fully optimize query
					_columnUsageCollector.CollectUsedColumns(_rootElement);
					RemoveNotUsedColumns(_columnUsageCollector.UsedColumns, _root);

					// do it always, ignore dataOptions.LinqOptions.OptimizeJoins
					JoinsOptimizer.UnnestJoins(_root);

					// convert remaining nested joins to subqueries
					if (!_providerFlags.IsNestedJoinsSupported)
						JoinsOptimizer.UndoNestedJoins(_root);
				}
			}

			return _root;
		}

		bool CorrectColumnsNesting()
		{
			_columnNestingCorrector.CorrectColumnNesting(_root);

			return _columnNestingCorrector.HasSelectQuery;
		}

		static void RemoveNotUsedColumns(ICollection<ColumnWord> usedColumns, Clause element)
		{
			if (usedColumns.Count == 0)
				return;

			element.Visit(usedColumns, static (uc, e) =>
			{
				if (e is SelectClause select)
				{
					for (var i = select.Columns.Count - 1; i >= 0; i--)
					{
						var column = select.Columns[i];
						if (!uc.Contains(column))
						{
							var remove = true;

							if (select.From.Tables.Count == 0 && select.Columns.Count == 1)
							{
								remove = false;
							}

							if (remove)
								select.Columns.content.RemoveAt(i);
						}
					}

					if (!select.GroupBy.IsEmpty && select.Columns.Count == 0)
					{
						select.AddNew(new ValueWord(1));
					}
				}
			});
		}

		public override void Cleanup()
		{
			base.Cleanup();

			_providerFlags     = default!;
			_evaluationContext = default!;
			_root              = default!;
			_rootElement       = default!;
			_dependencies      = default!;
			_parentSelect      = default!;
			_applySelect       = default!;
			_version           = default;
			_isInRecursiveCte  = false;
			_updateQuery       = default;

			_columnNestingCorrector.Cleanup();
			_columnUsageCollector.Cleanup();
			_orderByOptimizer.Cleanup();
			_movingComplexityVisitor.Cleanup();
			_expressionOptimizerVisitor.Cleanup();
			_movingOuterPredicateVisitor.Cleanup();
		}

		public override Clause NotifyReplaced(Clause newElement, Clause oldElement)
		{
			++_version;
			return base.NotifyReplaced(newElement, oldElement);
		}

        public override Clause VisitJoinedTable(JoinTableWord element)
		{
			var saveQuery = _applySelect;

			if (element.JoinType == JoinKind.CrossApply || element.JoinType == JoinKind.OuterApply)
				_applySelect = element.Table.FindISrc() as SelectQueryClause;
			else
				_applySelect = null;

			var newElement = base.VisitJoinedTable(element);

			_applySelect = saveQuery;

			return newElement;
		}

		public override Clause VisitSelectQuery(SelectQueryClause selectQuery)
		{
			var saveSetOperatorCount = selectQuery.HasSetOperators ? selectQuery.SetOperators.Count : 0;
			var saveParent           = _parentSelect;
			var saveIsInsideNot      = _isInsideNot;
			
			_parentSelect = selectQuery;
			_isInsideNot = false;

			var newQuery = (SelectQueryClause)base.VisitSqlQuery(selectQuery);

			if (_correcting == null)
			{
				_parentSelect = selectQuery;

				if (saveParent == null)
				{
#if DEBUG
					var before = selectQuery.ToDebugString();
#endif
					// only once
					_expressionOptimizerVisitor.Optimize(_evaluationContext, NullabilityContext.GetContext(selectQuery), null,  selectQuery, visitQueries : true, isInsideNot : false, reduceBinary: false);
				}
				else
				{
					OptimizeColumns(selectQuery);
				}

				do
				{
					var currentVersion = _version;
					var isModified     = false;

					if (OptimizeSubQueries(selectQuery))
					{
						isModified = true;
					}
					
					if (MoveOuterJoinsToSubQuery(selectQuery, processMultiColumn: false))
					{
						isModified = true;
					}

					if (OptimizeApplies(selectQuery, _providerFlags.IsApplyJoinSupported))
					{
						isModified = true;
						EnsureReferencesCorrected(selectQuery);
					}

					if (MoveOuterJoinsToSubQuery(selectQuery, processMultiColumn: true))
					{
						isModified = true;
					}

					if (ResolveWeakJoins(selectQuery))
					{
						isModified = true;
						EnsureReferencesCorrected(selectQuery);
					}

					if (OptimizeJoinSubQueries(selectQuery))
					{
						isModified = true;
						EnsureReferencesCorrected(selectQuery);
					}

					if (CorrectRecursiveCteJoins(selectQuery))
					{
						isModified = true;
						EnsureReferencesCorrected(selectQuery);
					}

					if (CorrectMultiTables(selectQuery))
					{
						isModified = true;
					}

					if (FinalizeAndValidateInternal(selectQuery))
					{
						isModified = true;
					}

					if (currentVersion != _version)
					{
						isModified = true;
						EnsureReferencesCorrected(selectQuery);
					}

					if (!isModified)
					{
						break;
					}

				} while (true);

				if (saveParent == null)
				{
					// do expression optimization again
#if DEBUG
					var before = selectQuery.ToDebugString();
#endif
					CorrectEmptyInnerJoinsRecursive(selectQuery);

					_expressionOptimizerVisitor.Optimize(_evaluationContext, NullabilityContext.GetContext(selectQuery), null,  selectQuery, visitQueries : true, isInsideNot : false, reduceBinary: false);
				}

				if (saveSetOperatorCount != (selectQuery.HasSetOperators ? selectQuery.SetOperators.Count : 0))
				{
					// Do it again. Appended new SetOperators. For ensuring how it works check CteTests
					//
					newQuery = (SelectQueryClause)VisitSqlQuery(selectQuery);
				}

				_parentSelect = saveParent;
			}

			_isInsideNot = saveIsInsideNot;

			return newQuery;
		}

        public override Clause VisitSetOperator(SetOperatorWord element)
		{
			var saveCurrent = _currentSetOperator;

			_currentSetOperator = element;

			var newElement = base.VisitSetOperator(element);

			_currentSetOperator = saveCurrent;

			return newElement;
		}

        public override Clause VisitTableSource(TableSourceWord element)
		{
			var saveCurrent        = _currentSetOperator;

			_currentSetOperator = null;
			
			var newElement = base.VisitTableSource(element);

			_currentSetOperator = saveCurrent;

			return newElement;
		}

        public override Clause VisitAffirmInSubQuery(InSubQuery predicate)
		{
			var saveInsubquery = _inSubquery;

			_inSubquery = predicate.SubQuery;
			var newNode = base.VisitAffirmInSubQuery(predicate);
			_inSubquery = saveInsubquery;

			return newNode;
		}

        public override Clause VisitOrderByClause(OrderByClause element)
		{
			var newElement = (OrderByClause)base.VisitOrderByClause(element);

			for (int i = newElement.Items.Count - 1; i >= 0; i--)
			{
				var item = newElement.Items[i];
				if (QueryHelper.IsConstantFast(item.Expression))
					newElement.Items.RemoveAt(i);
			}

			return newElement;
		}

		public override Clause VisitUpdateSentence(UpdateSentence element)
		{
			_updateQuery = element.SelectQuery;
			var result = base.VisitUpdateSentence(element);
			_updateQuery = null;
			return result;
		}

		bool OptimizeUnions(SelectQueryClause selectQuery)
		{
			var isModified = false;

			if (selectQuery.From.Tables.Count == 1 &&
			    selectQuery.From.Tables[0].FindISrc() is SelectQueryClause { HasSetOperators: true } mainSubquery)
			{
				var isOk = true;

				if (!selectQuery.HasSetOperators)
				{
					isOk = selectQuery.OrderBy.IsEmpty && selectQuery.Where.IsEmpty && selectQuery.GroupBy.IsEmpty && !selectQuery.Select.HasModifier;
					if (isOk)
					{
						if (_currentSetOperator != null)
						{
							isOk = _currentSetOperator.Operation == mainSubquery.SetOperators[0].Operation;
						}
					}
				}

				if (isOk && mainSubquery.Select.Columns.Count == selectQuery.Select.Columns.Count)
				{
					var newIndexes = new Dictionary<IExpWord, int>(Utils
						.ObjectReferenceEqualityComparer<IExpWord>
						.Default);

					for (var i = 0; i < selectQuery.Select.Columns.Count; i++)
					{
						var scol = selectQuery.Select.Columns[i];

						if (!newIndexes.ContainsKey(scol.Expression))
							newIndexes[scol.Expression] = i;
					}

					var operation = selectQuery.HasSetOperators ? selectQuery.SetOperators[0].Operation : mainSubquery.SetOperators[0].Operation;

					if (mainSubquery.SetOperators.All(so => so.Operation == operation))
					{
						if (CheckSetColumns(newIndexes, mainSubquery, operation))
						{
							UpdateSetIndexes(newIndexes, mainSubquery, operation);
							selectQuery.SetOperators.InsertRange(0, mainSubquery.SetOperators);
							mainSubquery.SetOperators.Clear();

							var s = selectQuery.From.Tables[0].FindISrc();
							//待修改赋值
                            s = mainSubquery;

							for (var i = 0; i < selectQuery.Select.Columns.Count; i++)
							{
								var c = selectQuery.Select.Columns[i];
								c.Expression = mainSubquery.Select.Columns[i];
							}

							isModified = true;
						}
					}
				}
			}

			if (!selectQuery.HasSetOperators)
				return isModified;

			for (var index = 0; index < selectQuery.SetOperators.Count; index++)
			{
				var setOperator = selectQuery.SetOperators[index];

				if (setOperator.SelectQuery.From.Tables.Count == 1 &&
				    setOperator.SelectQuery.From.Tables[0].FindISrc() is SelectQueryClause { HasSetOperators: true } subQuery)
				{
					if (subQuery.SetOperators.All(so => so.Operation == setOperator.Operation))
					{
						var allColumns = setOperator.Operation != SetOperation.UnionAll;

						if (allColumns)
						{
							if (subQuery.Select.Columns.Count != selectQuery.Select.Columns.Count)
								continue;
						}

						var newIndexes =
							new Dictionary<IExpWord, int>(Utils.ObjectReferenceEqualityComparer<IExpWord>
								.Default);

						for (var i = 0; i < setOperator.SelectQuery.Select.Columns.Count; i++)
						{
							var scol = setOperator.SelectQuery.Select.Columns[i];

							if (!newIndexes.ContainsKey(scol.Expression))
								newIndexes[scol.Expression] = i;
						}

						if (!CheckSetColumns(newIndexes, subQuery, setOperator.Operation))
							continue;

						UpdateSetIndexes(newIndexes, subQuery, setOperator.Operation);

						setOperator.Modify(subQuery);
						selectQuery.SetOperators.InsertRange(index + 1, subQuery.SetOperators);
						subQuery.SetOperators.Clear();
						--index;

						isModified = true;
					}
				}
			}

			return isModified;
		}

		static void UpdateSetIndexes(Dictionary<IExpWord, int> newIndexes, SelectQueryClause setQuery, SetOperation setOperation)
		{
			if (setOperation == SetOperation.UnionAll)
			{
				for (var index = 0; index < setQuery.Select.Columns.Count; index++)
				{
					var column = setQuery.Select.Columns[index];
					if (!newIndexes.ContainsKey(column))
					{
						setQuery.Select.Columns.content.RemoveAt(index);

						foreach (var op in setQuery.SetOperators)
						{
							if (op.SelectQuery.SourceID == 115)
							{

							}
							if (index < op.SelectQuery.Select.Columns.Count)
								op.SelectQuery.Select.Columns.content.RemoveAt(index);
						}

						--index;
					}
				}
			}

			foreach (var pair in newIndexes.OrderBy(x => x.Value))
			{
				var currentIndex = setQuery.Select.Columns.content.FindIndex(c => ReferenceEquals(c, pair.Key));
				if (currentIndex < 0)
				{
					if (setOperation != SetOperation.UnionAll)
						throw new InvalidOperationException();

					foreach (var op in setQuery.SetOperators)
					{
						op.SelectQuery.Select.Columns.content.Insert(pair.Value, new ColumnWord(op.SelectQuery, pair.Key));
					}

					continue;
				}

				var newIndex = pair.Value;
				if (currentIndex != newIndex)
				{
					var uc = setQuery.Select.Columns[currentIndex];
					setQuery.Select.Columns.content.RemoveAt(currentIndex);
					setQuery.Select.Select.Columns.content.Insert(newIndex, uc);

					// change indexes in SetOperators
					foreach (var op in setQuery.SetOperators)
					{
						var column = op.SelectQuery.Select.Columns[currentIndex];
						op.SelectQuery.Select.Columns.content.RemoveAt(currentIndex);
						op.SelectQuery.Select.Columns.content.Insert(newIndex, column);
					}
				}
			}
		}

		static bool CheckSetColumns(Dictionary<IExpWord, int> newIndexes, SelectQueryClause setQuery, SetOperation setOperation)
		{
			foreach (var pair in newIndexes.OrderBy(x => x.Value))
			{
				var currentIndex = setQuery.Select.Columns.content.FindIndex(c => ReferenceEquals(c, pair.Key));
				if (currentIndex < 0)
				{
					if (setOperation != SetOperation.UnionAll)
						return false;

					if (!QueryHelper.IsConstantFast(pair.Key))
						return false;
				}
			}

			return true;
		}

		bool FinalizeAndValidateInternal(SelectQueryClause selectQuery)
		{
			var isModified = false;

			if (OptimizeGroupBy(selectQuery))
				isModified = true;

			if (OptimizeUnions(selectQuery))
				isModified = true;

			if (OptimizeDistinct(selectQuery))
				isModified = true;

			if (CorrectColumns(selectQuery))
				isModified = true;

			return isModified;
		}

		bool OptimizeGroupBy(SelectQueryClause selectQuery)
		{
			var isModified = false;

			if (!selectQuery.GroupBy.IsEmpty)
			{
				// Remove constants.
				//
				for (int i = selectQuery.GroupBy.Items.Count - 1; i >= 0; i--)
				{
					var groupByItem = selectQuery.GroupBy.Items[i];
					if (QueryHelper.IsConstantFast(groupByItem))
					{
						selectQuery.GroupBy.Items.RemoveAt(i);
						isModified = true;
					}
				}
			}

			return isModified;
		}

		bool CorrectColumns(SelectQueryClause selectQuery)
		{
			var isModified = false;
			if (!selectQuery.GroupBy.IsEmpty && selectQuery.Select.Columns.Count == 0)
			{
				isModified = true;
				foreach (var item in selectQuery.GroupBy.Items)
				{
					selectQuery.Select.Add(item);
				}
			}

			return isModified;
		}

		void EnsureReferencesCorrected(SelectQueryClause selectQuery)
		{
			if (_correcting != null)
				throw new InvalidOperationException();

			_correcting = selectQuery;

			base.Visit(selectQuery);

			_correcting = null;
		}

		bool IsRemovableJoin(JoinTableWord join)
		{
			if (join.IsWeak)
				return true;

			if (join.JoinType == JoinKind.Left)
			{
				if (join.Condition.IsFalse())
					return true;
			}

			if (join.JoinType is JoinKind.Left or JoinKind.OuterApply)
			{
				if ((join.Cardinality & SourceCardinality.One) != 0)
					return true;

				if (join.Table.FindISrc() is SelectQueryClause joinQuery)
				{
					if (joinQuery.Where.SearchCondition.IsFalse())
						return true;

					if (IsLimitedToOneRecord(joinQuery))
						return true;
				}
			}

			return false;
		}

		internal bool ResolveWeakJoins(SelectQueryClause selectQuery)
		{
			if (!_removeWeakJoins)
				return false;

			var isModified = false;

			foreach (var table in selectQuery.From.Tables)
			{
				var joins = table.GetJoins();
				for (var i = joins.Count - 1; i >= 0; i--)
				{
					var join = joins[i];

					if (IsRemovableJoin(join))
					{
						var sources = QueryHelper.EnumerateAccessibleSources(join.Table as TableSourceWord).ToList();
						var ignore  = new[] { join };
						if (QueryHelper.IsDependsOnSources(_rootElement, sources, ignore))
						{
							join.IsWeak = false;
							continue;
						}

						var moveNext = false;
						foreach (var d in _dependencies)
						{
							if (QueryHelper.IsDependsOnSources(d, sources, ignore))
							{
								join.IsWeak = false;
								moveNext    = true;
								break;
							}
						}

						if (moveNext)
							continue;

                        joins.RemoveAt(i);
						isModified = true;
					}
				}

				for (var i = joins.Count - 1; i >= 0; i--)
				{
					var join = joins[i];

					if (join.Table.FindISrc() is SelectQueryClause subQuery && (join.JoinType is JoinKind.Left or JoinKind.OuterApply))
					{
						var canRemoveEmptyJoin = false;

						if (join.JoinType == JoinKind.Left && join.Condition.IsFalse())
							canRemoveEmptyJoin = true;
						else if (join.JoinType == JoinKind.OuterApply && subQuery.Where.SearchCondition.IsFalse())
							canRemoveEmptyJoin = true;

						if (canRemoveEmptyJoin)
						{
							// we can substitute all values by null

							foreach (var column in subQuery.Select.Columns.content)
							{
								var nullValue = column.Expression as ValueWord;
								if (nullValue is not { Value: null })
								{
									var dbType = QueryHelper.GetDbDataType(column.Expression, DBLive);
									var type   = dbType.SystemType;
									if (!type.IsReferType())
										type = type.WrapNullable();
									nullValue = new ValueWord(dbType.WithSystemType(type), null);
								}

								NotifyReplaced(nullValue, column);
							}

                            joins.RemoveAt(i);
							isModified = true;
						}
					}
				}
			}

			return isModified;
		}

		static bool IsLimitedToOneRecord(SelectQueryClause query)
		{
			if (query.Select.TakeValue is ValueWord { Value: 1 })
				return true;

			if (query.GroupBy.IsEmpty && query.Select.Columns.Count > 0 && query.Select.Columns.content.All(c => QueryHelper.ContainsAggregationFunction(c.Expression)))
				return true;

			if (query.From.Tables.Count == 1 && query.From.Tables[0].FindISrc() is SelectQueryClause subQuery)
				return IsLimitedToOneRecord(subQuery);

			return false;
		}

		static bool IsComplexQuery(SelectQueryClause query)
		{
			var accessibleSources = new HashSet<ITableNode>();

			var complexFound = false;
			foreach (var source in QueryHelper.EnumerateAccessibleSources(query))
			{
				accessibleSources.Add(source);
				if (source is SelectQueryClause q && (q.From.Tables.Count != 1 || q.GroupBy.IsEmpty && QueryHelper.EnumerateJoins(q).Any()))
				{
					complexFound = true;
					break;
				}
			}

			if (complexFound)
				return true;

			var usedSources = new HashSet<ITableNode>();
			QueryHelper.CollectUsedSources(query, usedSources);

			return usedSources.Count > accessibleSources.Count;
		}

		bool OptimizeDistinct(SelectQueryClause selectQuery)
		{
			if (!selectQuery.Select.IsDistinct || !selectQuery.Select.OptimizeDistinct)
				return false;

			if (IsComplexQuery(selectQuery))
				return false;

			if (IsLimitedToOneRecord(selectQuery))
			{
				// we can simplify query if we take only one record
				selectQuery.Select.IsDistinct = false;
				return true;
			}

			if (!selectQuery.GroupBy.IsEmpty)
			{
				if (selectQuery.GroupBy.Items.All(gi => selectQuery.Select.Columns.content.Any(c => c.Expression.Equals(gi))))
				{
					selectQuery.GroupBy.Items.Clear();
					return true;
				}
			}

			var table = selectQuery.From.Tables[0];

			var keys = new List<IList<IExpWord>>();

			QueryHelper.CollectUniqueKeys(selectQuery, includeDistinct: false, keys);
			QueryHelper.CollectUniqueKeys(table as TableSourceWord, keys);
			if (keys.Count == 0)
				return false;

			var expressions = new HashSet<IExpWord>(selectQuery.Select.Columns.content.Select(static c => c.Expression));
			var foundUnique = false;

			foreach (var key in keys)
			{
				foundUnique = true;
				foreach (var expr in key)
				{
					if (!expressions.Contains(expr))
					{
						foundUnique = false;
						break;
					}
				}

				if (foundUnique)
					break;

				foundUnique = true;
				foreach (var expr in key)
				{
					var underlyingField = QueryHelper.GetUnderlyingField(expr);
					if (underlyingField == null || !expressions.Contains(underlyingField))
					{
						foundUnique = false;
						break;
					}
				}

				if (foundUnique)
					break;
			}

			var isModified = false;
			if (foundUnique)
			{
				// We have found that distinct columns has unique key, so we can remove distinct
				selectQuery.Select.IsDistinct = false;
				isModified = true;
			}

			return isModified;
		}

		static void ApplySubsequentOrder(SelectQueryClause mainQuery, SelectQueryClause subQuery)
		{
			if (subQuery.OrderBy.Items.Count > 0)
			{
				var filterItems = mainQuery.Select.IsDistinct || !mainQuery.GroupBy.IsEmpty;

				foreach (var item in subQuery.OrderBy.Items)
				{
					if (filterItems)
					{
						var skip = true;
						foreach (var column in mainQuery.Select.Columns.content)
						{
							if (column.Expression is ColumnWord sc && sc.Expression.Equals(item.Expression))
							{
								skip = false;
								break;
							}
						}

						if (skip)
							continue;
					}

					mainQuery.OrderBy.Expr(item.Expression, item.IsDescending, item.IsPositioned);
				}
			}
		}

		static void ApplySubQueryExtensions(SelectQueryClause mainQuery, SelectQueryClause subQuery)
		{
			if (subQuery.SqlQueryExtensions is not null)
				(mainQuery.SqlQueryExtensions ??= new()).AddRange(subQuery.SqlQueryExtensions);
		}

		static JoinKind ConvertApplyJoinType(JoinKind joinType)
		{
			var newJoinType = joinType switch
			{
				JoinKind.CrossApply => JoinKind.Inner,
				JoinKind.OuterApply => JoinKind.Left,
				JoinKind.FullApply  => JoinKind.Full,
				JoinKind.RightApply => JoinKind.Right,
				_ => throw new InvalidOperationException($"Invalid APPLY Join: {joinType}"),
			};

			return newJoinType;
		}

		bool OptimizeApply(JoinTableWord joinTable, bool isApplySupported)
		{
			var joinSource = joinTable.Table;

			var accessible = QueryHelper.EnumerateAccessibleSources(joinTable.Table as TableSourceWord).ToList();

			var optimized = false;

			if (!joinTable.CanConvertApply)
				return optimized;

			if (!QueryHelper.IsDependsOnOuterSources(joinSource.FindISrc()))
			{
				var newJoinType = ConvertApplyJoinType(joinTable.JoinType);

				joinTable.JoinType = newJoinType;
				optimized          = true;
				return optimized;
			}

			if (joinSource.FindISrc().NodeType == ClauseType.SqlQuery)
			{
				var sql   = (SelectQueryClause)joinSource.FindISrc();
				var isAgg = sql.Select.Columns.content.Any(static c => QueryHelper.IsAggregationOrWindowFunction(c.Expression));

				isApplySupported = isApplySupported && (joinTable.JoinType == JoinKind.CrossApply ||
				                                        joinTable.JoinType == JoinKind.OuterApply);

				if (isApplySupported && sql.Select.HasModifier && _providerFlags.IsSubQueryTakeSupported)
					return optimized;

				if (isApplySupported && isAgg)
					return optimized;

				if (isAgg)
					return optimized;

				var skipValue = sql.Select.SkipValue;
				var takeValue = sql.Select.TakeValue;

				if (sql.Select.TakeHints != null)
				{
					if (isApplySupported)
						return optimized;
					throw new LinqToDBException("SQL query requires TakeHints in CROSS/OUTER query, which are not supported by provider");
				}

				IExpWord?       rnExpression = null;
				List<IExpWord>? partitionBy  = null;

				if (skipValue != null || takeValue != null)
				{
					if (!_providerFlags.IsWindowFunctionsSupported)
						return optimized;

					var parameters = new List<IExpWord>();

					var found   = new HashSet<IExpWord>();

					if (sql.Select.IsDistinct)
					{
						found.AddRange(sql.Select.Columns.content.Select(c => c.Expression));
					}

					sql.Where.VisitAll(1, (ctx, e) =>
					{
						if (e is ExprExpr exprExpr)
						{
							var expr1 = SequenceHelper.UnwrapNullability(exprExpr.Expr1);
							var expr2 = SequenceHelper.UnwrapNullability(exprExpr.Expr2);

							var depended1 = QueryHelper.IsDependsOnOuterSources(expr1, currentSources : accessible);
							var depended2 = QueryHelper.IsDependsOnOuterSources(expr2, currentSources : accessible);

							if (depended1 && !depended2)
							{
								found.Add(expr2);
							}
							else if (!depended1 && depended2)
							{
								found.Add(expr1);
							}
						}
					});

					if (found.Count > 0)
					{
						partitionBy = found.ToList();
					}

					var rnBuilder = new StringBuilder();
					rnBuilder.Append("ROW_NUMBER() OVER (");

					if (partitionBy != null)
					{
						rnBuilder.Append("PARTITION BY ");
						for (int i = 0; i < partitionBy.Count; i++)
						{
							if (i > 0)
								rnBuilder.Append(", ");

							rnBuilder.Append(CultureInfo.InvariantCulture, $"{{{parameters.Count}}}");
							parameters.Add(partitionBy[i]);
						}
					}

					var orderByItems = sql.OrderBy.Items.ToList();

					if (sql.OrderBy.IsEmpty)
					{
						if (partitionBy != null)
							orderByItems.Add(new OrderByWord(partitionBy[0], false, false));
						else if (!_providerFlags.IsRowNumberWithoutOrderBySupported)
						{
							if (sql.Select.Columns.content.Count == 0)
							{
								throw new InvalidOperationException("OrderBy not specified for limited recordset.");
							}
							orderByItems.Add(new OrderByWord(sql.Select.Columns[0].Expression, false, false));
						}
					}

					if (orderByItems.Count > 0)
					{
						if (partitionBy != null)
							rnBuilder.Append(' ');

						rnBuilder.Append("ORDER BY ");
						for (int i = 0; i < orderByItems.Count; i++)
						{
							if (i > 0)
								rnBuilder.Append(", ");

							var orderItem = orderByItems[i];
							rnBuilder.Append(CultureInfo.InvariantCulture, $"{{{parameters.Count}}}");
							if (orderItem.IsDescending)
								rnBuilder.Append(" DESC");

							parameters.Add(orderItem.Expression);
						}
					}

					rnBuilder.Append(')');

					rnExpression = new ExpressionWord(typeof(long), rnBuilder.ToString(), PrecedenceLv.Primary,
						SqlFlags.IsWindowFunction, ParametersNullabilityType.NotNullable, null, parameters.ToArray());
				}

				var whereToIgnore = new List<Clause> { sql.Where, sql.Select };

				// add join conditions
				// Check SelectManyTests.Basic9 for Access
				foreach (var join in sql.From.Tables.SelectMany(t => t.GetJoins()))
				{
					if (join.JoinType == JoinKind.Inner && join.Table.FindISrc() is TableWord)
						whereToIgnore.Add(join.Condition);
					else
						break;
				}

                // we cannot optimize apply because reference to parent sources are used inside the query
                var para = new List<ISQLNode>();
                foreach (var i in whereToIgnore)
                {
                    para.Add(i);
                }
                if (QueryHelper.IsDependsOnOuterSources(sql, para))
					return optimized;

				var searchCondition = new List<IAffirmWord>();

				var predicates = sql.Where.SearchCondition.Predicates;

				if (predicates.Count > 0)
				{
					List<IAffirmWord>? toRemove = null;
					for (var i = predicates.Count - 1; i >= 0; i--)
					{
						var predicate = predicates[i];

						var contains = QueryHelper.IsDependsOnOuterSources(predicate, currentSources: accessible);

						if (contains)
						{
							if (rnExpression != null)
							{
								// we can only optimize equals
								if (predicate is not ExprExpr expExpr || expExpr.Operator != AffirmWord.Operator.Equal)
								{
									return optimized;
								}
							}

							if (!sql.GroupBy.IsEmpty)
							{
								// we can only optimize SqlPredicate.ExprExpr
								if (predicate is not ExprExpr expExpr)
								{
									return optimized;
								}

								// check that used key in grouping
								if (!sql.GroupBy.Items.Any(gi => QueryHelper.SameWithoutNullablity(gi, expExpr.Expr1) || QueryHelper.SameWithoutNullablity(gi, expExpr.Expr2)))
								{
									return optimized;
								}
							}

							toRemove ??= new List<IAffirmWord>();
							toRemove.Add(predicate);
						}
					}

					if (toRemove != null)
					{
						foreach (var predicate in toRemove)
						{
							searchCondition.Insert(0, predicate);
							predicates.Remove(predicate);
						}
					}
				}

				if (rnExpression != null)
				{
					// processing ROW_NUMBER

					sql.Select.SkipValue = null;
					sql.Select.TakeValue = null;

					var rnColumn = sql.Select.AddNewColumn(rnExpression);
					rnColumn.RawAlias = "rn";

					if (skipValue != null)
					{
						searchCondition.Add(new ExprExpr(rnColumn, AffirmWord.Operator.Greater, skipValue, null));

						if (takeValue != null)
						{
							searchCondition.Add(new ExprExpr(rnColumn, AffirmWord.Operator.LessOrEqual, new BinaryWord(skipValue.SystemType!, skipValue, "+", takeValue), null));
						}
					}
					else if (takeValue != null)
					{
						searchCondition.Add(new ExprExpr(rnColumn, AffirmWord.Operator.LessOrEqual, takeValue, null));
					}
					else if (sql.Select.IsDistinct)
					{
						sql.Select.IsDistinct = false;
						searchCondition.Add(new ExprExpr(rnColumn, AffirmWord.Operator.Equal, new ValueWord(1), null));
					}
				}

				var toCheck = QueryHelper.EnumerateAccessibleSources(sql).ToList();

				for (int i = 0; i < searchCondition.Count; i++)
				{
					var predicate = searchCondition[i];

					var newPredicate = _movingOuterPredicateVisitor.CorrectReferences(sql, toCheck, predicate);

					searchCondition[i] = newPredicate;
				}

				var newJoinType = ConvertApplyJoinType(joinTable.JoinType);

				joinTable.JoinType = newJoinType;
				joinTable.Condition.Predicates.AddRange(searchCondition);

				optimized = true;
			}

			return optimized;
		}

		bool IsColumnExpressionAllowedToMoveUp(SelectQueryClause parentQuery, NullabilityContext nullability, ColumnWord column, IExpWord columnExpression, bool ignoreWhere, bool inGrouping)
		{
			if (columnExpression.NodeType is ClauseType.Column or ClauseType.SqlRawSqlTable or ClauseType.SqlField or ClauseType.SqlValue or ClauseType.SqlParameter)
			{
				return true;
			}

			var underlying = QueryHelper.UnwrapExpression(columnExpression, false);
			if (!ReferenceEquals(underlying, columnExpression))
			{
				return IsColumnExpressionAllowedToMoveUp(parentQuery, nullability, column, underlying, ignoreWhere, inGrouping);
			}

			if (underlying is BinaryWord binary)
			{
				if (QueryHelper.IsConstantFast(binary.Expr1))
				{
					return IsColumnExpressionAllowedToMoveUp(parentQuery, nullability, column, binary.Expr2, ignoreWhere, inGrouping);
				}

				if (QueryHelper.IsConstantFast(binary.Expr2))
				{
					return IsColumnExpressionAllowedToMoveUp(parentQuery, nullability, column, binary.Expr1, ignoreWhere, inGrouping);
				}
			}

			var allowed = _movingComplexityVisitor.IsAllowedToMove(column, parent : parentQuery,
				nullability,
				_expressionOptimizerVisitor,
				_evaluationContext,
				// Elements which should be ignored while searching for usage
				column.Parent,
				_applySelect == parentQuery ? parentQuery.Where : null,
				!inGrouping && _applySelect == parentQuery ? parentQuery.Select : null,
				ignoreWhere ? parentQuery.Where : null
			);

			return allowed;
		}

		bool MoveSubQueryUp(SelectQueryClause parentQuery, TableSourceWord tableSource)
		{
			if (tableSource.Source is not SelectQueryClause subQuery)
				return false;

			if (subQuery.DoNotRemove)
				return false;

			if (subQuery.From.Tables.Count == 0)
			{
				// optimized in level up function
				return false;
			}

			if (!IsMovingUpValid(parentQuery, tableSource, subQuery, out var havingDetected))
			{
				return false;
			}

			// -------------------------------------------
			// Actual modification starts from this point
			//

			if (subQuery.HasSetOperators)
			{
				var newIndexes =
					new Dictionary<IExpWord, int>(Utils.ObjectReferenceEqualityComparer<IExpWord>
						.Default);

				if (parentQuery.Select.Columns.content.Count == 0)
				{
					for (var i = 0; i < subQuery.Select.Columns.Count; i++)
					{
						var scol = subQuery.Select.Columns[i];
						newIndexes[scol] = i;
					}
				}
				else
				{
					for (var i = 0; i < parentQuery.Select.Columns.Count; i++)
					{
						var scol = parentQuery.Select.Columns[i];

						if (!newIndexes.ContainsKey(scol.Expression))
							newIndexes[scol.Expression] = i;
					}
				}

				var operation = subQuery.SetOperators[0].Operation;

				if (!CheckSetColumns(newIndexes, subQuery, operation))
					return false;

				UpdateSetIndexes(newIndexes, subQuery, operation);

				parentQuery.SetOperators.InsertRange(0, subQuery.SetOperators);
				subQuery.SetOperators.Clear();
			}

			parentQuery.QueryName ??= subQuery.QueryName;

			if (!subQuery.GroupBy.IsEmpty)
			{
				parentQuery.GroupBy.Items.InsertRange(0, subQuery.GroupBy.Items);
				parentQuery.GroupBy.GroupingType = subQuery.GroupBy.GroupingType;

				if (havingDetected?.Count > 0)
				{
					// Should be checked earlier
					if (havingDetected.Count != parentQuery.Where.SearchCondition.Predicates.Count)
						throw new InvalidOperationException();

					// move Where to Having
					parentQuery.Having.SearchCondition.Predicates.InsertRange(0, parentQuery.Where.SearchCondition.Predicates);
					parentQuery.Where.SearchCondition.Predicates.Clear();
				}
			}

			if (!subQuery.Where.IsEmpty)
			{
				parentQuery.Where.SearchCondition.Predicates.InsertRange(0, subQuery.Where.SearchCondition.Predicates);
			}

			if (!subQuery.Having.IsEmpty)
			{
				parentQuery.Having.SearchCondition.Predicates.AddRange(subQuery.Having.SearchCondition.Predicates);
			}

			if (subQuery.Select.IsDistinct)
				parentQuery.Select.IsDistinct = true;

			if (subQuery.Select.TakeValue != null)
			{
				parentQuery.Select.Take(subQuery.Select.TakeValue, subQuery.Select.TakeHints);
			}

			if (subQuery.Select.SkipValue != null)
			{
				parentQuery.Select.SkipValue = subQuery.Select.SkipValue;
			}

			foreach (var column in subQuery.Select.Columns.content)
			{
				NotifyReplaced(column.Expression as Clause, column);
			}

			if (parentQuery.Select.Columns.Count == 0 && (subQuery.Select.IsDistinct || parentQuery.HasSetOperators))
			{
				foreach (var column in subQuery.Select.Columns.content)
				{
					parentQuery.Select.AddNew(column.Expression);
				}
			}

			// First table processing
			if (subQuery.From.Tables.Count > 0)
			{
				var subQueryTableSource = subQuery.From.Tables[0];

				NotifyReplaced(subQueryTableSource.All, subQuery.All);

				if (subQueryTableSource.GetJoins().Count > 0)
					tableSource.GetJoins().InsertRange(0, subQueryTableSource.GetJoins());

				tableSource.Source = subQueryTableSource.FindISrc();

				if (subQueryTableSource.HasUniqueKeys())
				{
					tableSource.UniqueKeys.AddRange(subQueryTableSource.FindUniqueKeys());
				}
				if (subQuery.HasUniqueKeys)
				{
					tableSource.UniqueKeys.AddRange(subQuery.UniqueKeys);
				}
			}

			if (subQuery.From.Tables.Count > 1)
			{
				var idx = parentQuery.From.Tables.IndexOf(tableSource);
				for (var i = subQuery.From.Tables.Count - 1; i >= 1; i--)
				{
					var subQueryTableSource = subQuery.From.Tables[i];
					parentQuery.From.Tables.Insert(idx + 1, subQueryTableSource);
				}

				// Move joins to last table
				//
				if (tableSource.GetJoins().Count > 0)
				{
					var lastTableSource = subQuery.From.Tables[subQuery.From.Tables.Count-1];
					lastTableSource.GetJoins().InsertRange(0, tableSource.GetJoins());
					tableSource.Joins.Clear();
				}
			}

			ApplySubQueryExtensions(parentQuery, subQuery);

			if (subQuery.OrderBy.Items.Count > 0 && !parentQuery.Select.Columns.content.Any(static c => QueryHelper.IsAggregationOrWindowFunction(c.Expression)))
			{
				ApplySubsequentOrder(parentQuery, subQuery);
			}

			return true;
		}

		bool IsMovingUpValid(SelectQueryClause parentQuery, TableSourceWord tableSource, SelectQueryClause subQuery, out HashSet<IAffirmWord>? havingDetected)
		{
			havingDetected = null;

			if (subQuery.IsSimple() && parentQuery.IsSimple())
			{
				if (parentQuery.Select.Columns.content.All(c => c.Expression is ColumnWord))
				{
					// shortcut
					return true;
				}
			}

			if (subQuery.From.Tables.Count > 1)
			{
				if (!_providerFlags.IsMultiTablesSupportsJoins)
				{
					if (QueryHelper.EnumerateJoins(parentQuery).Any())
					{
						// do not allow moving subquery with joins to multitable parent query
						return false;
					}
				}
			}

			// Trying to do not mix query hints
			if (subQuery.SqlQueryExtensions?.Count > 0)
			{
				if (tableSource.Joins.Count > 0 || parentQuery.From.Tables.Count > 1)
					return false;
			}

			if (!parentQuery.GroupBy.IsEmpty)
			{
				if (!subQuery.GroupBy.IsEmpty)
					return false;
				if (parentQuery.Select.Columns.Count == 0)
					return false;

				// Check that all grouping columns are simple
				if (parentQuery.GroupBy.EnumItems().Any(gi =>
				    {
					    if (gi is not ColumnWord sc)
						    return true;

					    if (QueryHelper.UnwrapNullablity(sc.Expression) is not (ColumnWord or FieldWord or ParameterWord or ValueWord))
						    return true;

					    return false;
				    }))
				{
					return false;
				}
			}

			var nullability = NullabilityContext.GetContext(parentQuery);

			// Check columns
			//

			foreach (var parentColumn in parentQuery.Select.Columns.content)
			{
				if (QueryHelper.ContainsAggregationFunction(parentColumn.Expression))
				{
					if (subQuery.Select.HasModifier || subQuery.HasSetOperators || !subQuery.GroupBy.IsEmpty || !subQuery.Having.IsEmpty)
					{
						// not allowed to move to parent if it has aggregates
						return false;
					}
				}

				if (QueryHelper.ContainsWindowFunction(parentColumn.Expression))
				{
					if (subQuery.Select.HasModifier || subQuery.HasSetOperators || !subQuery.Where.IsEmpty || !subQuery.Having.IsEmpty || !subQuery.GroupBy.IsEmpty)
					{
						// not allowed to break window
						return false;
					}
				}

				if (!parentQuery.GroupBy.IsEmpty)
				{
					if (QueryHelper.UnwrapNullablity(parentColumn.Expression) is ColumnWord sc && sc.Parent == subQuery)
					{
						var expr = QueryHelper.UnwrapNullablity(sc.Expression);

						// not allowed to move complex expressions for grouping
						if (expr.NodeType is not (ClauseType.SqlField or ClauseType.Column or ClauseType.SqlValue or ClauseType.SqlParameter))
						{
							return false;
						}
					}

				}
			}

			List<ColumnWord>? groupingConstants = null;

			foreach (var column in subQuery.Select.Columns.content)
			{
				if (QueryHelper.ContainsWindowFunction(column.Expression))
				{
					if (!parentQuery.IsSimpleOrSet())
					{
						// not allowed to break query window 
						return false;
					}
				}

				if (QueryHelper.ContainsAggregationFunction(column.Expression))
				{
					if (parentQuery.Having.HasElement(column) || parentQuery.Select.GroupBy.HasElement(column))
					{
						// aggregate moving not allowed
						return false;
					}

					if (!IsColumnExpressionAllowedToMoveUp(parentQuery, nullability, column, column.Expression, ignoreWhere : true, inGrouping: !subQuery.GroupBy.IsEmpty))
					{
						// Column expression is complex and Column has more than one reference
						return false;
					}
				}
				else
				{
					if (!IsColumnExpressionAllowedToMoveUp(parentQuery, nullability, column, column.Expression, ignoreWhere : false, inGrouping: !subQuery.GroupBy.IsEmpty))
					{
						// Column expression is complex and Column has more than one reference
						return false;
					}
				}

				if (QueryHelper.IsConstantFast(column.Expression))
				{
					if (parentQuery.GroupBy.HasElement(column))
					{
						groupingConstants ??= new List<ColumnWord>();
						groupingConstants.Add(column);
					}
				}
			}

			if (groupingConstants != null)
			{
				// All constants in grouping will be optimized to query which produce different query. Optimization will be done in 'OptimizeGroupBy'.
				// See 'GroupByConstantsEmpty' test. It will fail if this check is not performed.
				// 
				if (!parentQuery.GroupBy.EnumItems().Except(groupingConstants, Utils.ObjectReferenceEqualityComparer<IExpWord>.Default).Any())
				{
					return false;
				}
			}

			HashSet<IExpWord>? aggregates = null;

			// Check possible moving Where to Having
			//
			if (!subQuery.GroupBy.IsEmpty)
			{
				if (!parentQuery.Where.IsEmpty)
				{
					foreach (var subColumn in subQuery.Select.Columns.content)
					{
						if (QueryHelper.IsAggregationFunction(subColumn.Expression))
						{
							aggregates ??= new(Utils.ObjectReferenceEqualityComparer<IExpWord>.Default);
							aggregates.Add(subColumn);

							for (var i = 0; i < parentQuery.Where.SearchCondition.Predicates.Count; i++)
							{
								var p = parentQuery.Where.SearchCondition.Predicates[i];
								if (p.NodeType == ClauseType.ExprExprPredicate)
								{
									if (p.HasElement(subColumn))
									{
										havingDetected ??= new(Utils.ObjectReferenceEqualityComparer<IAffirmWord>.Default);
										havingDetected.Add(p);
									}
								}
								else
								{
									// no optimization allowed
									return false;
								}
							}
						}
					}

					if (havingDetected?.Count != parentQuery.Where.SearchCondition.Predicates.Count)
					{
						// everything should be moved to having
						return false;
					}
				}
			}

			// named sub-query cannot be removed
			if (subQuery.QueryName != null
			    // parent also has name
			    && (parentQuery.QueryName != null
			        // parent has other tables/sub-queries
			        || parentQuery.From.Tables.Count > 1
			        || parentQuery.From.Tables.Any(static t => t.GetJoins().Count > 0)))
			{
				return false;
			}

			if (_currentSetOperator?.SelectQuery == parentQuery || parentQuery.HasSetOperators)
			{
				// processing parent query as part of Set operation
				//

				if (subQuery.Select.HasModifier)
					return false;

				if (!subQuery.Select.OrderBy.IsEmpty)
				{
					return false;
				}
			}

			if (parentQuery.Select.IsDistinct)
			{
				// Common check for Distincts

				if (!subQuery.GroupBy.Having.IsEmpty)
					return false;

				if (subQuery.Select.SkipValue    != null || subQuery.Select.TakeValue    != null ||
				    parentQuery.Select.SkipValue != null || parentQuery.Select.TakeValue != null)
				{
					return false;
				}

				// Common column check for Distincts

				foreach (var parentColumn in parentQuery.Select.Columns.content)
				{
					if (parentColumn.Expression is not ColumnWord column || column.Parent != subQuery || QueryHelper.ContainsAggregationOrWindowFunction(parentColumn.Expression))
					{
						return false;
					}
				}
			}

			if (subQuery.Select.IsDistinct != parentQuery.Select.IsDistinct)
			{
				if (subQuery.Select.IsDistinct)
				{
					// Columns in parent query should match
					//

					if (!(parentQuery.Select.Columns.Count == 0 || subQuery.Select.Columns.content.All(sc =>
						    parentQuery.Select.Columns.content.Any(pc => ReferenceEquals(QueryHelper.UnwrapNullablity(pc.Expression), sc)))))
					{
						return false;
					}

					if (parentQuery.Select.Columns.Count > 0 && parentQuery.Select.Columns.Count != subQuery.Select.Columns.Count)
					{
						return false;
					}
				}
				else
				{
					// handling case when we have two DISTINCT
					// Note, columns already checked above
					//
				}
			}

			if (subQuery.Select.HasModifier)
			{
				// Do not optimize queries for update
				if (_updateQuery == parentQuery
					&& subQuery.Select.HasSomeModifiers(_providerFlags.IsUpdateSkipTakeSupported, _providerFlags.IsUpdateTakeSupported))
					return false;

				if (tableSource.Joins.Count > 0)
					return false;
				if (parentQuery.From.Tables.Count > 1)
					return false;

				if (!parentQuery.Select.OrderBy.IsEmpty)
					return false;

				if (!parentQuery.Select.Where.IsEmpty)
				{
					if (subQuery.Select.TakeValue != null || subQuery.Select.SkipValue != null)
						return false;
				}

				if (parentQuery.Select.Columns.content.Any(c => QueryHelper.ContainsAggregationOrWindowFunction(c.Expression)))
				{
					return false;
				}
			}

			if (subQuery.Select.HasModifier || !subQuery.Where.IsEmpty)
			{
				if (tableSource.Joins.Any(j => j.JoinType == JoinKind.Right || j.JoinType == JoinKind.RightApply ||
				                               j.JoinType == JoinKind.Full  || j.JoinType == JoinKind.FullApply))
				{
					return false;
				}
			}

			if (!_providerFlags.AcceptsOuterExpressionInAggregate)
			{
				if (QueryHelper.EnumerateJoins(subQuery).Any(j => j.JoinType != JoinKind.Inner))
				{
					if (subQuery.Select.Columns.content.Any(c => IsInsideAggregate(parentQuery, c)))
					{
						if (QueryHelper.IsDependsOnOuterSources(subQuery))
							return false;
					}
				}
			}

			if (parentQuery.GroupBy.IsEmpty && !subQuery.GroupBy.IsEmpty)
			{
				if (tableSource.Joins.Count > 0)
					return false;
				if (parentQuery.From.Tables.Count > 1)
					return false;

/*
				throw new NotImplementedException();

				if (selectQuery.Select.Columns.All(c => QueryHelper.IsAggregationFunction(c.Expression)))
					return false;
*/
			}

			if (subQuery.Select.TakeHints != null && parentQuery.Select.TakeValue != null)
				return false;

			if (subQuery.HasSetOperators)
			{
				if (parentQuery.HasSetOperators)
					return false;

				if (parentQuery.Select.Columns.Count != subQuery.Select.Columns.Count)
				{
					if (subQuery.SetOperators.Any(so => so.Operation != SetOperation.UnionAll))
						return false;
				}

				if (!parentQuery.Select.Where.IsEmpty || !parentQuery.Select.Having.IsEmpty || parentQuery.Select.HasModifier || !parentQuery.OrderBy.IsEmpty)
					return false;

				var operation = subQuery.SetOperators[0].Operation;

				if (_currentSetOperator != null && _currentSetOperator.Operation != operation)
					return false;

				if (!subQuery.SetOperators.All(so => so.Operation == operation))
					return false;
			}

			// Do not optimize t.Field IN (SELECT x FROM o)
			if (parentQuery == _inSubquery && (subQuery.Select.HasModifier || subQuery.HasSetOperators))
			{
				return false;
			}

			return true;
		}

		bool JoinMoveSubQueryUp(SelectQueryClause selectQuery, JoinTableWord joinTable)
		{
			if (joinTable.Table.FindISrc() is not SelectQueryClause subQuery)
				return false;

			if (subQuery.DoNotRemove)
				return false;

			if (subQuery.SqlQueryExtensions?.Count > 0)
				return false;

			if (subQuery.From.Tables.Count != 1)
				return false;

			// named sub-query cannot be removed
			if (subQuery.QueryName != null)
				return false;

			if (!subQuery.GroupBy.IsEmpty)
				return false;

			if (subQuery.Select.HasModifier)
				return false;

			if (subQuery.HasSetOperators)
				return false;

			if (!subQuery.GroupBy.IsEmpty)
				return false;

			// Rare case when LEFT join is empty. We move search condition up. See TestDefaultExpression_22 test.
			if (joinTable.JoinType == JoinKind.Left && subQuery.Where.SearchCondition.IsFalse())
			{
				subQuery.Where.SearchCondition.Predicates.Clear();
				joinTable.Condition.Predicates.Clear();
				joinTable.Condition.Predicates.Add(AffirmWord.False);

				// Continue in next loop
				return true;
			}

			var moveConditionToQuery = joinTable.JoinType == JoinKind.Inner || joinTable.JoinType == JoinKind.CrossApply;

			if (joinTable.JoinType != JoinKind.Inner)
			{
				if (!subQuery.Where.IsEmpty)
				{
					if (joinTable.JoinType == JoinKind.OuterApply)
					{
						if (_providerFlags.IsOuterApplyJoinSupportsCondition)
							moveConditionToQuery = false;
						else
							return false;
					}
					else if (joinTable.JoinType == JoinKind.CrossApply)
					{
						if (_providerFlags.IsCrossApplyJoinSupportsCondition)
							moveConditionToQuery = false;
					}
					else if (joinTable.JoinType == JoinKind.Left)
					{
						if (joinTable.Condition.IsTrue())
						{
							// See `PostgreSQLExtensionsTests.GenerateSeries`
							if (subQuery.From.Tables[0].GetJoins().Count > 0)
							{
								// See 'Issue2199Tests.LeftJoinTests2'
								return false;
							}
						}

						moveConditionToQuery = false;
					}
					else
					{
						return false;
					}
				}

				if (!_providerFlags.IsOuterJoinSupportsInnerJoin)
				{
					// Especially for Access. See ComplexTests.Contains3
					//
					if (QueryHelper.EnumerateJoins(subQuery).Any(j => j.JoinType == JoinKind.Inner))
						return false;
				}

				if (!subQuery.Select.Columns.content.All(c =>
					{
						var columnExpression = QueryHelper.UnwrapCastAndNullability(c.Expression);

						if (columnExpression is ColumnWord or FieldWord or TableWord or BinaryWord)
							return true;
						if (columnExpression is FunctionWord func)
							return !func.IsAggregate;
						return false;
					}))
				{
					return false;
				}
			}

			if (subQuery.Select.Columns.content.Any(c => QueryHelper.IsAggregationOrWindowFunction(c.Expression)))
				return false;

			// Actual modification starts from this point
			//

			if (!subQuery.Where.IsEmpty)
			{
				if (moveConditionToQuery)
				{
					selectQuery.Where.EnsureConjunction().Predicates.AddRange(subQuery.Where.SearchCondition.Predicates);
				}
				else
				{
					joinTable.Condition.Predicates.AddRange(subQuery.Where.SearchCondition.Predicates);
				}
			}

			if (selectQuery.Select.Columns.Count == 0)
			{
				foreach(var column in subQuery.Select.Columns.content)
				{
					selectQuery.Select.AddColumn(column.Expression);
				}
			}

			foreach (var column in subQuery.Select.Columns.content)
			{
				NotifyReplaced(column.Expression as Clause, column);
			}

			if (subQuery.OrderBy.Items.Count > 0 && !selectQuery.Select.Columns.content.All(static c => QueryHelper.IsAggregationOrWindowFunction(c.Expression)))
			{
				ApplySubsequentOrder(selectQuery, subQuery);
			}

			var subQueryTableSource = subQuery.From.Tables[0];
			joinTable.Table.GetJoins().AddRange(subQueryTableSource.GetJoins());
			//joinTable.Table.Source = subQueryTableSource.FindISrc();

			//if (joinTable.Table.RawAlias == null && subQueryTableSource.RawAlias != null)
			//	joinTable.Table.Alias = subQueryTableSource.RawAlias;

			if (!joinTable.Table.HasUniqueKeys() && subQueryTableSource.HasUniqueKeys())
				joinTable.Table.FindUniqueKeys().AddRange(subQueryTableSource.FindUniqueKeys());

			return true;
		}

		public override Clause VisitFromClause(FromClause element)
		{
			element = (FromClause)base.VisitFromClause(element);

			if (_correcting != null)
				return element;

			return element;
		}

		bool OptimizeSubQueries(SelectQueryClause selectQuery)
		{
			var replaced = false;

			for (var i = 0; i < selectQuery.From.Tables.Count; i++)
			{
				var tableSource = selectQuery.From.Tables[i];
				if (MoveSubQueryUp(selectQuery, tableSource as TableSourceWord))
				{
					replaced = true;

					EnsureReferencesCorrected(selectQuery);

					--i; // repeat again
				}
			}

			// Removing subqueries which has no tables

			for (var i = 0; i < selectQuery.From.Tables.Count; i++)
			{
				var tableSource = selectQuery.From.Tables[i];
				if (tableSource.GetJoins().Count == 0 && tableSource.FindISrc() is SelectQueryClause { From.Tables.Count: 0, Where.IsEmpty: true, HasSetOperators: false } subQuery)
				{
					if (selectQuery.From.Tables.Count == 1)
					{
						if (!selectQuery.GroupBy.IsEmpty
						    || !selectQuery.Having.IsEmpty
						    || !selectQuery.OrderBy.IsEmpty)
						{
							continue;
						}
					}

					replaced = true;

					foreach (var c in subQuery.Select.Columns.content)
					{
						NotifyReplaced(c.Expression as Clause, c);
					}

					selectQuery.From.Tables.RemoveAt(i);

					--i; // repeat again
				}
			}

			return replaced;
		}

		bool OptimizeJoinSubQueries(SelectQueryClause selectQuery)
		{
			var replaced = false;

			for (var i = 0; i < selectQuery.From.Tables.Count; i++)
			{
				var tableSource = selectQuery.From.Tables[i];
				var joins = tableSource.GetJoins();
				if (joins.Count > 0)
				{
					foreach (var join in joins)
					{
						if (JoinMoveSubQueryUp(selectQuery, join))
							replaced = true;
					}
				}
			}

			for (var i = 0; i < selectQuery.From.Tables.Count; i++)
			{
				var tableSource = selectQuery.From.Tables[i];
                var joins = tableSource.GetJoins();
                if (joins.Count > 0)
				{
					for (var index = 0; index < joins.Count; index++)
					{
						var join = joins[index];
						if (join.JoinType == JoinKind.Inner && join.Table.FindISrc() is SelectQueryClause joinQuery)
						{
							if (joinQuery.From.Tables.Count == 0)
							{
								replaced = true;

								foreach (var c in joinQuery.Select.Columns.content)
								{
									NotifyReplaced(c.Expression as Clause, c);
								}

                                joins.RemoveAt(index);
								--index;
							}
						}
					}
				}
			}

			return replaced;
		}

		bool CorrectRecursiveCteJoins(SelectQueryClause selectQuery)
		{
			var isModified = false;

			if (!_providerFlags.IsRecursiveCTEJoinWithConditionSupported && _isInRecursiveCte)
			{
				for (int i = 0; i < selectQuery.From.Tables.Count; i++)
				{
					var ts = selectQuery.From.Tables[i];
					var joins = ts.GetJoins();
					if (joins.Count > 0)
					{
						var join = joins[0];

						if (join.JoinType != JoinKind.Inner)
							break;

						isModified = true;
						selectQuery.From.Tables.Insert(i + 1, join.Table);
						selectQuery.Where.ConcatSearchCondition(join.Condition);
                        joins.RemoveAt(0);
						--i;
					}
				}
			}

			return isModified;
		}

		SelectQueryClause MoveMutliTablesToSubquery(SelectQueryClause selectQuery)
		{
			var joins = new List<JoinTableWord>(selectQuery.From.Tables.Count);
			foreach (var t in selectQuery.From.Tables)
			{
				joins.AddRange(t.GetJoins());
				t.GetJoins().Clear();
			}

			var subQuery = new SelectQueryClause();

			var tables = selectQuery.From.Tables.ToArray();
			selectQuery.From.Tables.Clear();

			var baseTable = new TableSourceWord(subQuery, "cross");
			baseTable.Joins.AddRange(joins);
			selectQuery.Select.From.Tables.Add(baseTable);
			subQuery.From.Tables.AddRange(tables);

			var sources     = new HashSet<ITableNode>(tables.Select(static t => t.FindISrc()));
			var foundFields = new HashSet<IExpWord>();

			QueryHelper.CollectDependencies(_rootElement, sources, foundFields);

			var toReplace = new Dictionary<Clause, Clause>(foundFields.Count);
			foreach (var expr in foundFields)
				toReplace.Add(expr as Clause, subQuery.Select.AddColumn(expr));

			if (toReplace.Count > 0)
			{
				_rootElement.Replace(toReplace, subQuery.Select);
			}

			subQuery.DoNotRemove = true;

			return subQuery;
		}

		bool CorrectMultiTables(SelectQueryClause selectQuery)
		{
			if (_providerFlags.IsMultiTablesSupportsJoins)
				return false;

			var isModified = false;

			if (selectQuery.From.Tables.Count > 1)
			{
				if (QueryHelper.EnumerateJoins(selectQuery).Any())
				{
					MoveMutliTablesToSubquery(selectQuery);

					isModified = true;
				}
			}

			return isModified;
		}

		bool OptimizeColumns(SelectQueryClause selectQuery)
		{
			if (_parentSelect == null)
				return false;

			if (_currentSetOperator != null)
				return false;

			if (selectQuery.HasSetOperators)
				return false;

			var isModified = false;

			for (var index = 0; index < selectQuery.Select.Columns.Count; index++)
			{
				var c = selectQuery.Select.Columns[index];
				for(var nextIndex = index + 1; nextIndex < selectQuery.Select.Columns.Count; nextIndex++)
				{
					var nc = selectQuery.Select.Columns[nextIndex];

					if (ReferenceEquals(c.Expression, nc.Expression))
					{
						selectQuery.Select.Columns.content.RemoveAt(nextIndex);
						--nextIndex;

						NotifyReplaced(c, nc);

						isModified = true;
					}
				}
			}

			return isModified;
		}

		bool OptimizeApplies(SelectQueryClause selectQuery, bool isApplySupported)
		{
			var optimized = false;

			foreach (var table in selectQuery.From.Tables)
			{
				foreach (var join in table.GetJoins())
				{
					if (join.JoinType == JoinKind.CrossApply || join.JoinType == JoinKind.OuterApply || join.JoinType == JoinKind.FullApply || join.JoinType == JoinKind.RightApply)
					{
						if (OptimizeApply(join, isApplySupported))
						{
							optimized = true;
						}
					}
				}
			}

			return optimized;
		}

		void CorrectEmptyInnerJoinsRecursive(SelectQueryClause selectQuery)
		{
			selectQuery.Visit(e =>
			{
				if (e is SelectQueryClause sq)
					CorrectEmptyInnerJoinsInQuery(sq);
			});
		}

		bool CorrectEmptyInnerJoinsInQuery(SelectQueryClause selectQuery)
		{
			var isModified = false;

			for (var queryTableIndex = 0; queryTableIndex < selectQuery.From.Tables.Count; queryTableIndex++)
			{
				var table = selectQuery.From.Tables[queryTableIndex];
				var joins = table.GetJoins();
				for (var joinIndex = 0; joinIndex < joins.Count; joinIndex++)
				{
					var join = joins[joinIndex];
					var jJoins = join.Table.GetJoins();
					if (join.JoinType == JoinKind.Inner && join.Condition.IsTrue())
					{
						if (_providerFlags.IsCrossJoinSupported && (joins.Count > 1 || !QueryHelper.IsDependsOnSource(selectQuery.Where, join.Table.FindISrc())))
						{
							join.JoinType = JoinKind.Cross;
							if (jJoins.Count > 0)
							{
								// move joins to the same level as parent table
								for (var ij = 0; ij < jJoins.Count; ij++)
								{
                                    joins.Insert(joinIndex + ij + 1, jJoins[ij]);
								}
                                jJoins.Clear();
							}
							isModified = true;
						}
						else 
						{
							selectQuery.From.Tables.Insert(queryTableIndex + 1, join.Table);
                            joins.RemoveAt(joinIndex);

							// move joins INNER JOIN table from parent
							for (var ij = 0; ij < joins.Count; ij++)
							{
                                jJoins.Insert(ij, joins[ij]);
							}

                            joins.Clear();

							--joinIndex;
							isModified = true;
						}
					}
				}
			}

			return isModified;
		}

		//protected override IExpWord VisitSqlColumnExpression(ColumnWord column, IExpWord expression)
		//{
		//	expression      = base.VisitSqlColumnExpression(column, expression);

		//	expression = QueryHelper.SimplifyColumnExpression(expression);

		//	return expression;
		//}

		static bool IsLimitedToOneRecord(SelectQueryClause parentQuery, SelectQueryClause selectQuery, EvaluateContext context)
		{
			if (selectQuery.Select.TakeValue != null &&
			    selectQuery.Select.TakeValue.TryEvaluateExpression(context, out var takeValue))
			{
				if (takeValue is int intValue)
				{
					return intValue == 1;
				}
			}

			if (selectQuery.Select.Columns.Count == 1)
			{
				var column = selectQuery.Select.Columns[0];
				if (QueryHelper.IsAggregationFunction(column.Expression) && !QueryHelper.IsWindowFunction(column.Expression))
					return true;

				if (selectQuery.Select.From.Tables.Count == 0)
					return true;
			}

			if (!selectQuery.Where.IsEmpty)
			{
				var keys = new List<IList<IExpWord>>();
				QueryHelper.CollectUniqueKeys(selectQuery, true, keys);

				if (keys.Count > 0)
				{
					var outerSources = QueryHelper.EnumerateAccessibleSources(parentQuery)
						.Where(s => s != selectQuery)
						.ToList();

					var innerSources = QueryHelper.EnumerateAccessibleSources(selectQuery).ToList();

					var toIgnore = new List<ITableNode>() { selectQuery };

					var foundEquality = new List<IExpWord>();
					foreach (var p in selectQuery.Where.SearchCondition.Predicates)
					{
						if (p is ExprExpr { Operator: AffirmWord.Operator.Equal } equality)
						{
							var left  = QueryHelper.UnwrapNullablity(equality.Expr1);
							var right = QueryHelper.UnwrapNullablity(equality.Expr2);

							if (!left.Equals(right))
							{
								if (QueryHelper.IsDependsOnSources(left, outerSources, toIgnore) && QueryHelper.IsDependsOnSources(right, innerSources))
									foundEquality.Add(right);
								else if (QueryHelper.IsDependsOnSources(right, outerSources, toIgnore) && QueryHelper.IsDependsOnSources(left, innerSources))
									foundEquality.Add(left);
							}
						}
					}

					// all keys should be matched
					if (keys.Any(kl => kl.All(k => foundEquality.Contains(k))))
						return true;
				}
			}

			return false;
		}

		static bool IsUniqueUsage(SelectQueryClause rootQuery, ColumnWord column)
		{
			int counter = 0;

			rootQuery.VisitParentFirstAll(e =>
			{
				// do not search in the same query
				if (e is SelectQueryClause sq && sq == column.Parent)
					return false;

				if (e == column)
				{
					++counter;
				}

				return counter < 2;
			});

			return counter <= 1;
		}

		static bool IsInsideAggregate(Clause testedElement, ColumnWord column)
		{
			bool result = false;

			testedElement.VisitParentFirstAll(e =>
			{
				// do not search in the same query
				if (QueryHelper.IsAggregationFunction(e))
				{
					result = result || null != e.Find(1, (_, te) => ReferenceEquals(te, column));
					return false;
				}

				return !result;
			});

			return result;
		}

		void MoveDuplicateUsageToSubQuery(SelectQueryClause query)
		{
			var subQuery = new SelectQueryClause();

			subQuery.DoNotRemove = true;

			subQuery.From.Tables.AddRange(query.From.Tables);

			query.Select.From.Tables.Clear();
			_ = query.Select.From.FindTableSrc(subQuery);

			_columnNestingCorrector.CorrectColumnNesting(query);
		}

		bool ProviderOuterCanHandleSeveralColumnsQuery(SelectQueryClause selectQuery)
		{
			if (_providerFlags.IsApplyJoinSupported)
				return true;

			if (_providerFlags.IsWindowFunctionsSupported)
			{
				if (!selectQuery.GroupBy.IsEmpty)
				{
					return false;
				}

				if (selectQuery.Select.TakeValue != null)
				{
					if (!selectQuery.Where.IsEmpty)
					{
						if (selectQuery.Where.SearchCondition.Predicates.Any(predicate => predicate is not ExprExpr expExpr || expExpr.Operator != AffirmWord.Operator.Equal))
						{
							// OuterApply cannot be converted in this case
							return false;
						}
					}
				}

				// provider can handle this query
				return true;
			}

			return false;
		}

		bool MoveOuterJoinsToSubQuery(SelectQueryClause selectQuery, bool processMultiColumn)
		{
			if (!_providerFlags.IsSubQueryColumnSupported)
				return false;

			var currentVersion = _version;

			EvaluateContext? evaluationContext = null;

			var selectQueries = QueryHelper.EnumerateAccessibleSources(selectQuery).OfType<SelectQueryClause>().ToList();
			foreach (var sq in selectQueries)
			{
				for (var ti = 0; ti < sq.From.Tables.Count; ti++)
				{
					var table = sq.From.Tables[ti];

					for (int j = table.GetJoins().Count - 1; j >= 0; j--)
					{
						var join            = table.GetJoins()[j];
						var joinQuery       = join.Table.FindISrc() as SelectQueryClause;

						if (join.JoinType == JoinKind.OuterApply ||
						    join.JoinType == JoinKind.Left       ||
						    join.JoinType == JoinKind.CrossApply)
						{
							if (join.JoinType == JoinKind.CrossApply)
							{
								if (_applySelect == null)
								{
									continue;
								}
							}

							evaluationContext ??= new EvaluateContext();

							if (joinQuery != null && joinQuery.Select.Columns.Count > 0)
							{
								if (joinQuery.Select.Columns.Count > 1)
								{
									if (!processMultiColumn || ProviderOuterCanHandleSeveralColumnsQuery(joinQuery))
									{
										// provider can handle this query
										continue;
									}
								}

								if (!IsLimitedToOneRecord(sq, joinQuery, evaluationContext))
									continue;

								if (!SqlProviderHelper.IsValidQuery(joinQuery, parentQuery: sq, fakeJoin: null, forColumn: true, _providerFlags, out _))
									continue;

								if (_providerFlags.DoesNotSupportCorrelatedSubquery)
								{
									if (QueryHelper.IsDependsOnOuterSources(join))
										continue;
								}

								var isValid = true;

								foreach (var testedColumn in joinQuery.Select.Columns.content)
								{
									// where we can start analyzing that we can move join to subquery
									
									if (!IsUniqueUsage(sq, testedColumn))
									{
										if (_providerFlags.IsApplyJoinSupported)
										{
											MoveDuplicateUsageToSubQuery(sq);
											// will be processed in the next step
											ti = -1;
											isValid = false;
											break;
										}	
									}

									if (testedColumn.Expression is FunctionWord function)
									{
										if (function.IsAggregate)
										{
											if (!_providerFlags.AcceptsOuterExpressionInAggregate && IsInsideAggregate(sq.Select, testedColumn))
											{
												if (_providerFlags.IsApplyJoinSupported)
												{
													// Well, provider can process this query as OUTER APPLY
													isValid = false;
													break;
												}

												MoveDuplicateUsageToSubQuery(sq);
												// will be processed in the next step
												ti      = -1;
												isValid = false;
												break;
											}

											if (!_providerFlags.IsCountSubQuerySupported)
											{
												isValid = false;
												break;
											}
										}
									}
								}

								if (!isValid)
									continue;

								// moving whole join to subquery

								table.GetJoins().RemoveAt(j);
								joinQuery.Where.ConcatSearchCondition(join.Condition);

								// replacing column with subquery

								for (var index = joinQuery.Select.Columns.Count - 1; index >= 0; index--)
								{
									var queryToReplace = joinQuery;
									var testedColumn   = joinQuery.Select.Columns[index];

									// cloning if there are many columns
									if (index > 0)
									{
										queryToReplace = joinQuery.Clone();
									}

									if (queryToReplace.Select.Columns.Count > 1)
									{
										var sourceColumn = queryToReplace.Select.Columns[index];
										queryToReplace.Select.Columns.Clear();
										queryToReplace.Select.Columns.Add(sourceColumn);
									}

									NotifyReplaced(queryToReplace, testedColumn);
								}
							}
						}
					}
				}
			}

			if (_version != currentVersion)
			{
				EnsureReferencesCorrected(selectQuery);
				return true;
			}

			return false;
		}

		public override Clause VisitCteClause(CTEClause element)
		{
			var saveIsInRecursiveCte = _isInRecursiveCte;
			if (element.IsRecursive)
				_isInRecursiveCte = true;

			var saveParent = _parentSelect;
			_parentSelect = null;
			
			var newElement = base.VisitCteClause(element);

			_parentSelect = saveParent;

			_isInRecursiveCte = saveIsInRecursiveCte;

			return newElement;
		}

		public override Clause VisitAffirmFuncLike(FuncLike element)
		{
			var result = base.VisitAffirmFuncLike(element);

			if (!ReferenceEquals(result, element))
				return Visit(element);
            // Parameters: [SelectQuery sq]
            if (element.Function is { Name: "EXISTS", Parameters.Length:1 } && element.Function.Parameters[0] is SelectQueryClause sq)
			{
				// We can safely optimize out Distinct
				if (sq.Select.IsDistinct)
				{
					sq.Select.IsDistinct = false;
				}

				if (sq.GroupBy.IsEmpty && !sq.HasSetOperators)
				{
					// non aggregation columns can be removed
					for (int i = sq.Select.Columns.Count - 1; i >= 0; i--)
					{
						var colum = sq.Select.Columns[i];
						if (!QueryHelper.ContainsAggregationFunction(colum.Expression))
						{
							sq.Select.Columns.content.RemoveAt(i);
						}
					}
				}
			}

			return element;
		}

		#region Helpers

		class MovingComplexityVisitor : ClauseVisitor
		{
			IExpWord                _expressionToCheck = default!;
			ISQLNode?[]              _ignore            = default!;
			NullabilityContext            _nullability       = default!;
			EvaluateContext             _evaluationContext = default!;
			SqlExpressionOptimizerVisitor _optimizerVisitor  = default!;
			bool                          _isInsideNot;
			int                           _foundCount;
			bool                          _notAllowedScope;
			bool                          _doNotAllow;

			public bool DoNotAllow
			{
				get => _doNotAllow;
				private set => _doNotAllow = value;
			}

			public VisitMode VisitingMode;

            public MovingComplexityVisitor()
			{
				VisitingMode = VisitMode.ReadOnly;

            }

			public void Cleanup()
			{
				_ignore            = default!;
				_expressionToCheck = default!;
				_doNotAllow        = default;
				_nullability       = default!;
				_evaluationContext = default!;
				_optimizerVisitor  = default!;


				_foundCount = 0;
				_isInsideNot       = default;
			}

			public bool IsAllowedToMove(IExpWord testExpression, Clause parent, NullabilityContext nullability, SqlExpressionOptimizerVisitor optimizerVisitor, 
				EvaluateContext evaluationContext, params Clause?[] ignore)
			{
				_ignore            = ignore;
				_expressionToCheck = testExpression;
				_nullability       = nullability;
				_evaluationContext = evaluationContext;
				_optimizerVisitor  = optimizerVisitor;
				_doNotAllow        = default;
				_foundCount        = 0;
				_isInsideNot       = default;

				Visit(parent);

				return !DoNotAllow;
			}

			public override Clause? Visit(Clause? element)
			{
				if (element == null)
					return null;

				if (DoNotAllow)
					return element;

				if (_ignore.Contains(element, Utils.ObjectReferenceEqualityComparer<ISQLNode?>.Default))
					return element;

				if (ReferenceEquals(element, _expressionToCheck))
				{
					if (_notAllowedScope)
					{
						DoNotAllow = true;
						return element;
					}

					++_foundCount;

					if (_foundCount > 1)
						DoNotAllow = true;

					return element;
				}

				return base.Visit(element);
			}

			public override Clause VisitAffirmExprExpr(ExprExpr predicate)
			{
				ISQLNode reduced = predicate.Reduce(_nullability, _evaluationContext, _isInsideNot);
				if (!ReferenceEquals(reduced, predicate))
				{
					reduced = _optimizerVisitor.Optimize(_evaluationContext, _nullability, null, reduced as Clause, false, _isInsideNot, true);

					Visit(reduced as Clause);
				}
				else
					base.VisitAffirmExprExpr(predicate);

				return predicate;
			}

            public override Clause VisitOrderByItem(OrderByWord element)
			{
				if (element.IsPositioned)
				{
					// do not count complexity for positioned order item
					if (ReferenceEquals(element.Expression, _expressionToCheck))
						return element;
				}

				return base.VisitOrderByItem(element);
			}

            public override Clause VisitSqlQuery(SelectQueryClause selectQuery)
			{
				var saveIsInsideNot = _isInsideNot;
				_isInsideNot = false;
				var newElement =  base.VisitSqlQuery(selectQuery);
				_isInsideNot = saveIsInsideNot;
				return newElement;
			}

            public override Clause VisitAffirmNot(Not predicate)
			{
				var saveValue = _isInsideNot;
				_isInsideNot = true;

				var result = base.VisitAffirmNot(predicate);

				_isInsideNot = saveValue;

				return result;
			}

            public override Clause VisitAffirmInList(InList predicate)
			{
				using var scope = DoNotAllowScope(predicate.Expr1.NodeType == ClauseType.SqlObjectExpression);
				return base.VisitAffirmInList(predicate);
			}

			readonly struct DoNotAllowScopeStruct : IDisposable
			{
				readonly MovingComplexityVisitor _visitor;
				readonly bool                    _saveValue;

				public DoNotAllowScopeStruct(MovingComplexityVisitor visitor, bool? doNotAllow)
				{
					_visitor   = visitor;
					_saveValue = visitor._notAllowedScope;
					if (doNotAllow != null)
						visitor._notAllowedScope = doNotAllow.Value;
				}

				public void Dispose()
				{
					_visitor._notAllowedScope = _saveValue;
				}
			}

			DoNotAllowScopeStruct DoNotAllowScope(bool? doNotAllow)
			{
				return new DoNotAllowScopeStruct(this, doNotAllow);
			}
		}

		class MovingOuterPredicateVisitor : ClauseVisitor
		{
			SelectQueryClause                          _forQuery       = default!;
			IAffirmWord                        _predicate      = default!;
			IReadOnlyCollection<ITableNode> _currentSources = default!;
            public VisitMode VisitingMode;
            public MovingOuterPredicateVisitor() 
			{
                VisitingMode = VisitMode.Transform;
            }

			public IAffirmWord CorrectReferences(SelectQueryClause forQuery, IReadOnlyCollection<ITableNode> currentSources, IAffirmWord predicate)
			{
				_forQuery       = forQuery;
				_predicate      = predicate;
				_currentSources = currentSources;

				return (IAffirmWord)VisitAffirmWord(predicate);
			}

			public void Cleanup()
			{
				_forQuery       = default!;
				_predicate      = default!;
				_currentSources = default!;
			}

			[return: NotNullIfNotNull(nameof(element))]
			public override Clause? Visit(Clause? element)
			{
				if (ReferenceEquals(element, _predicate))
					return base.Visit(element);

				if (element is IExpWord sqlExpr)
				{
                    var para = new List<ITableNode>();
                    foreach (var i in _currentSources)
                    {
                        para.Add(i);
                    }
                    if (QueryHelper.IsDependsOnSources(sqlExpr, _currentSources) && !QueryHelper.IsDependsOnOuterSources(sqlExpr, currentSources : para))
					{
						if (sqlExpr is ColumnWord column && column.Parent == _forQuery)
							return sqlExpr as Clause;

						var withoutNullabilityCheck = sqlExpr;

						var nullabilityExpression = sqlExpr as NullabilityWord;
						if (nullabilityExpression != null)
							withoutNullabilityCheck = nullabilityExpression.SqlExpression;

						var newExpr = (IExpWord)_forQuery.Select.AddColumn(withoutNullabilityCheck);

						if (nullabilityExpression != null)
							newExpr = NullabilityWord.ApplyNullability(newExpr, nullabilityExpression.CanBeNull);

						return newExpr as Clause;
					}

					return element;
				}

				return base.Visit(element);
			}
		}

		#endregion

	}
}
