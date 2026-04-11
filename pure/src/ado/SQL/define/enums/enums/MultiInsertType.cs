namespace mooSQL.data.model
{
	/// <summary>多表插入（如 Oracle <c>INSERT ALL</c>）的分支语义。</summary>
	public enum MultiInsertType
	{
		/// <summary>无条件插入所有目标行。</summary>
		Unconditional,
		/// <summary>对所有 WHEN 条件求值并插入匹配分支。</summary>
		All,
		/// <summary>按顺序匹配第一个满足的 WHEN。</summary>
		First,
	}
}
