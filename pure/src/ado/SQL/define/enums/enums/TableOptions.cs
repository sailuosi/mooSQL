using System;
using System.Runtime.CompilerServices;



namespace mooSQL.data
{


	/// <summary>
	/// 表行为选项，控制表存在时的动作
	/// </summary>
	[Flags]
	
	public enum TableOptions
	{
		/// <summary>未设置任何选项。</summary>
		NotSet                     = 0b000000000,
		/// <summary>占位基础位（无额外语义）。</summary>
		None                       = 0b000000001,
		/// <summary>
		/// 不存在则创建
		/// </summary>
		CreateIfNotExists          = 0b000000010,
		/// <summary>
		/// 存在则删除
		/// </summary>
		DropIfExists               = 0b000000100,
		/// <summary>
		/// 临时表
		/// </summary>
		IsTemporary                = 0b000001000,
		/// <summary>
		/// 临时会话表
		/// </summary>
		IsLocalTemporaryStructure  = 0b000010000,
		/// <summary>
		/// 全局临时表
		/// </summary>
		IsGlobalTemporaryStructure = 0b000100000,
		/// <summary>
		/// 本地临时表
		/// </summary>
		IsLocalTemporaryData       = 0b001000000,
		/// <summary>
		/// 全局临时表，其他会话可见
		/// </summary>
		IsGlobalTemporaryData      = 0b010000000,
		/// <summary>
		/// 全局临时表，事务可见
		/// </summary>
		IsTransactionTemporaryData = 0b100000000,

		/// <summary>存在性检查相关选项组合。</summary>
		CheckExistence             = CreateIfNotExists | DropIfExists,
		/// <summary>任一类临时表选项已设置。</summary>
		IsTemporaryOptionSet       = IsTemporary | IsLocalTemporaryStructure | IsGlobalTemporaryStructure | IsLocalTemporaryData | IsGlobalTemporaryData | IsTransactionTemporaryData,
	}


}
