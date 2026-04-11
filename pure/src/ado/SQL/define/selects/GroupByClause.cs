using System;
using System.Collections.Generic;

namespace mooSQL.data.model
{
	/// <summary>GROUP BY 的分组扩展形式。</summary>
	public enum GroupingType
	{
		/// <summary>普通列分组。</summary>
		Default,
		/// <summary>GROUPING SETS。</summary>
		GroupBySets,
		/// <summary>ROLLUP。</summary>
		Rollup,
		/// <summary>CUBE。</summary>
		Cube
	}

	/// <summary>
	/// group by 词组，持有 Items
	/// </summary>
	public class GroupByClause		: ClauseBase, ISQLNode
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitGroupByClause(this);
        }
        /// <summary>绑定到查询体。</summary>
        public GroupByClause(SelectQueryClause selectQuery, Type type = null) : base(selectQuery, ClauseType.GroupByClause, type)
        {
		}

        /// <summary>独立分组列表（无外层查询体）。</summary>
        public GroupByClause(GroupingType groupingType, IEnumerable<IExpWord> items, Type type = null) : base(null, ClauseType.GroupByClause, type)
        {
			GroupingType = groupingType;
			Items.AddRange(items);
		}

		/// <summary>追加表达式并返回自身（链式）。</summary>
		public GroupByClause Expr(IExpWord expr)
		{
			Add(expr);
			return this;
		}

		/// <summary>追加字段表达式。</summary>
		public GroupByClause Field(FieldWord field)
		{
			return Expr(field);
		}

		void Add(IExpWord expr)
		{
			foreach (var e in Items)
				if (e.Equals(expr))
					return;

			Items.Add(expr);
		}

		/// <summary>ROLLUP/CUBE/GROUPING SETS 等模式。</summary>
		public GroupingType GroupingType  { get; set; } = GroupingType.Default;

		// Note: List is used in Visitor to modify elements, by replacing List by ReadOnly collection,
		// Visitor should be corrected and appropriate Modify function updated.
		/// <summary>分组键表达式列表。</summary>
		public List<IExpWord> Items { get; } = new List<IExpWord>();

		/// <summary>是否尚未指定分组键。</summary>
		public bool IsEmpty => Items.Count == 0;

		/// <summary>展开枚举（含 <see cref="GroupingSetWord"/> 内层项）。</summary>
		public IEnumerable<IExpWord> EnumItems()
		{
			foreach (var item in Items)
			{
				if (item is GroupingSetWord groupingSet)
				{
					foreach (var gropingSetItem in groupingSet.Items)
					{
						yield return gropingSetItem;
					}
				}
				else
				{
					yield return item;
				}
			}
		}

		#region QueryElement overrides

		/// <inheritdoc />
		public override ClauseType NodeType => ClauseType.GroupByClause;

		/// <inheritdoc />
		public IElementWriter ToString(IElementWriter writer)
		{
			if (Items.Count == 0)
				return writer;

			writer
				.AppendLine()
				.AppendLine(" GROUP BY");

			switch (GroupingType)
			{
				case GroupingType.Default:
					break;
				case GroupingType.GroupBySets:
					writer.AppendLine(" GROUPING SETS (");
					break;
				case GroupingType.Rollup:
					writer.AppendLine(" ROLLUP (");
					break;
				case GroupingType.Cube:
					writer.AppendLine(" CUBE (");
					break;
				default:
					throw new InvalidOperationException($"Unexpected grouping type: {GroupingType}");
			}

			using(writer.IndentScope())
			{
				for (var index = 0; index < Items.Count; index++)
				{
					var item = Items[index];
					writer.AppendElement(item);
					if (index < Items.Count - 1)
						writer.AppendLine(',');
				}
			}

			if (GroupingType != GroupingType.Default)
				writer.Append(')');

			return writer;
		}

		#endregion

		/// <summary>重置为默认空分组。</summary>
		public void Cleanup()
		{
			GroupingType = GroupingType.Default;
			Items.Clear();
		}
	}
}
