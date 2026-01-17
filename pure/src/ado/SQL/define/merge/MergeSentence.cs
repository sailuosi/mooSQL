using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace mooSQL.data.model
{
	/// <summary>
	/// merge语句
	/// </summary>
	public class MergeSentence : BaseSentenceWithQuery
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitMergeSentence(this);
        }
		private const string TargetAlias = "Target";

		public MergeSentence(ITableNode target, Type type = null) : base(null, ClauseType.MergeStatement, type)
        {
			Target = new DerivatedTableWord(target, TargetAlias);
		}

        public MergeSentence(
			WithClause?                       with,
			string?                              hint,
            ITableNode target,
			ITableNode                   source,
			SearchConditionWord                   on,
			IEnumerable<MergeOperationClause> operations, Type type = null) 
			: base(null, ClauseType.MergeStatement, type)
        {
			With = with;
			Hint = hint;
			Target = target;
			Source = source;
			On = on;

			foreach (var operation in operations)
				Operations.Add(operation);
		}

		public string?                       Hint       { get;  set; }
		public ITableNode Target     { get; private  set; }
		public ITableNode Source     { get;  set; } = null!;
		public SearchConditionWord            On         { get; private  set; } = new();
		public List<MergeOperationClause> Operations { get; private  set; } = new();
		public OutputClause?              Output     { get; set; }

		public bool                          HasIdentityInsert => Operations.Any(o => o.OperationType == MergeOperateType.Insert && o.Items.Any(item => item.Column is FieldWord field && field.IsIdentity));
		public override QueryType            QueryType         => QueryType.Merge;
		public override ClauseType     NodeType       => ClauseType.MergeStatement;

		public void Update(ITableNode target, ITableNode source, SearchConditionWord on, OutputClause? output)
		{
			Target = target;
			Source = source;
			On     = on;
			Output = output;
		}

		public override IElementWriter ToString(IElementWriter writer)
		{
			writer
				.AppendElement(With)
				.Append("MERGE INTO ")
				.AppendElement(Target)
				.AppendLine()
				.Append("USING (")
				.AppendElement(Source)
				.AppendLine(")")
				.Append("ON ")
				.AppendElement(On)
				.AppendLine();

			foreach (var operation in Operations)
			{
				writer
					.AppendElement(operation)
					.AppendLine();
			}

			if (Output?.HasOutput == true)
				writer.AppendElement(Output);
			return writer;
		}



		//[NotNull]
		public override SelectQueryClause? SelectQuery
		{
			get => base.SelectQuery;
			set => throw new InvalidOperationException();
		}


	}
}
