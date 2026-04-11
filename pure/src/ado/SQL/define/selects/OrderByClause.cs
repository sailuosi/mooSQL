using System;
using System.Collections.Generic;

namespace mooSQL.data.model
{
	/// <summary>
	/// order by 词组，持有 Items 即排序依据
	/// </summary>
	public class OrderByClause
		: ClauseBase, ISQLNode
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitOrderByClause(this);
        }
        /// <summary>
        /// 挂在指定 <see cref="SelectQueryClause"/> 上的 ORDER BY 子句。
        /// </summary>
        public OrderByClause(SelectQueryClause selectQuery, Type type = null) : base(selectQuery, ClauseType.OrderByClause, type)
        {
		}

        /// <summary>
        /// 使用已有排序项列表构造（常用于复制或合并）。
        /// </summary>
        public OrderByClause(IEnumerable<OrderByWord> items, Type type = null) : base(null, ClauseType.OrderByClause, type)
        {
			Items.AddRange(items);
		}

		/// <summary>
		/// 追加按表达式排序；<paramref name="isDescending"/> 为降序，<paramref name="isPositioned"/> 为按列序号/索引排序。
		/// </summary>
		public OrderByClause Expr(IExpWord expr, bool isDescending, bool isPositioned)
		{
			Add(expr, isDescending, isPositioned);
			return this;
		}

		/// <summary>按表达式升序追加排序项。</summary>
		public OrderByClause Expr     (IExpWord expr, bool isPositioned = false) => Expr(expr, false, isPositioned);
		/// <inheritdoc cref="Expr(IExpWord, bool, bool)" />
		public OrderByClause ExprAsc  (IExpWord expr, bool isPositioned = false) => Expr(expr, false, isPositioned);
		/// <summary>按表达式降序追加排序项。</summary>
		public OrderByClause ExprDesc(IExpWord  expr, bool isPositioned = false) => Expr(expr, true, isPositioned);

		/// <summary>按字段排序（封装为 <see cref="FieldWord"/>）。</summary>
		public OrderByClause Field(FieldWord     field, bool isDescending, bool isPositioned) => Expr(field, isDescending, isPositioned);
		/// <summary>按字段升序排序。</summary>
		public OrderByClause Field(FieldWord     field, bool isPositioned = false) => Expr(field, false, isPositioned);
		/// <summary>按字段升序排序。</summary>
		public OrderByClause FieldAsc (FieldWord field, bool isPositioned = false) => Expr(field, false, isPositioned);
		/// <summary>按字段降序排序。</summary>
		public OrderByClause FieldDesc(FieldWord field, bool isPositioned = false) => Expr(field, true, isPositioned);

		void Add(IExpWord expr, bool isDescending, bool isPositioned)
		{
			foreach (var item in Items)
				if (item.Expression.Equals(expr, (x, y) => !(x is ColumnWord col) || !col.Parent!.HasSetOperators || x == y))
					return;

			Items.Add(new OrderByWord(expr, isDescending, isPositioned));
		}

		/// <summary>ORDER BY 中的各项排序键（顺序即 SQL 中的顺序）。</summary>
		public List<OrderByWord> Items { get; } = [];

		/// <summary>是否尚未包含任何排序项。</summary>
		public bool IsEmpty => Items.Count == 0;

		#region QueryElement overrides

		/// <inheritdoc />
		public override ClauseType NodeType => ClauseType.OrderByClause;

		/// <inheritdoc />
		public IElementWriter ToString(IElementWriter writer)
		{
			if (Items.Count == 0)
				return writer;

			writer
				.AppendLine()
				.AppendLine("ORDER BY");

			using(writer.IndentScope())
				for (var index = 0; index < Items.Count; index++)
				{
					var item = Items[index];
					writer.AppendElement(item);
					if (index < Items.Count - 1)
						writer.AppendLine(',');
				}

			return writer;
		}

		#endregion

		/// <summary>清空排序项列表。</summary>
		public void Cleanup()
		{
			Items.Clear();
		}
	}
}
