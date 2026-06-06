using System;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.data.model;
	using SqlQuery;

	sealed class MultiInsertContext : BuildContextBase
	{
		public MultiInsertContext(TableLikeQueryContext source)
			: base(source.Builder, source.ElementType, source.SelectQuery)
		{
			MultiInsertStatement = new MultiInsertSentence(source.Source);
			QuerySource          = source;
		}

		public TableLikeQueryContext   QuerySource          { get; }
		public MultiInsertSentence MultiInsertStatement { get; }

		public override Expression MakeExpression(Expression path, ProjectFlags flags) => path;

		public override IBuildContext Clone(CloningContext context)
			=> throw new NotImplementedException();

		public override BaseSentence GetResultStatement() => MultiInsertStatement;
	}
}
