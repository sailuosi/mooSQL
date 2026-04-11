using System;
using System.Collections.Generic;

namespace mooSQL.data.model
{
	/// <summary>GROUPING SETS 中的单列集或括号分组。</summary>
	public class GroupingSetWord : Clause, IExpWord
	{
#if DEBUG
		/// <summary>调试文本。</summary>
		public string DebugText => this.ToDebugString();
#endif

        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitGroupingSet(this);
        }
		/// <inheritdoc />
		public ClauseType NodeType => ClauseType.GroupingSet;

		/// <summary>空集合。</summary>
		public GroupingSetWord() : base(ClauseType.GroupingSet, null)
        {

		}

        /// <summary>指定成员表达式。</summary>
        public GroupingSetWord(IEnumerable<IExpWord> items) : base(ClauseType.GroupingSet, null)
        {
			Items.AddRange(items);
		}

#if OVERRIDETOSTRING

		/// <inheritdoc />
		public override string ToString()
		{
			return this.ToDebugString();
		}

#endif

		/// <inheritdoc />
		public IElementWriter ToString(IElementWriter writer)
		{
			writer.Append('(');
			for (int i = 0; i < Items.Count; i++)
			{
				//Items[i].ToString(writer);
				if (i < Items.Count - 1)
					writer.Append(", ");
			}
			writer.Append(')');
			return writer;
		}

		/// <inheritdoc />
		public bool Equals(IExpWord? other)
		{
			if (this == other)
				return true;

			if (!(other is GroupingSetWord otherSet))
				return false;

			if (Items.Count != otherSet.Items.Count)
				return false;

			for (int i = 0; i < Items.Count; i++)
			{
				if (!Items[i].Equals(otherSet.Items[i]))
					return false;
			}

			return true;
		}

		/// <inheritdoc />
		public bool Equals(IExpWord other, Func<IExpWord, IExpWord, bool> comparer)
		{
			if (this == other)
				return true;

			if (!(other is GroupingSetWord otherSet))
				return false;

			if (Items.Count != otherSet.Items.Count)
				return false;

			for (int i = 0; i < Items.Count; i++)
			{
				if (!Items[i].Equals(otherSet.Items[i], comparer))
					return false;
			}

			return true;
		}


		/// <inheritdoc />
		public int   Precedence => PrecedenceLv.Primary;
		/// <inheritdoc />
		public Type? SystemType => typeof(object);

		/// <summary>集合内的列表达式。</summary>
		public List<IExpWord> Items { get; } = new List<IExpWord>();
	}
}
