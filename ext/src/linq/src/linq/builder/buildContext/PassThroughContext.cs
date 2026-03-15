using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using Mapping;
    using mooSQL.data.model;
    using SqlQuery;

	abstract class PassThroughContext : BuildContextBase
	{
		protected PassThroughContext(IBuildContext context, SelectQueryClause selectQuery) : base(context.Builder, context.ElementType, selectQuery)
		{
			Context = context;
			Parent  = context.Parent;
		}

		protected PassThroughContext(IBuildContext context) : this(context, context.SelectQuery)
		{
		}


		public          IBuildContext Context       { get; protected set; }

		public override Expression?   Expression => Context.Expression;

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			var corrected = SequenceHelper.CorrectExpression(path, this, Context);
			var result = Builder.MakeExpression(Context, corrected, flags);

			if (flags.IsSql() && !flags.IsTest())
			{
				result = SequenceHelper.CorrectTrackingPath(result, Context, this);
			}

			return result;
		}

		public override void SetRunQuery<T>(SentenceBag<T> query, Expression expr)
		{
			Context.SetRunQuery(query, expr);
		}

		public override void SetAlias(string? alias)
		{
			Context.SetAlias(alias);
		}

		public override BaseSentence GetResultStatement()
		{
			return Context.GetResultStatement();
		}

		public override void CompleteColumns()
		{
			Context.CompleteColumns();
		}

		public override bool IsOptional => Context.IsOptional;
	}
}
