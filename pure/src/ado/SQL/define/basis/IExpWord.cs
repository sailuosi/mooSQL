using System;

namespace mooSQL.data.model
{
	/// <summary>可作为 SQL 表达式子树节点的抽象（含优先级与类型信息）。</summary>
	public interface IExpWord : ISQLNode, IEquatable<IExpWord>,IValueNode
	{
		/// <summary>使用自定义比较委托判断结构相等。</summary>
		bool Equals(IExpWord other, Func<IExpWord,IExpWord,bool> comparer);

		/// <summary>
		/// 优先级
		/// </summary>
		int   Precedence { get; }

        /// <summary>对应的 CLR 类型（若已知）。</summary>
        Type? SystemType { get; }
    }
}
