using System;
using System.Collections.Generic;

namespace mooSQL.data.model
{
	public class GroupingSetWord : Clause, IExpWord
	{
#if DEBUG
		public string DebugText => this.ToDebugString();
#endif

        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitGroupingSet(this);
        }
		public ClauseType NodeType => ClauseType.GroupingSet;

		public GroupingSetWord() : base(ClauseType.GroupingSet, null)
        {

		}

        public GroupingSetWord(IEnumerable<IExpWord> items) : base(ClauseType.GroupingSet, null)
        {
			Items.AddRange(items);
		}

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return this.ToDebugString();
		}

#endif

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


		public int   Precedence => PrecedenceLv.Primary;
		public Type? SystemType => typeof(object);

		public List<IExpWord> Items { get; } = new List<IExpWord>();
	}
}
