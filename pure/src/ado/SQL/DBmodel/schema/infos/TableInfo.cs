using System.Diagnostics;

namespace mooSQL.data.model
{
	[DebuggerDisplay("CatalogName = {CatalogName}, SchemaName = {SchemaName}, TableName = {TableName}, IsDefaultSchema = {IsDefaultSchema}, IsView = {IsView}, Description = {Description}")]
	/// <summary>
	/// 类型 TableInfo。
	/// </summary>
	public class TableInfo
	{
		/// <summary>
		/// 字段 TableID（string）。
		/// </summary>
		public string  TableID = null!;
		/// <summary>
		/// 字段 CatalogName（string?）。
		/// </summary>
		public string? CatalogName;
		/// <summary>
		/// 字段 SchemaName（string?）。
		/// </summary>
		public string? SchemaName;
		/// <summary>
		/// 字段 TableName（string）。
		/// </summary>
		public string  TableName = null!;
		/// <summary>
		/// 字段 Description（string?）。
		/// </summary>
		public string? Description;
		/// <summary>
		/// 字段 IsDefaultSchema（bool）。
		/// </summary>
		public bool    IsDefaultSchema;
		/// <summary>
		/// 字段 IsView（bool）。
		/// </summary>
		public bool    IsView;
		/// <summary>
		/// 字段 IsProviderSpecific（bool）。
		/// </summary>
		public bool    IsProviderSpecific;
	}
}