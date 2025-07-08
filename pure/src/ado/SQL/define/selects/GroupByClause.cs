using System;
using System.Collections.Generic;

namespace mooSQL.data.model
{
	public enum GroupingType
	{
		Default,
		GroupBySets,
		Rollup,
		Cube
	}

	/// <summary>
	/// group by 词组，持有 Items
	/// </summary>
	public class GroupByClause		: ClauseBase, ISQLNode
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitGroupByClause(this);
        }
        public GroupByClause(SelectQueryClause selectQuery, Type type = null) : base(selectQuery, ClauseType.GroupByClause, type)
        {
		}

        public GroupByClause(GroupingType groupingType, IEnumerable<IExpWord> items, Type type = null) : base(null, ClauseType.GroupByClause, type)
        {
			GroupingType = groupingType;
			Items.AddRange(items);
		}

		public GroupByClause Expr(IExpWord expr)
		{
			Add(expr);
			return this;
		}

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

		public GroupingType GroupingType  { get; set; } = GroupingType.Default;

		// Note: List is used in Visitor to modify elements, by replacing List by ReadOnly collection,
		// Visitor should be corrected and appropriate Modify function updated.
		public List<IExpWord> Items { get; } = new List<IExpWord>();

		public bool IsEmpty => Items.Count == 0;

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

		public override ClauseType NodeType => ClauseType.GroupByClause;

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

		public void Cleanup()
		{
			GroupingType = GroupingType.Default;
			Items.Clear();
		}
	}
}
