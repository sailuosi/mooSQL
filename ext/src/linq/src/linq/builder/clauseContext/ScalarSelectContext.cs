using System.Diagnostics;
using System.Linq.Expressions;

namespace mooSQL.linq.builder
{
	using mooSQL.linq.expressions;
	using mooSQL.linq.mapping;
	using mooSQL.linq.clause;
    using mooSQL.data.model;

	[DebuggerDisplay("{ClauseContextDebuggingHelper.GetContextInfo(this)}")]
	sealed class ScalarSelectContext : ClauseContextBase
	{
		public override Expression    Expression    => Body;

		public Expression Body { get; }

		public ScalarSelectContext(ClauseSqlTranslator builder, Expression body, SelectQueryClause selectQuery) : base(builder, body.Type, selectQuery)
		{
			Body = body;
		}

		public override Expression BuildProjection(Expression path, ProjectFlags flags)
		{
			if (SequenceHelper.IsSameContext(path, this))
			{
				var expression = Body.Unwrap();
				return expression;
			}

			return path;
		}

		public override IClauseContext Clone(CloningContext context)
		{
			return new ScalarSelectContext(Builder, context.CloneExpression(Body), context.CloneElement(SelectQuery));
		}

		public override BaseSentence GetResultStatement()
		{
			return new SelectSentence(SelectQuery);
		}
	}
}
