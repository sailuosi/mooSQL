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
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitMergeSentence(this);
        }
		private const string TargetAlias = "Target";

		/// <summary>目标表默认别名为 <c>Target</c>。</summary>
		public MergeSentence(ITableNode target, Type type = null) : base(null, ClauseType.MergeStatement, type)
        {
			Target = new DerivatedTableWord(target, TargetAlias);
		}

        /// <summary>完整指定 WITH、表提示、目标/源、ON 与 WHEN 操作列表。</summary>
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

		/// <summary>方言表提示文本。</summary>
		public string?                       Hint       { get;  set; }
		/// <summary>MERGE INTO 目标。</summary>
		public ITableNode Target     { get; private  set; }
		/// <summary>USING 子句中的源表或查询。</summary>
		public ITableNode Source     { get;  set; } = null!;
		/// <summary>ON 连接条件。</summary>
		public SearchConditionWord            On         { get; private  set; } = new();
		/// <summary>各 WHEN 分支。</summary>
		public List<MergeOperationClause> Operations { get; private  set; } = new();
		/// <summary>OUTPUT 子句。</summary>
		public OutputClause?              Output     { get; set; }

		/// <summary>是否包含向标识列插入的分支。</summary>
		public bool                          HasIdentityInsert => Operations.Any(o => o.OperationType == MergeOperateType.Insert && o.Items.Any(item => item.Column is FieldWord field && field.IsIdentity));
		/// <inheritdoc />
		public override QueryType            QueryType         => QueryType.Merge;
		/// <inheritdoc />
		public override ClauseType     NodeType       => ClauseType.MergeStatement;

		/// <summary>更新目标表、源、ON 条件与可选 <c>OUTPUT</c> 子句。</summary>
		public void Update(ITableNode target, ITableNode source, SearchConditionWord on, OutputClause? output)
		{
			Target = target;
			Source = source;
			On     = on;
			Output = output;
		}

		/// <inheritdoc />
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
		/// <inheritdoc />
		public override SelectQueryClause? SelectQuery
		{
			get => base.SelectQuery;
			set => throw new InvalidOperationException();
		}


	}
}
