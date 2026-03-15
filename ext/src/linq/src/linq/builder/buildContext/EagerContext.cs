using System;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
    using mooSQL.data.model;
    using mooSQL.linq.Mapping;
	using mooSQL.linq.SqlQuery;

	class EagerContext : BuildContextBase
	{

		public          IBuildContext Context       { get; }

		public EagerContext(IBuildContext context, Type elementType) : base(context.Builder, elementType, context.SelectQuery)
		{
			Context = context;
		}

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (flags.IsRoot() && flags.IsSubquery())
				return path;

			var corrected = SequenceHelper.CorrectExpression(path, this, Context);
			return corrected;
		}

		public override void SetRunQuery<T>(SentenceBag<T> query, Expression expr)
		{
			Context.SetRunQuery(query, expr);
		}

		public override IBuildContext Clone(CloningContext context)
		{
			return new EagerContext(context.CloneContext(Context), ElementType);
		}

		public override BaseSentence GetResultStatement()
		{
			return Context.GetResultStatement();
		}

		public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
		{
			return this;
		}
	}
}
