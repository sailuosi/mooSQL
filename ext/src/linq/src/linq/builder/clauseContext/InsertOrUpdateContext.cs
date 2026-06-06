using System;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.data.model;
	using SqlQuery;

	sealed class InsertOrUpdateContext : ClauseContextBase
	{
		public IClauseContext Context { get; }

		public InsertOrUpdateSentence InsertOrUpdateStatement { get; }

		public InsertOrUpdateContext(ClauseSqlTranslator buider, IClauseContext sequence,
			InsertOrUpdateSentence insertOrUpdateStatement) : base(buider, typeof(object), sequence.SelectQuery)
		{
			Context                 = sequence;
			InsertOrUpdateStatement = insertOrUpdateStatement;
		}

		public override Expression BuildProjection(Expression path, ProjectFlags flags)
		{
			if (SequenceHelper.IsSameContext(path, this))
				return Expression.Default(path.Type);
			throw new InvalidOperationException();
		}

		public override BaseSentence GetResultStatement() => InsertOrUpdateStatement;

		public override IClauseContext Clone(CloningContext context)
			=> new InsertOrUpdateContext(Builder, context.CloneContext(Context), context.CloneElement(InsertOrUpdateStatement));
	}
}
