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

		bool          CanInvert(ISQLNode nullability);
		IAffirmWord Invert(ISQLNode nullability);

		bool Equals(IAffirmWord other, Func<IExpWord, IExpWord, bool> comparer);
	}
}
