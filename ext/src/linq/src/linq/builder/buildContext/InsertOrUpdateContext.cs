using System;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.data.model;
	using SqlQuery;

	sealed class InsertOrUpdateContext : BuildContextBase
	{
		public IBuildContext Context { get; }

		public InsertOrUpdateSentence InsertOrUpdateStatement { get; }

		public InsertOrUpdateContext(ClauseSqlTranslator buider, IBuildContext sequence,
			InsertOrUpdateSentence insertOrUpdateStatement) : base(buider, typeof(object), sequence.SelectQuery)
		{
			Context                 = sequence;
			InsertOrUpdateStatement = insertOrUpdateStatement;
		}

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (SequenceHelper.IsSameContext(path, this))
				return Expression.Default(path.Type);
			throw new InvalidOperationException();
		}

		public override BaseSentence GetResultStatement() => InsertOrUpdateStatement;

		public override IBuildContext Clone(CloningContext context)
			=> new InsertOrUpdateContext(Builder, context.CloneContext(Context), context.CloneElement(InsertOrUpdateStatement));
	}
}
