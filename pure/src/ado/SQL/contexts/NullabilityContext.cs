using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;



namespace mooSQL.data.model
{
	/// <summary>
	/// 在当前查询或子查询的上下文中，提供可空性的信息。基于表达式或outer join 的可空注解 
	/// </summary>
	public sealed class NullabilityContext
	{
		/// <summary>
		/// Context for non-select queries of places where we don't know select query.
		/// </summary>
		public static NullabilityContext NonQuery { get; } = new(null, null);

		/// <summary>
		/// Creates nullability context for provided query or empty context if query is <c>null</c>.
		/// </summary>
		public static NullabilityContext GetContext(SelectQueryClause? selectQuery) =>
			selectQuery == null ? NonQuery : new NullabilityContext(selectQuery, null);

		/// <summary>
		/// Creates nullability context for provided query.
		/// </summary>
		public NullabilityContext(SelectQueryClause inQuery) : this(inQuery, null)
		{
		}

		NullabilityContext(SelectQueryClause? inQuery, NullabilityCache? nullabilityCache)
		{
			InQuery           = inQuery;
			_nullabilityCache = nullabilityCache;
		}

		/// <summary>
		/// Current context query.
		/// </summary>
		public SelectQueryClause?     InQuery     { get; }

		//[MemberNotNullWhen(false, nameof(InQuery))]
		public bool             IsEmpty     => InQuery == null;

		NullabilityCache? _nullabilityCache;

		bool? CanBeNullInternal(SelectQueryClause? query, ITableNode source)
		{
			// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
			if (query == null)
			{
				return null;
			}

			_nullabilityCache ??= new();
			return _nullabilityCache.IsNullableSource(query, source);
		}

		public void RegisterReplacement(SelectQueryClause oldQuery, SelectQueryClause newQuery)
		{
			_nullabilityCache ??= new();

			_nullabilityCache.RegisterReplacement(oldQuery, newQuery);
		}

		/// <summary>
		/// Returns wether expression could contain null values or not.
		/// </summary>
		public bool CanBeNull(IExpWord expression)
		{
			if (expression is ColumnWord column)
			{
				// if column comes from nullable subquery - column is always nullable
				if (column.Parent != null && CanBeNullInternal(InQuery, column.Parent) == true)
					return true;

				// otherwise check column expression nullability
				return CanBeNull(column.Expression as IExpWord);
			}

			if (expression is FieldWord field)
			{
				// column is nullable itself or otherwise check if column source nullable
				return field.CanBeNull
					|| (field.Table != null && (CanBeNullInternal(InQuery, field.Table) ?? false));
			}

			// explicit nullability specification
			if (expression is NullabilityWord nullability)
			{
				return nullability.CanBeNull;
			}

			// allow expression to calculate it's nullability
			//return expression.CanBeNullable(this);
			return true;
		}

		/// <summary>
		/// Collect and cache information about nullablity of each table source in specific <see cref="SelectQueryClause"/>.
		/// </summary>
		sealed class NullabilityCache
		{
			[DebuggerDisplay("Q[{InQuery.SourceID}] -> TS[{Source.SourceID}]")]
			record struct NullabilityKey(SelectQueryClause InQuery, ITableNode Source);

			Dictionary<NullabilityKey, bool>?     _nullableSources;
			HashSet<SelectQueryClause>?                 _processedQueries;
			Dictionary<SelectQueryClause, SelectQueryClause>? _replacements;

			/// <summary>
			/// Returns nullability status of <paramref name="source"/> in specific <paramref name="inQuery"/>.
			/// </summary>
			/// <returns>
			/// <list type="bullet">
			/// <item><c>true</c>: <paramref name="source"/> records are nullable in <paramref name="inQuery"/>;</item>
			/// <item><c>false</c>: <paramref name="source"/> records are not nullable in <paramref name="inQuery"/>;</item>
			/// <item><c>null</c>: <paramref name="source"/> is not reachable/available in <paramref name="inQuery"/>.</item>
			/// </list>
			/// </returns>
			public bool? IsNullableSource(SelectQueryClause inQuery, ITableNode source)
			{
				EnsureInitialized(inQuery);

				if (_nullableSources!.TryGetValue(new(inQuery, source), out var isNullable))
				{
					return isNullable;
				}

				if (_replacements != null && source is SelectQueryClause sourceQuery)
				{
					var oldSource  = GetReplacement(sourceQuery) ?? source;
					var oldInQuery = GetReplacement(inQuery) ?? inQuery;

					if (!ReferenceEquals(oldSource, source) || !ReferenceEquals(oldInQuery, inQuery))
					{
						if (_nullableSources!.TryGetValue(new(oldInQuery, oldSource), out isNullable))
						{
							return isNullable;
						}
					}
				}

				return null;
			}

			void EnsureInitialized(SelectQueryClause inQuery)
			{
				_nullableSources  ??= new();
				_processedQueries ??= new HashSet<SelectQueryClause>();

				ProcessQuery(new Stack<SelectQueryClause>(), inQuery);
			}

			public void RegisterReplacement(SelectQueryClause oldQuery, SelectQueryClause newQuery)
			{
				_replacements           ??= new();

				_replacements[newQuery] = oldQuery;
			}

			public SelectQueryClause? GetReplacement(SelectQueryClause newQuery)
			{
				if (_replacements == null)
					return null;

				if (!_replacements.TryGetValue(newQuery, out var oldQuery)) 
					return null;

				while (true)
				{
					if (!_replacements.TryGetValue(oldQuery, out var foundOldQuery))
						break;

					oldQuery = foundOldQuery;
				}

				return oldQuery;
			}

			/// <summary>
			/// Goes from top to down into query and register nullability of each joined table source in current and upper queries.
			/// </summary>
			/// <param name="current">Parent queries stack.</param>
			/// <param name="selectQuery">Current query for which we inspect it's joins.</param>
			void ProcessQuery(Stack<SelectQueryClause> current, SelectQueryClause selectQuery)
			{
				void Register(ITableNode source, bool canBeNullTable)
				{
					foreach (var query in current)
					{
						_nullableSources![new (query, source)] = canBeNullTable;
					}
				}

				// cache hit
				if (!_processedQueries!.Add(selectQuery))
					return;

				current.Push(selectQuery);

				foreach (var table in selectQuery.From.Tables)
				{
					if (table is TableSourceWord srcTable) { 
						if (srcTable.Source is SelectQueryClause sc)
						{
							ProcessQuery(current, sc);
						}

						var canBeNullTable = srcTable.Joins.Any(static join =>
							join.JoinType == JoinKind.Right || join.JoinType == JoinKind.RightApply ||
							join.JoinType == JoinKind.Full  || join.JoinType == JoinKind.FullApply);

						// register nullability of right side of join
						Register(srcTable.Source, canBeNullTable);

						foreach (var join in srcTable.Joins)
						{
							var canBeNullJoin = join.JoinType == JoinKind.Full || join.JoinType == JoinKind.FullApply ||
												join.JoinType == JoinKind.Left ||
												join.JoinType == JoinKind.OuterApply;

							// register nullability of left right side of join
							if (join.Table is TableSourceWord joinSrc) { 
								Register(joinSrc.Source, canBeNullJoin);

								if (joinSrc.Source is SelectQueryClause jc)
								{
									ProcessQuery(current, jc);
								}							
							}

						}					
					}

				}

				_ = current.Pop();
			}
		}
	}
}
