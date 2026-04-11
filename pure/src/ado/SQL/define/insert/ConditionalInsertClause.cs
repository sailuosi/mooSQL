using System;

namespace mooSQL.data.model
{
	/// <summary>
	/// 带条件 when的插入
	/// </summary>
	public class ConditionalInsertClause :Clause, ISQLNode
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitConditionalInsertClause(this);
        }
		/// <summary>要执行的 INSERT 片段。</summary>
		public InsertClause     Insert { get; private set; }
		/// <summary>WHEN 条件；为 null 表示无条件分支。</summary>
		public SearchConditionWord? When   { get; private set; }

		/// <summary>构造条件插入分支。</summary>
		public ConditionalInsertClause(InsertClause insert, SearchConditionWord? when) : base(ClauseType.ConditionalInsertClause, null)
        {
			Insert = insert;
			When   = when;
		}

		/// <summary>就地替换插入体与条件。</summary>
		public void Modify(InsertClause insert, SearchConditionWord? when)
		{
			Insert = insert;
			When   = when;
		}

		#region IQueryElement

#if DEBUG
		/// <summary>调试文本。</summary>
		public string DebugText => this.ToDebugString();
#endif
		/// <inheritdoc />
		ClauseType ISQLNode.NodeType => ClauseType.ConditionalInsertClause;

        /// <inheritdoc />
        public IElementWriter ToString(IElementWriter writer)
		{
			if (When != null)
			{
				writer
					.Append("WHEN ")
					.AppendElement(When)
					.AppendLine(" THEN");
			}

			writer.AppendElement(Insert);

			return writer;
		}

		#endregion
	}
}
