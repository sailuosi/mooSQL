using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace mooSQL.linq.Linq.Builder
{
	using Async;
	using Common;
	using Extensions;
	using mooSQL.linq.Expressions;
	using Mapping;
	using Reflection;
	using SqlQuery;
	using mooSQL.data.model;
	using mooSQL.utils;
	using mooSQL.data;

	internal class GroupByContext : SubQueryContext
	{
		public GroupByContext(
			IClauseContext                  sequence,
			Expression                     sequenceExpr,
			Type                           groupingType,
			KeyContext                     key,
			ContextRefExpression           keyRef,
			List<SqlPlaceholderExpression> currentPlaceholders,
			ElementContext                 element,
			bool                           isGroupingGuardDisabled,
			bool                           addToSql)
			: this(sequence, new SelectQueryClause(), sequenceExpr, groupingType, key, keyRef, currentPlaceholders, element, isGroupingGuardDisabled, addToSql)
		{
		}

		public GroupByContext(
			IClauseContext                  sequence,
			SelectQueryClause              selectQuery,
			Expression                     sequenceExpr,
			Type                           groupingType,
			KeyContext                     key,
			ContextRefExpression           keyRef,
			List<SqlPlaceholderExpression> currentPlaceholders,
			ElementContext                 element,
			bool                           isGroupingGuardDisabled,
			bool                           addToSql)
			: base(sequence, selectQuery, addToSql)
		{
			_sequenceExpr       = sequenceExpr;
			_key                = key;
			_keyRef             = keyRef;
			CurrentPlaceholders = currentPlaceholders;
			Element             = element;
			_groupingType       = groupingType;

			IsGroupingGuardDisabled = isGroupingGuardDisabled;

			key.GroupByContext = this;
			key.Parent         = this;
		}

		readonly Expression                     _sequenceExpr;
		readonly KeyContext                     _key;
		readonly ContextRefExpression           _keyRef;
		public   List<SqlPlaceholderExpression> CurrentPlaceholders { get; }
		readonly Type                           _groupingType;
		public   bool                           IsGroupingGuardDisabled { get; }

		public ElementContext Element { get; }

		public override Type ElementType => _groupingType;

		internal static void AppendGroupBy(ClauseSqlTranslator builder, List<SqlPlaceholderExpression> currentPlaceholders, SelectQueryClause query, Expression groupByExpression)
		{
			var placeholders = ClauseSqlTranslator.CollectDistinctPlaceholders(groupByExpression);

			// it is a case whe we do not group elements
			if (placeholders.Count == 1 && QueryHelper.IsConstantFast(placeholders[0].Sql))
			{
				return;
			}

			foreach (var p in placeholders)
			{
				if (currentPlaceholders.Find(cp => ExpressionEqualityComparer.Instance.Equals(cp.Path, p.Path)) == null)
				{
					currentPlaceholders.Add(p);

					var updated = builder.UpdateNesting(query, p);
					query.GroupBy.Items.Add(updated.Sql);
				}
			}
		}

		public override Expression BuildProjection(Expression path, ProjectFlags flags)
		{
			var isSameContext = SequenceHelper.IsSameContext(path, this);

			if (isSameContext)
			{
				if (flags.IsExtractProjection())
				{
					if (path.Type == ElementType)
						return MakeSubQueryExpression(path);
					return path;
				}
			}

			if (isSameContext && (flags.IsRoot() || flags.IsTraverse()))
			{
				return path;
			}

			if (flags.IsAggregationRoot())
			{
				return path;
			}

			if (isSameContext && flags.IsKeys() && GetInterfaceGroupingType().IsSameOrParentOf(path.Type))
			{
				var result = Builder.BuildProjection(this, _keyRef, flags);
				return result;
			}

			if (isSameContext && flags.IsExpression()/* && GetInterfaceGroupingType().IsSameOrParentOf(path.Type)*/)
			{
				if (!IsGroupingGuardDisabled)
				{
					var ex = new SooQueryException(
						
						"You should explicitly specify selected fields for server-side GroupBy() call or add AsEnumerable() call before GroupBy() to perform client-side grouping.Set ExtLinqOptions.Linq.GuardGrouping = false to disable this check.Additionally this guard exception can be disabled by extension GroupBy(...).DisableGuard().NOTE! By disabling this guard you accept Eager Loading for grouping query."
					)
					{
						HelpLink = "https://github.com/mooSQL/mooSQL/issues/365"
					};

					throw ex;
				}

				var groupingType = GetGroupingType();

				var groupingPath = ((ContextRefExpression)path).WithType(groupingType);

				var assignments = new List<SqlGenericConstructorExpression.Assignment>(2);

				assignments.Add(new SqlGenericConstructorExpression.Assignment(
					groupingType.GetProperty(nameof(Grouping<int, int>.Key))!,
					Expression.Property(groupingPath, nameof(IGrouping<int, int>.Key)), true, false));

				var eagerLoadingExpression = MakeSubQueryExpression(new ContextRefExpression(groupingType, this));

				assignments.Add(new SqlGenericConstructorExpression.Assignment(
					groupingType.GetProperty(nameof(Grouping<int, int>.Items))!,
					eagerLoadingExpression, true, false));

				return new SqlGenericConstructorExpression(
					SqlGenericConstructorExpression.CreateType.Auto,
					groupingType, null, assignments.AsReadOnly(), DB, path);
			}

			if (path is MemberExpression me)
			{
				var currentMemberExpr = me;
				var found             = false;
				while (true)
				{
					if (currentMemberExpr.Expression is ContextRefExpression && currentMemberExpr.Member.Name == "Key")
					{
						found = true;
						break;
					}

					if (currentMemberExpr.Expression is MemberExpression memberExpr)
					{
						currentMemberExpr = memberExpr;
					}
					else
						break;
				}

				if (found)
				{
					var keyRef  = new ContextRefExpression(currentMemberExpr.Type, _key);
					var keyPath = me.Replace(currentMemberExpr, keyRef);

					if (flags.HasFlag(ProjectFlags.Root) || flags.HasFlag(ProjectFlags.AssociationRoot))
					{
						return new ContextRefExpression(path.Type, new ScopeContext(_key, this));
					}

					var result = Builder.BuildProjection(_key, keyPath, flags);

					return result;
				}
			}

			if (!isSameContext || !flags.IsSql())
			{
				var root = Builder.GetRootContext(this, path, true);
				if (root != null && typeof(IGrouping<,>).IsSameOrParentOf(root.Type))
				{
					return path;
				}
			}

			if (isSameContext && flags.IsSql() && !flags.IsKeys() && path.Type != Element.ElementType)
			{
				return path;
			}

			var newPath = SequenceHelper.CorrectExpression(path, this, Element);

			return newPath;
		}

		public Type GetGroupingType()
		{
			var groupingType = typeof(Grouping<,>).MakeGenericType(
				_key.Body.Type, Element.Body.Type);
			return groupingType;
		}

		public Type GetInterfaceGroupingType()
		{
			var groupingType = typeof(IGrouping<,>).MakeGenericType(
				_key.Body.Type, Element.Body.Type);
			return groupingType;
		}

		public override IClauseContext Clone(CloningContext context)
		{
			var clone = new GroupByContext(context.CloneContext(SubQuery), context.CloneElement(SelectQuery), context.CloneExpression(_sequenceExpr), _groupingType,
				context.CloneContext(_key), context.CloneExpression(_keyRef),
				CurrentPlaceholders.Select(p => context.CloneExpression(p)).ToList(), context.CloneContext(Element),
				IsGroupingGuardDisabled, false);

			return clone;
		}

		static Expression MakeSubQueryExpression(DBInstance mappingSchema, Expression sequence,
			ParameterExpression                                param,         Expression expr1, Expression expr2)
		{
			var filterLambda = Expression.Lambda(ClauseSqlTranslator.Equal(mappingSchema, expr1, expr2), param);
			return TypeHelper.MakeMethodCall(Methods.Enumerable.Where, sequence, filterLambda);
		}

		public Expression MakeSubQueryExpression(Expression buildExpression)
		{
			var expr = MakeSubQueryExpression(
				DB,
				_sequenceExpr,
				_key.Lambda.Parameters[0],
				ExpressionHelper.PropertyOrField(buildExpression, "Key"),
				_key.Lambda.Body);

			// do not repeat simple projection
			if (Element.Body != Element.Lambda.Parameters[0])
			{
				expr = TypeHelper.MakeMethodCall(Methods.Enumerable.Select, expr, Element.Lambda);
			}

			return expr;
		}

		public override IClauseContext? GetContext(Expression expression, BuildInfo buildInfo)
		{
			if (!buildInfo.IsSubQuery)
				return this;

			if (buildInfo.IsAggregation && !buildInfo.CreateSubQuery)
				return this;

			if (!SequenceHelper.IsSameContext(expression, this))
				return null;

			var expr = MakeSubQueryExpression(((ContextRefExpression)buildInfo.Expression).WithType(GetInterfaceGroupingType()));

			var parentContext = buildInfo.Parent ?? this;

			expr = Builder.UpdateNesting(parentContext, expr);

			var buildResult = Builder.TryBuildSequence(new BuildInfo(buildInfo, expr) { IsAggregation = false, CreateSubQuery = false});

			return buildResult.BuildContext;
		}

		internal class Grouping<TKey,TElement> : IGrouping<TKey,TElement>
		{
			public TKey                   Key   { get; set; } = default!;
			public IEnumerable<TElement>? Items { get; set; } = default!;

			public IEnumerator<TElement> GetEnumerator()
			{
				if (Items == null)
					return Enumerable.Empty<TElement>().GetEnumerator();

				return Items.GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		class GroupingEnumerable<TKey, TElement> : IResultEnumerable<IGrouping<TKey, TElement>>
		{
			readonly IResultEnumerable<TElement> _elements;
			readonly Func<TElement, TKey>        _groupingKey;

			public GroupingEnumerable(IResultEnumerable<TElement> elements, Func<TElement, TKey> groupingKey)
			{
				_elements    = elements;
				_groupingKey = groupingKey;
			}

			public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
			{
				return _elements.GroupBy(_groupingKey).GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
#if NET5_0_OR_GREATER
			public IAsyncEnumerator<IGrouping<TKey, TElement>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
			{
				return new GroupingAsyncEnumerator(_elements, _groupingKey, cancellationToken);
			}

			class GroupingAsyncEnumerator : IAsyncEnumerator<IGrouping<TKey, TElement>>
			{
				readonly IResultEnumerable<TElement> _elements;
				readonly Func<TElement, TKey>        _groupingKey;
				readonly CancellationToken           _cancellationToken;

				IEnumerator<IGrouping<TKey, TElement>>? _grouped;
				IGrouping<TKey, TElement>?              _current;

				public GroupingAsyncEnumerator(IResultEnumerable<TElement> elements, Func<TElement, TKey> groupingKey, CancellationToken cancellationToken)
				{
					_elements          = elements;
					_groupingKey       = groupingKey;
					_cancellationToken = cancellationToken;
				}
#if NET5_0_OR_GREATER
				public ValueTask DisposeAsync()
				{
					_grouped?.Dispose();
					return new ValueTask();
				}
#endif


				public IGrouping<TKey, TElement> Current
				{
					get
					{
						if (_grouped == null)
							throw new InvalidOperationException("Enumeration not started.");

						if (_current == null)
							throw new InvalidOperationException("Enumeration returned no result.");

						return _current;
					}
				}

				public async ValueTask<bool> MoveNextAsync()
				{
#if NET6_0_OR_GREATER
					_grouped ??= (await _elements.ToListAsync(_cancellationToken)
							.ConfigureAwait(ExtLinqOptions.ContinueOnCapturedContext))
						.GroupBy(_groupingKey)
						.GetEnumerator();
#else
					_grouped ??= _elements.ToList()
						.GroupBy(_groupingKey)
						.GetEnumerator();
#endif


					if (_grouped.MoveNext())
					{
						_current = _grouped.Current;
						return true;
					}

					_current = null;
					return false;
				}
			}
#endif



		}
	}
}
