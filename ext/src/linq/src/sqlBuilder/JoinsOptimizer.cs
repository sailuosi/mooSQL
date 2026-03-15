using System;
using System.Collections.Generic;
using System.Linq;

namespace mooSQL.linq.SqlProvider
{
	using mooSQL.data.model;

	using SqlQuery;

	sealed partial class JoinsOptimizer
	{
		Dictionary<int, IExpWord[][]?>?                                                    _tableKeysCache;
		Dictionary<(TableSourceWord? left,TableSourceWord right),IReadOnlyList<EqualityFields>?>? _equalityPairsCache;

		public BaseSentence Optimize(BaseSentence statement, EvaluateContext evaluationContext)
		{
			RemoveUnusedLeftJoins(statement, evaluationContext);

			statement = RemoveDuplicateJoins(statement);

			return statement;
		}

		/// <summary>
		/// Removes left joins if they doesn't change query cardinality and joined tables not used in query.
		/// </summary>
		void RemoveUnusedLeftJoins(BaseSentence statement, EvaluateContext evaluationContext)
		{
			statement.Visit((this_: this, statement, evaluationContext), static (ctx, e) =>
			{
				if (e is TableSourceWord source)
				{
					// detect unused LEFT joins that doesn't change query cardinality and remove them
					for (var index = source.Joins.Count - 1; index >= 0; index--)
					{
						if (ctx.this_.CanRemoveLeftJoin(ctx.statement, source.Joins[index], ctx.evaluationContext))
							source.Joins.RemoveAt(index);
					}
				}
			});
		}

		/// <summary>
		/// Moves nested joins to upper level when outer join type is compatible with first nested join type.
		/// </summary>
		public static void UnnestJoins(Clause statement)
		{
			statement.Visit(static e =>
			{
				if (e is TableSourceWord source)
				{
					for (var i = 0; i < source.Joins.Count; i++)
					{
						var insertIndex = i + 1;
						var parent      = source.Joins[i];

						// INNER/LEFT join with nested joins
						var parentJoins = parent.Table.GetJoins();

                        if (parentJoins.Count > 0 && parent.JoinType is JoinKind.Inner or JoinKind.Left)
						{
							var child = parentJoins[0];

							// check compatibility of outer join with first nested join:
							// INNER + INNER/LEFT/CROSS APPLY/OUTER APPLY
							// LEFT + LEFT
							if ((parent.JoinType == JoinKind.Inner && (child.JoinType is JoinKind.Inner or JoinKind.Left or JoinKind.CrossApply or JoinKind.OuterApply)) ||
								(parent.JoinType == JoinKind.Left && child.JoinType == JoinKind.Left))
							{
								// check that join condition doesn't reference child tables
								var sources = new HashSet<int>(parentJoins.SelectMany(j => j.Table.FindTables().Select(t => t.SourceID)));
								var found = parent.Condition.Find(sources, static (sources, e) =>
								{
									if (e is IExpWord expr
										&& GetUnderlyingFieldOrColumn(expr) is IExpWord field
										&& sources.Contains(GetFieldSourceID(field)))
									{
										return true;
									}

									return false;
								});

								if (found != null)
									continue;

								// move all nested joins up
								source.Joins.InsertRange(insertIndex, parentJoins);
                                parentJoins.Clear();
							}
						}
					}
				}
			});
		}

		public static void UndoNestedJoins(Clause statement)
		{
			var correct = false;
			statement.Visit(e =>
			{
				if (e is TableSourceWord source)
				{
					for (var i = 0; i < source.Joins.Count; i++)
					{
						var join = source.Joins[i];

						if (join.Table.GetJoins().Count > 0)
						{
							var subQueryTableSource = new TableSourceWord(
								join.Table.FindISrc(),
								join.Table.FindAlias(),
								join.Table.GetJoins(),
								join.Table.HasUniqueKeys() ? join.Table.FindUniqueKeys() : null);

							var subQuery = new SelectQueryClause();
							subQuery.From.Tables.Add(subQueryTableSource);

							//join.Table.Source = subQuery;
							join.Table.setAlias( null);
							join.Table.GetJoins().Clear();
							if (join.Table.HasUniqueKeys())
								join.Table.FindUniqueKeys().Clear();

							correct = true;
						}
					}
				}
			});

			if (correct)
			{
				var corrector = new SqlQueryColumnNestingCorrector();
				corrector.CorrectColumnNesting(statement);
			}
		}

		#region Helpers
		bool CanRemoveLeftJoin(BaseSentence statement, JoinTableWord join, EvaluateContext evaluationContext)
		{
			// left joins only
			if (join.JoinType is not JoinKind.Left)
				return false;

			// has nested joins
			if (join.Table.GetJoins().Count > 0)
				return false;

			// we cannot make assumptions on non-standard joins
			if (join.SqlQueryExtensions?.Count > 0)
				return false;

			// some table extensions also could affect cardinality
			// https://github.com/linq2db/linq2db/pull/4016
			if (join.Table.FindISrc() is TableWord { SqlQueryExtensions.Count: > 0 })
				return false;

			// check wether join used outside join itself
			if (null != statement.FindExcept(join.Table.SourceID, join, static (object sourceID, ISQLNode e) =>
				(e is FieldWord field && field.Table?.FindSrc().SourceID.ToString() == sourceID.ToString()) ||
				(e is ColumnWord column && column.Parent?.SourceID.ToString() == sourceID.ToString())))
				return false;

			if (!IsLeftJoinCardinalityPreserved(join, evaluationContext))
				return false;

			return true;
		}

		bool IsLeftJoinCardinalityPreserved(JoinTableWord join, EvaluateContext evaluationContext)
		{
			// Check that join doesn't change rowcount and has 1-0/1 cardinality:
			// - 1-0 cardinality: condition is false constant
			// - 1-1 cardinality: join made by unique key fields (optionally could have extra AND filters)

			// TODO: this currently doesn't work for cases where nullability makes condition false (e.g. "non_nullable_field == null")
			if (join.Condition.TryEvaluateExpression(evaluationContext, out var value) && value is false)
				return true;

			// get fields, used in join condition
			var found = SearchForJoinEqualityFields(null, join);

			// not joined by left table fields
			if (found == null)
				return false;

			// collect unique keys for table
			var uniqueKeys = GetTableKeys(join.Table as TableSourceWord);
			if (uniqueKeys == null)
				return false;

			var foundFields = new HashSet<IExpWord>(found.Select(f => f.RightField));

			// check if any of keysets used for join
			for (var i = 0; i < uniqueKeys.Length; i++)
				if (uniqueKeys[i].All(foundFields.Contains))
					return true;

			var unwrapped = new HashSet<IExpWord>(foundFields.Select(f => f is ColumnWord c ? c.Expression : f));

			for (var i = 0; i < uniqueKeys.Length; i++)
				if (uniqueKeys[i].All(unwrapped.Contains))
					return true;

			return false;
		}

		sealed class EqualityFields {
			public EqualityFields(IAffirmWord Condition, IExpWord? LeftField, IExpWord RightField) { 
				this.Condition = Condition;
				this.LeftField = LeftField;
				this.RightField = RightField;
			}
			public IAffirmWord Condition;
            public IExpWord? LeftField;
			public IExpWord RightField;
        }

		/// <summary>
		/// Inspect join condition and return list of field pairs used in equals conditions between <paramref name="leftSource"/> (when specified) and <paramref name="rightJoin"/> tables.
		/// If condition contains top-level OR operator, method returns <c>null</c>.
		/// </summary>
		IReadOnlyList<EqualityFields>? SearchForJoinEqualityFields(TableSourceWord? leftSource, JoinTableWord rightJoin)
		{
			var key = (leftSource, rightJoin.Table);

			if (_equalityPairsCache?.TryGetValue(((TableSourceWord? left, TableSourceWord right))key, out var found) != true)
			{
				List<EqualityFields>? pairs = null;

				if (!rightJoin.Condition.IsOr)
				{
					for (var i1 = 0; i1 < rightJoin.Condition.Predicates.Count; i1++)
					{
						var p = rightJoin.Condition.Predicates[i1];

						// ignore all predicates except "x == y"
						if (p is not mooSQL.data.model.affirms.ExprExpr exprExpr || exprExpr.Operator != AffirmWord.Operator.Equal)
							continue;

						// try to extract joined tables fields from predicate
						var field1 = GetUnderlyingFieldOrColumn(exprExpr.Expr1);
						var field2 = GetUnderlyingFieldOrColumn(exprExpr.Expr2);

                        IExpWord? leftField  = null;
                        IExpWord? rightField = null;

						if (field1 != null)
							DetectField(leftSource, rightJoin.Table as TableSourceWord, GetNewField(field1), ref leftField, ref rightField);

						if (field2 != null)
							DetectField(leftSource, rightJoin.Table as TableSourceWord, GetNewField(field2), ref leftField, ref rightField);

						if (rightField != null && (leftSource == null || leftField != null))
							(pairs ??= new()).Add(new(p, leftField, rightField));
					}
				}

				(_equalityPairsCache ??= new()).Add(((TableSourceWord? left, TableSourceWord right))key, found = pairs);
			}
			
			return found;

			void DetectField(TableSourceWord? leftSource, TableSourceWord rightSource, IExpWord field, ref IExpWord? leftField, ref IExpWord? rightField)
			{
				var sourceID = GetFieldSourceID(field);

				if (rightSource.Source.SourceID == sourceID)
					rightField = field;
				else if (rightSource.Source is SelectQueryClause select && select.Select.From.Tables.Count == 1 && select.Select.From.Tables[0].SourceID == sourceID)
					throw new InvalidOperationException("DetectField:Debug");
					//rightField = field;
				else if (leftSource?.Source.SourceID == sourceID)
					leftField = field;
				else if (leftSource != null)
					leftField = MapToSource(leftSource, field, leftSource.Source.SourceID, rightSource.SourceID);
			}
		}

		static int GetFieldSourceID(IExpWord field)
		{
			return field switch
			{
                FieldWord  sqlField  => sqlField .Table? .SourceID,
                ColumnWord sqlColumn => sqlColumn.Parent?.SourceID,
				_ => null
			} ?? -1;
		}

        /// <summary>
        /// Returns table unique keysets or null of no unique keys found.
        /// </summary>
        IExpWord[][]? GetTableKeys(TableSourceWord tableSource)
		{
			if (_tableKeysCache?.TryGetValue(tableSource.SourceID, out var keys) != true)
				(_tableKeysCache ??= new()).Add(tableSource.SourceID, keys = GetTableKeysInternal(tableSource));

			return keys;

			static IExpWord[][]? GetTableKeysInternal(TableSourceWord tableSource)
			{
				var knownKeys = new List<IList<IExpWord>>();

                QueryHelper.CollectUniqueKeys(tableSource, knownKeys);

				if (knownKeys.Count == 0)
					return null;

				// unwrap keyset expressions as field/column
				var result = new IExpWord[knownKeys.Count][];

				for (var i = 0; i < knownKeys.Count; i++)
				{
					var keyset = knownKeys[i];
					var fields = result[i] = new IExpWord[keyset.Count];

					for (var j = 0; j < keyset.Count; j++)
						fields[j] = GetUnderlyingFieldOrColumn(keyset[j]) ?? throw new InvalidOperationException($"Cannot get field for {keyset[j]}");
				}

				return result;
			}
		}

		/// <summary>
		/// Reduce <paramref name="expr"/> to <see cref="FieldWord"/> or <see cref="ColumnWord"/> if possible.
		/// </summary>
		static IExpWord? GetUnderlyingFieldOrColumn(IExpWord expr)
		{
			switch (expr.NodeType)
			{
				case ClauseType.SqlExpression:
				{
					var sqlExpr = (ExpressionWord)expr;
					if (sqlExpr.Expr == "{0}" && sqlExpr.Parameters.Length == 1)
						return GetUnderlyingFieldOrColumn(sqlExpr.Parameters[0]);
					return null;
				}

				case ClauseType.SqlNullabilityExpression:
					return GetUnderlyingFieldOrColumn(((NullabilityWord)expr).SqlExpression);

				case ClauseType.SqlField:
				case ClauseType.Column:
					return expr;
			}

			return null;
		}
		#endregion
	}
}
