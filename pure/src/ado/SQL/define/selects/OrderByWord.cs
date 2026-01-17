using System;

namespace mooSQL.data.model
{
	/// <summary>
	/// 单个order by项，是否索引、升序等信息
	/// </summary>
	public class OrderByWord
		: SQLElement
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitOrderByItem(this);
        }
		public OrderByWord(IExpWord expression, bool isDescending, bool isPositioned) : base(ClauseType.OrderByItem, null)
        {
			Expression   = expression;
			IsDescending = isDescending;
			IsPositioned = isPositioned;
		}

		public IExpWord Expression   { get; set; }
		public bool           IsDescending { get; }
		public bool           IsPositioned { get; }

		#region Overrides

		public override ClauseType NodeType => ClauseType.OrderByItem;

		public IElementWriter ToString(IElementWriter writer)
		{
			writer.AppendElement(Expression);

			if (IsPositioned)
				writer.Append(":by_index");

			if (IsDescending)
				writer.Append(" DESC");

			return writer;
		}

		public override string ToString()
		{
			return this.ToDebugString();
		}

		#endregion
	}
}
