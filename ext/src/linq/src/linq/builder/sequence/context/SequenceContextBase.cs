using System.Diagnostics;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.linq.Expressions;
	using Mapping;
	using SqlQuery;
    using mooSQL.data.model;

    [DebuggerDisplay("{ClauseContextDebuggingHelper.GetContextInfo(this)}")]
	abstract class SequenceContextBase : ClauseContextBase
	{
		protected SequenceContextBase(IClauseContext? parent, IClauseContext[] sequences, LambdaExpression? lambda)
			: base(sequences[0].Builder, sequences[0].ElementType, sequences[0].SelectQuery)
		{
			Parent          = parent;
			Sequences       = sequences;
			Body            = lambda == null ? null : SequenceHelper.PrepareBody(lambda, sequences);
			Sequence.Parent = this;
		}

		protected SequenceContextBase(IClauseContext? parent, IClauseContext sequence, LambdaExpression? lambda)
			: this(parent, new[] { sequence }, lambda)
		{
		}

		public          IClauseContext[] Sequences     { get; set; }
		public          Expression?     Body          { get; set; }
		public          IClauseContext   Sequence      => Sequences[0];

		public override Expression? Expression => Body;

		public override Expression BuildProjection(Expression path, ProjectFlags flags)
		{
			var newPath = SequenceHelper.CorrectExpression(path, this, Sequence);
			var result = Builder.BuildProjection(Sequence, newPath, flags);

			if (ExpressionEqualityComparer.Instance.Equals(newPath, result))
				return path;

			if (flags.IsTable())
				return result;

			result = SequenceHelper.CorrectExpression(result, Sequence, this);
			return result;
		}


		public override BaseSentence GetResultStatement()
		{
			return Sequence.GetResultStatement();
		}

		public override void CompleteColumns()
		{
			foreach (var sequence in Sequences)
			{
				sequence.CompleteColumns();
			}
		}

		public override void SetAlias(string? alias)
		{
			if (SelectQuery.Select.Columns.Count == 1)
			{
				SelectQuery.Select.Columns[0].Alias = alias;
			}

			if (SelectQuery.From.Tables.Count > 0)
				SelectQuery.From.Tables[SelectQuery.From.Tables.Count - 1].setAlias( alias);
		}
	}
}
