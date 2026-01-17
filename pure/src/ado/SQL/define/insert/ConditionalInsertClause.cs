using System;

namespace mooSQL.data.model
{
	/// <summary>
	/// 带条件 when的插入
	/// </summary>
	public class ConditionalInsertClause :Clause, ISQLNode
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitConditionalInsertClause(this);
        }
		public InsertClause     Insert { get; private set; }
		public SearchConditionWord? When   { get; private set; }

		public ConditionalInsertClause(InsertClause insert, SearchConditionWord? when) : base(ClauseType.ConditionalInsertClause, null)
        {
			Insert = insert;
			When   = when;
		}

		public void Modify(InsertClause insert, SearchConditionWord? when)
		{
			Insert = insert;
			When   = when;
		}

		#region IQueryElement

#if DEBUG
		public string DebugText => this.ToDebugString();
#endif
		ClauseType ISQLNode.NodeType => ClauseType.ConditionalInsertClause;

        IElementWriter ToString(IElementWriter writer)
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
