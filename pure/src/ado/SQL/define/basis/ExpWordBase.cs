using System;

namespace mooSQL.data.model
{
	/// <summary>
	/// SQL表达式，含有优先级、可空等信息
	/// </summary>
	public abstract class ExpWordBase : SQLElement, IExpWord
	{
		public virtual bool Equals(IExpWord? other)
		{
			if (ReferenceEquals(this, other))
				return true;

			if (ReferenceEquals(other, null))
				return false;

			return Equals(other, DefaultComparer);
		}
        internal static Func<IExpWord, IExpWord, bool> DefaultComparer = (x, y) => true;

        protected ExpWordBase(ClauseType clauseType, Type type) : base(clauseType, type)
        {
        }

        public abstract bool  Equals(IExpWord other, Func<IExpWord, IExpWord, bool> comparer);

		public abstract int   Precedence { get; }

        public abstract Type? SystemType { get; }
    }
}
