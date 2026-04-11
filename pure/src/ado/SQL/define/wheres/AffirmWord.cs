
using mooSQL.data.model.affirms;
using System;
using System.Collections.Generic;

namespace mooSQL.data.model
{
	/// <summary>
	/// WHERE 条件中的断言表达式基类（比较、真假等），带运算符优先级与取反语义。
	/// </summary>
	public abstract class AffirmWord : SQLElement, IAffirmWord
	{
		/// <summary>断言中使用的比较/集合运算符种类。</summary>
		public enum Operator
		{
			/// <summary>= 等于。</summary>
			Equal,
			/// <summary>&lt;&gt; / != 不等于。</summary>
			NotEqual,
			/// <summary>&gt; 大于。</summary>
			Greater,
			/// <summary>&gt;= 大于等于。</summary>
			GreaterOrEqual,
			/// <summary>!&gt; 不大于。</summary>
			NotGreater,
			/// <summary>&lt; 小于。</summary>
			Less,
			/// <summary>&lt;= 小于等于。</summary>
			LessOrEqual,
			/// <summary>!&lt; 不小于。</summary>
			NotLess,
			/// <summary>范围重叠（如 PostgreSQL 等）。</summary>
			Overlaps,
		}

#if DEBUG
		static readonly TrueAffirm  _trueInstance  = new();
		static readonly FalseAffirm _falseInstance = new();

		/// <summary>恒为真的断言单例。</summary>
		public static TrueAffirm True
		{
			get
			{
				return _trueInstance;
			}
		}

		/// <summary>恒为假的断言单例。</summary>
		public static FalseAffirm False
		{
			get
			{
				return _falseInstance;
			}
		}
#else
		/// <summary>恒为真的断言单例。</summary>
		public static readonly TrueAffirm True   = new TrueAffirm();
		/// <summary>恒为假的断言单例。</summary>
		public static readonly FalseAffirm False = new FalseAffirm();
#endif

		/// <summary>根据布尔值返回 <see cref="True"/> 或 <see cref="False"/>。</summary>
		/// <param name="isTrue">为 true 则取真断言。</param>
        public static IAffirmWord MakeBool(bool isTrue)
		{
			return isTrue ? AffirmWord.True : AffirmWord.False;
		}


		/// <summary>
		/// 使用运算符优先级初始化断言节点。
		/// </summary>
		/// <param name="precedence">用于括号与折叠的优先级，参见 <see cref="PrecedenceLv"/>。</param>
		protected AffirmWord(int precedence) : base(ClauseType.AffirmWord, null)
        {
			Precedence = precedence;
		}

		#region IPredicate Members

		/// <inheritdoc />
		public int  Precedence { get; }

		/// <inheritdoc />
		public abstract bool          CanInvert(ISQLNode nullability);
		/// <inheritdoc />
		public abstract IAffirmWord Invert(ISQLNode    nullability);

		/// <inheritdoc />
		public abstract bool Equals(IAffirmWord other, Func<IExpWord, IExpWord, bool> comparer);

		#endregion

		#region IQueryElement Members

		/// <summary>将断言内容写入 <paramref name="writer"/>（子类实现具体 SQL 片段）。</summary>
		protected abstract void WritePredicate(IElementWriter writer);

		/// <inheritdoc />
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
