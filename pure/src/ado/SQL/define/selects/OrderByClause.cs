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
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitOrderByClause(this);
        }
        public OrderByClause(SelectQueryClause selectQuery, Type type = null) : base(selectQuery, ClauseType.OrderByClause, type)
        {
		}

        public OrderByClause(IEnumerable<OrderByWord> items, Type type = null) : base(null, ClauseType.OrderByClause, type)
        {
			Items.AddRange(items);
		}

		public OrderByClause Expr(IExpWord expr, bool isDescending, bool isPositioned)
		{
			Add(expr, isDescending, isPositioned);
			return this;
		}

		public OrderByClause Expr     (IExpWord expr, bool isPositioned = false) => Expr(expr, false, isPositioned);
		public OrderByClause ExprAsc  (IExpWord expr, bool isPositioned = false) => Expr(expr, false, isPositioned);
		public OrderByClause ExprDesc(IExpWord  expr, bool isPositioned = false) => Expr(expr, true, isPositioned);

		public OrderByClause Field(FieldWord     field, bool isDescending, bool isPositioned) => Expr(field, isDescending, isPositioned);
		public OrderByClause Field(FieldWord     field, bool isPositioned = false) => Expr(field, false, isPositioned);
		public OrderByClause FieldAsc (FieldWord field, bool isPositioned = false) => Expr(field, false, isPositioned);
		public OrderByClause FieldDesc(FieldWord field, bool isPositioned = false) => Expr(field, true, isPositioned);

		void Add(IExpWord expr, bool isDescending, bool isPositioned)
		{
			foreach (var item in Items)
				if (item.Expression.Equals(expr, (x, y) => !(x is ColumnWord col) || !col.Parent!.HasSetOperators || x == y))
					return;

			Items.Add(new OrderByWord(expr, isDescending, isPositioned));
		}

		public List<OrderByWord> Items { get; } = [];

		public bool IsEmpty => Items.Count == 0;

		#region QueryElement overrides

		public override ClauseType NodeType => ClauseType.OrderByClause;

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

		public void Cleanup()
		{
			Items.Clear();
		}
	}
}
