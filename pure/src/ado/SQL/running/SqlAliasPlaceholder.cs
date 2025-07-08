using System;

using mooSQL.data.model;

namespace mooSQL.linq.SqlQuery
{
	public class AliasPlaceholderWord : Clause, IExpWord
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitAliasPlaceholder(this);
        }
        public static readonly AliasPlaceholderWord Instance = new();

		AliasPlaceholderWord() : base(ClauseType.SqlAliasPlaceholder, null) { }

#if DEBUG
		public string DebugText => this.ToDebugString();
#endif

		public ClauseType NodeType => ClauseType.SqlAliasPlaceholder;



		public bool Equals(IExpWord? other)
		{
			return other == this;
		}

		public bool Equals(IExpWord other, Func<IExpWord, IExpWord, bool> comparer)
		{
			return comparer(this, other);
		}

		public bool CanBeNullable(NullabilityContext nullability) => false;
		public bool CanBeNull => false;
		public int Precedence => PrecedenceLv.Primary;
		public Type SystemType => typeof(object);
	}
}
