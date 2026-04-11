namespace mooSQL.data.model
{
	/// <summary>集合运算（UNION/EXCEPT/INTERSECT 及其 ALL 变体）。</summary>
	public enum SetOperation
	{
		/// <summary><c>UNION</c>（去重）。</summary>
		Union,
		/// <summary><c>UNION ALL</c>。</summary>
		UnionAll,
		/// <summary><c>EXCEPT</c> / <c>MINUS</c>（去重）。</summary>
		Except,
		/// <summary><c>EXCEPT ALL</c>。</summary>
		ExceptAll,
		/// <summary><c>INTERSECT</c>（去重）。</summary>
		Intersect,
		/// <summary><c>INTERSECT ALL</c>。</summary>
		IntersectAll,
	}
}
