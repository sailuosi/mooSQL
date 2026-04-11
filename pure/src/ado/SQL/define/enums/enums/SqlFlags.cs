using System;

namespace mooSQL.data.model
{
	/// <summary>表达式节点的语义标志（聚合、谓词、窗口函数等）。</summary>
	[Flags]
	public enum SqlFlags
	{
		/// <summary>无标志。</summary>
		None             = 0,
		/// <summary>聚合函数或含聚合语义。</summary>
		IsAggregate      = 0x1,
		/// <summary>无副作用、可缓存的纯表达式。</summary>
		IsPure           = 0x4,
		/// <summary>布尔谓词语境。</summary>
		IsPredicate      = 0x8,
		/// <summary>窗口函数。</summary>
		IsWindowFunction = 0x10,
	}
}
