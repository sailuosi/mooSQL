using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.linq.Expressions;
	using Mapping;
	using SqlQuery;
    using mooSQL.data.model;


    /// <summary>
    /// 本类包含2个功能点，scarlar和成员类型的查询，可以视为2个不同的类使用。但这意味着有多个继承者，每个继承者都要实现2个，所以直接在这里一次性实现2个
    /// </summary>
    [DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
	class SelectContext : BuildContextBase
	{
		#region Init

#if DEBUG
		public MethodCallExpression? Debug_MethodCall;
#endif


		public          Expression     Body          { [DebuggerStepThrough] get; set; }
		public          bool           IsSubQuery    { get; }
		public          IBuildContext? InnerContext  { get; }


		public override Expression? Expression => Body;

		public readonly Dictionary<MemberInfo,Expression> Members = new (new MemberInfoComparer());

		public SelectContext(IBuildContext? parent, ExpressionBuilder builder, IBuildContext? innerContext, Expression body, SelectQueryClause selectQuery, bool isSubQuery)
			: base(builder, body.Type, selectQuery)
		{
			Parent         = parent;
			InnerContext   = innerContext;
			IsSubQuery     = isSubQuery;
			Body           = body;
		}

		public SelectContext(IBuildContext? parent, LambdaExpression lambda, bool isSubQuery, params IBuildContext[] sequences)
			: this(parent, SequenceHelper.PrepareBody(lambda, sequences), sequences[0], sequences[0].SelectQuery, isSubQuery)
		{

		}

		public SelectContext(IBuildContext? parent, Expression body, IBuildContext innerContext, bool isSubQuery)
			: this(parent, body, innerContext, innerContext.SelectQuery, isSubQuery)
		{

		}

		public SelectContext(IBuildContext? parent, Expression body, IBuildContext innerContext, SelectQueryClause selectQuery, bool isSubQuery)
			: this(parent, innerContext.Builder, innerContext, body, selectQuery, isSubQuery)
		{

		}

		#endregion

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			Expression result;

			if (flags.IsAggregationRoot() && InnerContext != null)
			{
				if (SequenceHelper.IsSameContext(path, this))
				{
					if (Builder.IsSequence(this, Body))
						return Body;
					result = new ContextRefExpression(InnerContext.ElementType, InnerContext);
				}
				else
				{
					result = Builder.Project(this, path, null, 0, flags, Body, false);
					if (result is not ContextRefExpression)
					{
						result = new ContextRefExpression(InnerContext.ElementType, InnerContext);
					}
				}

				return result;
			}

			if (SequenceHelper.IsSameContext(path, this))
			{
				if (flags.IsRoot() || flags.IsAssociationRoot() /*|| flags.HasFlag(ProjectFlags.Expand)*/ || flags.IsTable() || flags.IsTraverse())
				{
					if (Body is ContextRefExpression bodyRef)
					{
						// updating type for Inheritance mapping
						//
						return bodyRef.WithType(path.Type);
					}

					if (Body.NodeType == ExpressionType.MemberAccess)
					{
						return Body;
					}

					if (Body.NodeType == ExpressionType.TypeAs)
					{
						result = Builder.Project(this, path, null, 0, flags, Body, true);
						return result;
					}

					if (flags.IsTable())
					{
						if (InnerContext != null)
							return new ContextRefExpression(InnerContext.ElementType, InnerContext);
					}

					return path;
				}

				/*
				if (!(path.Type.IsSameOrParentOf(Body.Type) || Body.Type.IsSameOrParentOf(path.Type)))
				{
					if (flags.IsExpression())
						return new SqlEagerLoadExpression((ContextRefExpression)path, path, GetEagerLoadExpression(path));
					return ExpressionBuilder.CreateSqlError(this, path);
				}
				*/

				if (Body.NodeType == ExpressionType.TypeAs)
				{
					result = Builder.Project(this, path, null, 0, flags, Body, true);
					return result;
				}

				result = Body;
				result = SequenceHelper.RemapToNewPathSimple(Builder, result, path, flags);
			}
			else
			{
				// We can omit strict for expression building. It will help to do not crash when user uses Automapper and it tries to map non accessible fields
				//
				result = Builder.Project(this, path, null, 0, flags, Body, strict: true);

				if (result is SqlErrorExpression)
				{
					// Handling dumb case With column aliases
					//

					if (Builder.HandleAlias(this, path, flags, out var newResult))
						return newResult;

					if (flags.IsExpression())
						result = Builder.Project(this, path, null, 0, flags, Body, strict: false);
				}

				result = SequenceHelper.RemapToNewPathSimple(Builder, result, path, flags);

				if (!ReferenceEquals(result, Body))
				{
					if (!flags.IsTable())
					{
						if (flags.IsSubquery())
							result = Builder.RemoveNullPropagation(this, result, flags.SqlFlag(), false);

						if ((flags.IsRoot() || flags.IsTraverse()) &&
						    !(result is ContextRefExpression || result is MemberExpression ||
						      result is MethodCallExpression))
						{
							return path;
						}
					}
				}
			}

			return result;
		}

		public override IBuildContext Clone(CloningContext context)
		{
			var sc = context.CloneElement(SelectQuery);
			return new SelectContext(null, Builder, context.CloneContext(InnerContext), context.CloneExpression(Body), sc, IsSubQuery);
		}

		public override void SetRunQuery<T>(SentenceBag<T> query, Expression expr)
		{
			var mapper = Builder.BuildMapper<T>(SelectQuery, expr);

			QueryRunner.SetRunQuery(query, mapper);
		}

		public override void SetAlias(string? alias)
		{
			if (!string.IsNullOrEmpty(alias) && !alias!.Contains("<") && SelectQuery.Select.From.Tables.Count == 1)
			{
				var table = SelectQuery.Select.From.Tables[0];
				if (table.FindAlias() == null)
					table.setAlias( alias);
			}
		}

		public override BaseSentence GetResultStatement()
		{
			return new SelectSentence(SelectQuery);
		}

		public override void CompleteColumns()
		{
		}

		public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
		{
			if (!buildInfo.CreateSubQuery || buildInfo.IsTest)
				return this;

			var expr    = Body;
			var buildResult = Builder.TryBuildSequence(new BuildInfo(buildInfo, expr));

			return buildResult.BuildContext;
		}
	}
}
