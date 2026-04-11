using System;

namespace mooSQL.data.model
{
	/// <summary>
	/// 单个order by项，是否索引、升序等信息
	/// </summary>
	public class OrderByWord
		: SQLElement
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitOrderByItem(this);
        }
		/// <summary>
		/// 构造单项排序键。
		/// </summary>
		/// <param name="expression">排序表达式或列。</param>
		/// <param name="isDescending">是否降序（DESC）。</param>
		/// <param name="isPositioned">是否按位置/序号排序（方言相关标记）。</param>
		public OrderByWord(IExpWord expression, bool isDescending, bool isPositioned) : base(ClauseType.OrderByItem, null)
        {
			Expression   = expression;
			IsDescending = isDescending;
			IsPositioned = isPositioned;
		}

		/// <summary>排序依据表达式。</summary>
		public IExpWord Expression   { get; set; }
		/// <summary>是否为降序。</summary>
		public bool           IsDescending { get; }
		/// <summary>是否使用“按位置”语义。</summary>
		public bool           IsPositioned { get; }

		#region Overrides

		/// <inheritdoc />
		public override ClauseType NodeType => ClauseType.OrderByItem;

		/// <inheritdoc />
		public IElementWriter ToString(IElementWriter writer)
		{
			writer.AppendElement(Expression);

			if (IsPositioned)
				writer.Append(":by_index");

			if (IsDescending)
				writer.Append(" DESC");

			return writer;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.ToDebugString();
		}

		#endregion
	}
}
