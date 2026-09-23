using System;
using System.Linq.Expressions;

namespace mooSQL.linq.builder
{
	using mooSQL.data.model;
	using mooSQL.linq.clause;
	sealed class MultiInsertContext : ClauseContextBase
	{
		public MultiInsertContext(TableLikeQueryContext source)
			: base(source.Builder, source.ElementType, source.SelectQuery)
		{
			MultiInsertStatement = new MultiInsertSentence(source.Source);
			QuerySource          = source;
		}

		public TableLikeQueryContext   QuerySource          { get; }
		public MultiInsertSentence MultiInsertStatement { get; }

		public override Expression BuildProjection(Expression path, ProjectFlags flags) => path;

		public override IClauseContext Clone(CloningContext context)
			=> throw new NotImplementedException();

		public override BaseSentence GetResultStatement() => MultiInsertStatement;
	}
}
