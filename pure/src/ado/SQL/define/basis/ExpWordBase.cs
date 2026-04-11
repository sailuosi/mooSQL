using System;

namespace mooSQL.data.model
{
	/// <summary>
	/// SQL表达式，含有优先级、可空等信息
	/// </summary>
	public abstract class ExpWordBase : SQLElement, IExpWord
	{
		/// <inheritdoc />
		public virtual bool Equals(IExpWord? other)
		{
			if (ReferenceEquals(this, other))
				return true;

			if (ReferenceEquals(other, null))
				return false;

			return Equals(other, DefaultComparer);
		}
        /// <summary>内部默认比较委托。</summary>
        internal static Func<IExpWord, IExpWord, bool> DefaultComparer = (x, y) => true;

        /// <summary>由子类指定 <see cref="ClauseType"/> 与 CLR 类型。</summary>
        protected ExpWordBase(ClauseType clauseType, Type type) : base(clauseType, type)
        {
        }

        /// <inheritdoc />
        public abstract bool  Equals(IExpWord other, Func<IExpWord, IExpWord, bool> comparer);

		/// <inheritdoc />
		public abstract int   Precedence { get; }

        /// <inheritdoc />
        public abstract Type? SystemType { get; }
    }
}
