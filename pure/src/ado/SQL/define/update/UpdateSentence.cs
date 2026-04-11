using System;
using System.Collections.Generic;

namespace mooSQL.data.model
{
	/// <summary>
	/// 完整 <c>UPDATE</c> 语句节点（可含 WITH、SET、FROM/WHERE、OUTPUT 等）。
	/// </summary>
	public class UpdateSentence : BaseSentenceWithQuery
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitUpdateSentence(this);
        }

		/// <inheritdoc />
		public override QueryType QueryType          => QueryType.Update;
		/// <inheritdoc />
		public override ClauseType NodeType => ClauseType.UpdateStatement;

		/// <summary>可选 <c>OUTPUT</c> 子句（如 SQL Server）。</summary>
		public OutputClause? Output { get; set; }

		private UpdateClause? _update;

		/// <summary>UPDATE 目标与 SET 片段；首次访问时懒创建。</summary>
		public UpdateClause Update
		{
			get => _update ??= new UpdateClause();
			set => _update = value;
		}

		/// <summary>是否已显式创建过 <see cref="Update"/> 片段。</summary>
		internal bool HasUpdate => _update != null;


		/// <summary>可选子查询/CTE 体与语句 CLR 类型。</summary>
		public UpdateSentence(SelectQueryClause? selectQuery, Type type = null) : base(selectQuery, ClauseType.UpdateStatement, type)
        {
		}

		/// <inheritdoc />
		public override IElementWriter ToString(IElementWriter writer)
		{
			writer
				.AppendTag(Tag)
				.AppendElement(With)
				.AppendLine("UPDATE")
				.AppendElement(Update)
				.AppendLine()
				.AppendElement(SelectQuery)
				.AppendElement(Output);

			return writer;
		}

	}
}
