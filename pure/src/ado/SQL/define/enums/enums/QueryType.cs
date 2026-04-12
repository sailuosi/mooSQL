using System;

namespace mooSQL.data.model
{
	/// <summary>顶层 SQL 语句或查询的种类。</summary>
	public enum QueryType
	{
		/// <summary>查询。</summary>
		Select,
		/// <summary>删除。</summary>
		Delete,
		/// <summary>更新。</summary>
		Update,
		/// <summary>插入。</summary>
		Insert,
		/// <summary>插入或更新（UPSERT）。</summary>
		InsertOrUpdate,
		/// <summary>建表。</summary>
		CreateTable,
		/// <summary>删表。</summary>
		DropTable,
		/// <summary>截断表。</summary>
		TruncateTable,
		/// <summary>合并。</summary>
		Merge,
		/// <summary>多行/多目标插入。</summary>
		MultiInsert,
		/// <summary>未归类（如手写 SQL 未设置类型）。</summary>
		Unknown,
		/// <summary>多语句或无法单一归类。</summary>
		Composite,
	}
}
