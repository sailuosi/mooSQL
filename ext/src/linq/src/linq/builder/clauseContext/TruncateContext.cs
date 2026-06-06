namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.data.model;

	sealed class TruncateContext : PassThroughContext
	{
		readonly TruncateTableSentence _truncateTableStatement;

		public TruncateContext(IClauseContext sequence, TruncateTableSentence truncateTableStatement)
			: base(sequence, sequence.SelectQuery)
		{
			_truncateTableStatement = truncateTableStatement;
		}

		public override IClauseContext Clone(CloningContext context)
			=> new TruncateContext(context.CloneContext(Context), context.CloneElement(_truncateTableStatement));

		public override BaseSentence GetResultStatement()
			=> _truncateTableStatement;
	}
}
