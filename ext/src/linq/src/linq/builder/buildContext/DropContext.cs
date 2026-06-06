using System;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.data.model;

	sealed class DropContext : SequenceContextBase
	{
		readonly DropTableSentence _dropTableStatement;

		public DropContext(IBuildContext? parent, IBuildContext sequence, DropTableSentence dropTableStatement)
			: base(parent, sequence, null)
		{
			_dropTableStatement = dropTableStatement;
		}

		public override IBuildContext Clone(CloningContext context)
			=> new DropContext(null, context.CloneContext(Sequence), context.CloneElement(_dropTableStatement));

		public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
			=> throw new NotImplementedException();

		public override BaseSentence GetResultStatement()
			=> _dropTableStatement;
	}
}
