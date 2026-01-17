
using mooSQL.data.model.affirms;
using System;
using System.Collections.Generic;

namespace mooSQL.data.model
{
	public abstract class AffirmWord : SQLElement, IAffirmWord
	{
		public enum Operator
		{
			Equal,          // =     等于操作符.
			NotEqual,       // <> != 不等于操作符.
			Greater,        // >     大于.
			GreaterOrEqual, // >=    大于等于.
			NotGreater,     // !>    不大于.
			Less,           // <     小于.
			LessOrEqual,    // <=    小于等于.
			NotLess,        // !<    不小于.
			Overlaps,       // 重叠操作符.
		}

#if DEBUG
		static readonly TrueAffirm  _trueInstance  = new();
		static readonly FalseAffirm _falseInstance = new();

		public static TrueAffirm True
		{
			get
			{
				return _trueInstance;
			}
		}

		public static FalseAffirm False
		{
			get
			{
				return _falseInstance;
			}
		}
#else
		public static readonly TrueAffirm True   = new TrueAffirm();
		public static readonly FalseAffirm False = new FalseAffirm();
#endif

        public static IAffirmWord MakeBool(bool isTrue)
		{
			return isTrue ? AffirmWord.True : AffirmWord.False;
		}


		protected AffirmWord(int precedence) : base(ClauseType.AffirmWord, null)
        {
			Precedence = precedence;
		}

		#region IPredicate Members

		public int  Precedence { get; }

		public abstract bool          CanInvert(ISQLNode nullability);
		public abstract IAffirmWord Invert(ISQLNode    nullability);

		public abstract bool Equals(IAffirmWord other, Func<IExpWord, IExpWord, bool> comparer);

		#endregion

		#region IQueryElement Members

		protected abstract void WritePredicate(IElementWriter writer);

		public IElementWriter ToString(IElementWriter writer)
		{
			if (!writer.AddVisited(this))
				return writer.Append("...");

			WritePredicate(writer);

			writer.RemoveVisited(this);

			return writer;
		}

		#endregion
	}
}
