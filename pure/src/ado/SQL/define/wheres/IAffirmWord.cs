using System;

namespace mooSQL.data.model
{
	/// <summary>
	/// 断言条件
	/// </summary>
	public interface IAffirmWord : ISQLNode
	{
		/// <summary>
		/// 优先级
		/// </summary>
		int  Precedence { get; }

		/// <summary>在当前可空性上下文中是否可安全取反（德摩根等）。</summary>
		/// <param name="nullability">可空性/节点上下文。</param>
		bool          CanInvert(ISQLNode nullability);
		/// <summary>对断言取反，返回新的 <see cref="IAffirmWord"/>。</summary>
		/// <param name="nullability">可空性/节点上下文。</param>
		IAffirmWord Invert(ISQLNode nullability);

		/// <summary>按表达式比较器比较两断言是否等价。</summary>
		/// <param name="other">另一断言。</param>
		/// <param name="comparer">子表达式等价比较委托。</param>
		bool Equals(IAffirmWord other, Func<IExpWord, IExpWord, bool> comparer);
	}
}
