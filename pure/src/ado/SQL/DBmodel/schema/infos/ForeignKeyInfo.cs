namespace mooSQL.data.model
{
	/// <summary>
	/// 类型 ForeignKeyInfo。
	/// </summary>
	public class ForeignKeyInfo
	{
		/// <summary>
		/// 字段 Name（string）。
		/// </summary>
		public string Name         = null!;
		/// <summary>
		/// 字段 ThisTableID（string）。
		/// </summary>
		public string ThisTableID  = null!;
		/// <summary>
		/// 字段 ThisColumn（string）。
		/// </summary>
		public string ThisColumn   = null!;
		/// <summary>
		/// 字段 OtherTableID（string）。
		/// </summary>
		public string OtherTableID = null!;
		/// <summary>
		/// 字段 OtherColumn（string）。
		/// </summary>
		public string OtherColumn  = null!;
		/// <summary>
		/// 字段 Ordinal（int）。
		/// </summary>
		public int    Ordinal;
	}
}