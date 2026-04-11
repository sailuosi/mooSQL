namespace mooSQL.data.model
{
	/// <summary>建表等场景下列默认是否可空。</summary>
	public enum DefaultNullable
	{
		/// <summary>未指定（由方言或上下文决定）。</summary>
		None,
		/// <summary>默认可空。</summary>
		Null,
		/// <summary>默认非空。</summary>
		NotNull
	}
}
