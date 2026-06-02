using System.Diagnostics;

namespace mooSQL.data.model
{
	[DebuggerDisplay("TableID = {TableID}, PrimaryKeyName = {PrimaryKeyName}, ColumnName = {ColumnName}, Ordinal = {Ordinal}")]
	/// <summary>
	/// 类型 PrimaryKeyInfo。
	/// </summary>
	public class PrimaryKeyInfo
	{
		/// <summary>
		/// 字段 TableID（string）。
		/// </summary>
		public string  TableID    = null!;
		/// <summary>
		/// 字段 PrimaryKeyName（string?）。
		/// </summary>
		public string? PrimaryKeyName;
		/// <summary>
		/// 字段 ColumnName（string）。
		/// </summary>
		public string  ColumnName = null!;
		/// <summary>
		/// 字段 Ordinal（int）。
		/// </summary>
		public int     Ordinal;
	}
}