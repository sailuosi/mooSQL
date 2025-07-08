using System;

namespace mooSQL.data.model
{
	public interface IExpWord : ISQLNode, IEquatable<IExpWord>,IValueNode
	{
		bool Equals(IExpWord other, Func<IExpWord,IExpWord,bool> comparer);

		/// <summary>
		/// 优先级
		/// </summary>
		int   Precedence { get; }

        Type? SystemType { get; }
    }
}
