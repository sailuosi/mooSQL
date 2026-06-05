using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace mooSQL.linq.Linq.Builder
{
	using Common;
	using Common.Internal;
	using Data;
	using Extensions;
	using Translation;
	using mooSQL.linq.Expressions;
	using mooSQL.linq.translator;
	using Mapping;
	using Reflection;
	using SqlQuery;
	using DataProvider;
	using mooSQL.data.model;
	using mooSQL.data;
	using mooSQL.data.model.affirms;
    using mooSQL.data.Mapping;
    using mooSQL.utils;
    using mooSQL.data.mapping;

    partial class ExpressionBuilder
	{
		#region LinqOptions shortcuts

		public bool CompareNullsAsValues => DBLive.dialect.Option.CompareNullsAsValues;

		#endregion

		#region Build Where

		public IBuildContext? BuildWhere(
			IBuildContext?   parent,
			IBuildContext    sequence,
			LambdaExpression condition,
			bool             checkForSubQuery,
			bool             enforceHaving,
			bool             isTest)
		{
			var buildSequnce = sequence;

			if (enforceHaving)
			{
				var root = sequence.Builder.GetRootContext(sequence,
					new ContextRefExpression(sequence.ElementType, sequence), true);

				if (root != null && root.BuildContext is GroupByBuilder.GroupByContext groupByContext)
				{
					buildSequnce = groupByContext.SubQuery;
				}
				else
				{
					enforceHaving = false;
				}
			}

			if (!enforceHaving)
			{
				if (buildSequnce is not SubQueryContext subQuery || subQuery.NeedsSubqueryForComparison)
				{
					buildSequnce = new SubQueryContext(sequence);
				}

				sequence.SetAlias(condition.Parameters[0].Name);
				sequence = buildSequnce;
			}

			var body = SequenceHelper.PrepareBody(condition, sequence);
			var expr = body.Unwrap();

			var sc = new SearchConditionWord();

			var flags = ProjectFlags.SQL;
			if (isTest)
				flags |= ProjectFlags.Test;

			var predicateVisitor = new ClausePredicateVisitor(this, buildSequnce);
			if (!predicateVisitor.BuildSearchCondition(expr, flags, sc, out var error))
			{
				if (parent == null && !isTest)
					throw error.CreateException();
				return null;
			}

			if (!isTest)
			{
				if (enforceHaving)
					buildSequnce.SelectQuery.Having.ConcatSearchCondition(sc);
				else
					buildSequnce.SelectQuery.Where.ConcatSearchCondition(sc);
			}

			if (!enforceHaving)
			{
				return buildSequnce;
			}

			return sequence;
		}

		#endregion

		#region Build Skip/Take

		public void BuildTake(IBuildContext sequence, IExpWord expr, TakeHintType? hints)
		{
			var sql = sequence.SelectQuery;

			if (hints != null && !DBLive.dialect.Option.ProviderFlags.GetIsTakeHintsSupported(hints.Value))
				throw new LinqException($"TakeHints are {hints} not supported by current database");

			if (hints != null && sql.Select.SkipValue != null)
				throw new LinqException("Take with hints could not be applied with Skip");

			if (sql.Select.TakeValue != null)
			{
				expr = new ConditionWord(
					new ExprExpr(sql.Select.TakeValue, AffirmWord.Operator.Less, expr, null),
					sql.Select.TakeValue,
					expr);
			}

			sql.Select.Take(expr, hints);
		}

		public void BuildSkip(IBuildContext sequence, IExpWord expr)
		{
			var sql = sequence.SelectQuery;

			if (sql.Select.TakeHints != null)
				throw new LinqException("Skip could not be applied with Take with hints");

			if (sql.Select.SkipValue != null)
				sql.Select.Skip(new BinaryWord(typeof(int), sql.Select.SkipValue, "+", expr, PrecedenceLv.Additive));
			else
				sql.Select.Skip(expr);

			if (sql.Select.TakeValue != null)
			{
				sql.Select.Take(
					new BinaryWord(typeof(int), sql.Select.TakeValue, "-", expr, PrecedenceLv.Additive),
					sql.Select.TakeHints);
			}
		}

		#endregion

		#region SubQueryToSql

		/// <summary>
		/// Checks that provider can handle limitation inside subquery. This function is tightly coupled with <see cref="SentenceOptimizerVisitor.OptimizeApply"/>
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		public bool IsSupportedSubquery(IBuildContext parent, IBuildContext context, out string? errorMessage)
		{
			errorMessage = null;

			if (!_validateSubqueries)
				return true;

			// No check during recursion. Cloning may fail
			if (parent.Builder.IsRecursiveBuild)
				return true;

			if (!context.Builder.DBLive.dialect.Option.ProviderFlags.IsApplyJoinSupported)
			{
				// We are trying to simulate what will be with query after optimizer's work
				//
				var cloningContext = new CloningContext();

				var clonedParentContext = cloningContext.CloneContext(parent);
				var clonedContext       = cloningContext.CloneContext(context);

				cloningContext.UpdateContextParents();

				var expr = parent.Builder.MakeExpression(clonedContext, new ContextRefExpression(clonedContext.ElementType, clonedContext), ProjectFlags.SQL);

				expr = parent.Builder.ToColumns(clonedParentContext, expr);

                JoinTableWord? fakeJoin = null;

				// add fake join there is no still reference
				if (null == clonedParentContext.SelectQuery.Find(e => e is SelectQueryClause sc && sc == clonedContext.SelectQuery))
				{
					//fakeJoin = clonedContext.SelectQuery.OuterApply().JoinedTable;

					//clonedParentContext.SelectQuery.From.Tables[0].Joins.Add(fakeJoin);
					clonedParentContext.SelectQuery.From.OuterApply(clonedContext.SelectQuery, "", null);

                }

				using var visitor = QueryHelper.SelectOptimizer.Allocate();

#if DEBUG

				var sqlText = clonedParentContext.SelectQuery.ToDebugString();

#endif

				var optimizedQuery = (SelectQueryClause)visitor.Value.Optimize(
					root: clonedParentContext.SelectQuery,
					rootElement: clonedParentContext.SelectQuery,
					providerFlags: parent.Builder.DBLive.dialect.Option.ProviderFlags,
					removeWeakJoins: false,
					DBLive,
					evaluationContext: new EvaluateContext()
				);

				if (!SqlProviderHelper.IsValidQuery(optimizedQuery, 
					    parentQuery: null, 
					    fakeJoin: fakeJoin, 
					    forColumn: false, 
					    parent.Builder.DBLive.dialect.Option.ProviderFlags, 
					    out errorMessage))
				{
					return false;
				}
			}

			return true;
		}

		int _gettingSubquery;

		public IBuildContext? GetSubQuery(IBuildContext context, Expression expr, ProjectFlags flags, out bool isSequence, out string? errorMessage)
		{
			var info = new BuildInfo(context, expr, new SelectQueryClause())
			{
				CreateSubQuery = true,
			};

			if (flags.IsForceOuter())
				info.SourceCardinality = SourceCardinality.ZeroOrMany;

			++_gettingSubquery;
			var buildResult = TryBuildSequence(info);
			--_gettingSubquery;

			isSequence = buildResult.IsSequence;

			if (buildResult.BuildContext != null)
			{
				if (_gettingSubquery == 0)
				{
					++_gettingSubquery;
					var isSupported = IsSupportedSubquery(context, buildResult.BuildContext, out errorMessage);
					--_gettingSubquery;
					if (!isSupported)
						return null;
				}
			}

			errorMessage = buildResult.AdditionalDetails;
			return buildResult.BuildContext;
		}

		#endregion

		#region ConvertExpression

		public Expression ConvertExpression(Expression expression)
		{
			using var visitor = _exposeVisitorPool.Allocate();

			var result = visitor.Value.ExposeExpression(DBLive, _optimizationContext, ParameterValues, expression, includeConvert : true, optimizeConditions : false, compactBinary : false);

			return result;
		}

		public Expression ConvertSingleExpression(Expression expression, bool inProjection)
		{
			// We can convert only these expressions, so it is shortcut to do not allocate visitor

			if (expression.NodeType is ExpressionType.Call
				                    or ExpressionType.MemberAccess
				                    or ExpressionType.New
				|| expression is BinaryExpression)
			{
				var result = ConvertExpression(expression);

				return result;
			}

			return expression;
		}

		#endregion

		#region IsServerSideOnly

		public bool IsServerSideOnly(Expression expr, bool inProjection)
		{
			return _optimizationContext.IsServerSideOnly(expr, inProjection);
		}

		#endregion

		#region CanBeConstant

		internal bool CanBeConstant(Expression expr)
		{
			if (!ParametersContext.CanBeConstant(expr))
			{
				return false;
			}
			return _optimizationContext.CanBeConstant(expr);
		}

		#endregion

		#region CanBeCompiled

		public bool CanBeCompiled(Expression expr, bool inProjection)
		{
			return _optimizationContext.CanBeCompiled(expr, inProjection);
		}

		#endregion

		#region Build Constant

		readonly Dictionary<(Expression, EntityColumn?, int), ValueWord> _constants = new ();


        #endregion
		#region CanBeTranslatedToSql

		private sealed class CanBeTranslatedToSqlContext
		{
			public CanBeTranslatedToSqlContext(ExpressionBuilder builder, IBuildContext buildContext, bool canBeCompiled)
			{
				Builder       = builder;
				BuildContext  = buildContext;
				CanBeCompiled = canBeCompiled;
			}

			public readonly ExpressionBuilder Builder;
			public readonly IBuildContext     BuildContext;
			public readonly bool              CanBeCompiled;
		}

		#endregion

		#region Helpers

		public IBuildContext? GetContext(IBuildContext? current, Expression? expression)
		{
			throw new NotImplementedException();
		}

		internal bool IsNullConstant(Expression expr)
		{
			// TODO: is it correct to return true for DefaultValueExpression for non-reference type or when default value
			// set to non-null value?
			return expr.UnwrapConvert().IsNullValue();
		}

		internal bool IsConstantOrNullValue(Expression expr)
		{
			var unwrapped = expr.UnwrapConvert();
			if (unwrapped is ConstantExpression)
				return true;
			return unwrapped.IsNullValue();
		}

		private TransformVisitor<ExpressionBuilder>? _removeNullPropagationTransformer;
		private TransformVisitor<ExpressionBuilder>? _removeNullPropagationTransformerForSearch;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private TransformVisitor<ExpressionBuilder> GetRemoveNullPropagationTransformer(bool forSearch)
		{
			if (forSearch)
				return _removeNullPropagationTransformerForSearch ??= TransformVisitor<ExpressionBuilder>.Create(this, static (ctx, e) => ctx.RemoveNullPropagation(e, true));
			else
				return _removeNullPropagationTransformer ??= TransformVisitor<ExpressionBuilder>.Create(this, static (ctx, e) => ctx.RemoveNullPropagation(e, false));
		}

		public Expression RemoveNullPropagation(IBuildContext context, Expression expr, ProjectFlags flags, bool toSql)
		{
			static bool? IsNull(Expression sqlExpr)
			{
				if (sqlExpr.IsNullValue())
					return true;

				if (sqlExpr is not SqlPlaceholderExpression placeholder)
					return null;

				return QueryHelper.IsNullValue(placeholder.Sql);
			}

			if (expr.NodeType == ExpressionType.Equal || expr.NodeType == ExpressionType.NotEqual)
			{
				var binary = (BinaryExpression)expr;

				var left  = RemoveNullPropagation(context, binary.Left, flags, true);
				var right = RemoveNullPropagation(context, binary.Right, flags, true);

				if (toSql)
				{
					binary = binary.Update(
						left,
						binary.Conversion,
						right);
				}

				return binary;
			}

			if (expr.NodeType == ExpressionType.Conditional)
			{
				var cond = (ConditionalExpression)expr;

				var test    = RemoveNullPropagation(context, cond.Test, flags, true);
				var ifTrue  = RemoveNullPropagation(context, cond.IfTrue, flags, true);
				var ifFalse = RemoveNullPropagation(context, cond.IfFalse, flags, true);

				if (test.NodeType == ExpressionType.Equal || test.NodeType == ExpressionType.NotEqual)
				{
					var testLeft  = ((BinaryExpression)test).Left;
					var testRight = ((BinaryExpression)test).Right;

					var nullLeft  = IsNull(testLeft);
					var nullRight = IsNull(testRight);

					if (nullRight == true && nullLeft == true)
					{
						return test.NodeType == ExpressionType.Equal ? cond.IfTrue : cond.IfFalse;
					}

					if (test.NodeType == ExpressionType.Equal)
					{
						if (IsNull(ifTrue) == true && (nullLeft == true || nullRight == true))
						{
							return toSql ? ifFalse : cond.IfFalse;
						}
					}
					else
					{
						if (IsNull(ifFalse) == true && (nullLeft == true || nullRight == true))
						{
							return toSql ? ifTrue : cond.IfTrue;
						}
					}
				}

				if (toSql)
				{
					cond = cond.Update(test, ifTrue, ifFalse);
				}

				return cond;
			}

			var doNotConvert = 
				expr.NodeType is ExpressionType.Equal
				              or ExpressionType.NotEqual
				              or ExpressionType.GreaterThan
				              or ExpressionType.GreaterThanOrEqual
				              or ExpressionType.LessThan
				              or ExpressionType.LessThanOrEqual;

			if (!doNotConvert && toSql)
			{
				var sql = ConvertToSqlExpr(context, expr, flags);
				if (sql is SqlPlaceholderExpression or SqlGenericConstructorExpression)
					return sql;
			}
			return expr;
		}

		public Expression RemoveNullPropagation(Expression expr, bool forSearch)
		{
			bool IsAcceptableType(Type type)
			{
				if (!forSearch)
					return type.IsReferType();

				if (type != typeof(string) && typeof(IEnumerable<>).IsSameOrParentOf(type))
					return true;

				if (!DBLive.dialect.mapping.IsScalarType(type))
					return true;

				return false;
			}

			// Do not modify parameters
			//
			if (CanBeCompiled(expr, false))
				return expr;

			switch (expr.NodeType)
			{
				case ExpressionType.Conditional:
					var conditional = (ConditionalExpression)expr;
					if (conditional.Test.NodeType == ExpressionType.NotEqual)
					{
						var binary    = (BinaryExpression)conditional.Test;
						var nullRight = IsNullConstant(binary.Right);
						var nullLeft  = IsNullConstant(binary.Left);
						if (nullRight || nullLeft)
						{
							if (nullRight && nullLeft)
							{
								return GetRemoveNullPropagationTransformer(forSearch).Transform(conditional.IfFalse);
							}
							else if (IsNullConstant(conditional.IfFalse)
								&& ((nullRight && IsAcceptableType(binary.Left.Type) ||
									(nullLeft  && IsAcceptableType(binary.Right.Type)))))
							{
								return GetRemoveNullPropagationTransformer(forSearch).Transform(conditional.IfTrue);
							}
						}
					}
					else if (conditional.Test.NodeType == ExpressionType.Equal)
					{
						var binary    = (BinaryExpression)conditional.Test;
						var nullRight = IsNullConstant(binary.Right);
						var nullLeft  = IsNullConstant(binary.Left);
						if (nullRight || nullLeft)
						{
							if (nullRight && nullLeft)
							{
								return GetRemoveNullPropagationTransformer(forSearch).Transform(conditional.IfTrue);
							}
							else if (IsNullConstant(conditional.IfTrue)
									 && ((nullRight && IsAcceptableType(binary.Left.Type) ||
										  (nullLeft && IsAcceptableType(binary.Right.Type)))))
							{
								return GetRemoveNullPropagationTransformer(forSearch).Transform(conditional.IfFalse);
							}
						}
					}
					break;
			}

			return expr;
		}

		public bool ProcessProjection(Dictionary<MemberInfo,Expression> members, Expression expression)
		{
			void CollectParameters(Type forType, MethodBase method, ReadOnlyCollection<Expression> arguments)
			{
				var pms = method.GetParameters();

				var typeMembers = TypeAccessor.GetAccessor(forType).Members;

				for (var i = 0; i < pms.Length; i++)
				{
					var param = pms[i];
					MemberAccessor? foundMember = null;
					foreach (var tm in typeMembers)
					{
						if (tm.Name == param.Name)
						{
							foundMember = tm;
							break;
						}
					}

					if (foundMember == null)
					{
						foreach (var tm in typeMembers)
						{
							if (tm.Name.Equals(param.Name, StringComparison.OrdinalIgnoreCase))
							{
								foundMember = tm;
								break;
							}
						}
					}

					if (foundMember == null)
						continue;

					if (members.ContainsKey(foundMember.MemberInfo))
						continue;

					var converted = arguments[i];

					members.Add(foundMember.MemberInfo, converted);
				}
			}

			expression = GetRemoveNullPropagationTransformer(false).Transform(expression);

			switch (expression.NodeType)
			{
				// new { ... }
				//
				case ExpressionType.New        :
					{
						var expr = (NewExpression)expression;

						if (expr.Members != null)
						{
							for (var i = 0; i < expr.Members.Count; i++)
							{
								var member = expr.Members[i];

								var converted = expr.Arguments[i];
								members.Add(member, converted);

								if (member is MethodInfo info)
									members.Add(info.GetPropertyInfo(), converted);
							}
						}

						var isScalar =DBLive.dialect.mapping .IsScalarType(expr.Type);
						if (!isScalar)
							CollectParameters(expr.Type, expr.Constructor!, expr.Arguments);

						return members.Count > 0 || !isScalar;
					}

				// new MyObject { ... }
				//
				case ExpressionType.MemberInit :
					{
						var expr        = (MemberInitExpression)expression;
						var typeMembers = TypeAccessor.GetAccessor(expr.Type).Members;

						var dic  = typeMembers
							.Select(static (m,i) => new { m, i })
							.ToDictionary(static _ => _.m.MemberInfo.Name, static _ => _.i);

						var assignments = new List<(MemberAssignment ma, int order)>();
						foreach (var ma in expr.Bindings.Cast<MemberAssignment>())
							assignments.Add((ma, dic.TryGetValue(ma.Member.Name, out var idx) ? idx : 1000000));

						foreach (var (binding, _) in assignments.OrderBy(static a => a.order))
						{
							var converted = binding.Expression;
							members.Add(binding.Member, converted);

							if (binding.Member is MethodInfo info)
								members.Add(info.GetPropertyInfo(), converted);
						}

						return true;
					}

				case ExpressionType.Call:
					{
						var mc = (MethodCallExpression)expression;

						// process fabric methods

						if (!DBLive.dialect.mapping.IsScalarType(mc.Type))
							CollectParameters(mc.Type, mc.Method, mc.Arguments);

						return members.Count > 0;
					}

				case ExpressionType.NewArrayInit:
				case ExpressionType.ListInit:
					{
						return true;
					}
				// .Select(p => everything else)
				//
				default                        :
					return false;
			}
		}

		#endregion

		#region CTE

		Dictionary<Expression, CteContext>? _cteContexts;

		public void RegisterCteContext(CteContext cteContext, Expression cteExpression)
		{
			_cteContexts ??= new(ExpressionEqualityComparer.Instance);

			_cteContexts.Add(cteExpression, cteContext);
		}

		public CteContext? FindRegisteredCteContext(Expression cteExpression)
		{
			if (_cteContexts == null)
				return null;

			_cteContexts.TryGetValue(cteExpression, out var cteContext);

			return cteContext;
		}

		#endregion

		#region Query Filter

		private Stack<Type[]>? _disabledFilters;

		public void PushDisabledQueryFilters(Type[] disabledFilters)
		{
			_disabledFilters ??= new Stack<Type[]>();
			_disabledFilters.Push(disabledFilters);
		}

		public bool IsFilterDisabled(Type entityType)
		{
			if (_disabledFilters == null || _disabledFilters.Count == 0)
				return false;
			var filter = _disabledFilters.Peek();
			if (filter.Length == 0)
				return true;
			return Array.IndexOf(filter, entityType) >= 0;
		}

		public void PopDisabledFilter()
		{
			if (_disabledFilters == null)
				throw new InvalidOperationException();

			_ = _disabledFilters.Pop();
		}

		#endregion

		#region Query Hint Stack

		List<QueryExtension>? _sqlQueryExtensionStack;

		public void PushSqlQueryExtension(QueryExtension extension)
		{
			(_sqlQueryExtensionStack ??= new()).Add(extension);
		}

		public void PopSqlQueryExtension(QueryExtension extension)
		{
			if (_sqlQueryExtensionStack == null || _sqlQueryExtensionStack.Count > 0)
				throw new InvalidOperationException();
			_sqlQueryExtensionStack.RemoveAt(_sqlQueryExtensionStack.Count - 1);
		}

		#endregion

		#region Grouping Guard

		public bool IsGroupingGuardDisabled { get; set; }

		#endregion

	}
}
