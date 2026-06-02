using System.Diagnostics;

using mooSQL.data;

namespace mooSQL.data.model
{
	[DebuggerDisplay("TableID = {TableID}, Name = {Name}, DataType = {DataType}, Length = {Length}, Precision = {Precision}, Scale = {Scale}")]
	/// <summary>
	/// 类型 ColumnInfo。
	/// </summary>
	public class ColumnInfo
	{
		/// <summary>
		/// 字段 TableID（string）。
		/// </summary>
		public string    TableID = null!;
		/// <summary>
		/// 字段 Name（string）。
		/// </summary>
		public string    Name = null!;
		/// <summary>
		/// 字段 IsNullable（bool）。
		/// </summary>
		public bool      IsNullable;
		/// <summary>
		/// 字段 Ordinal（int）。
		/// </summary>
		public int       Ordinal;
		/// <summary>
		/// 字段 DataType（string?）。
		/// </summary>
		public string?   DataType;
		/// <summary>
		/// 字段 ColumnType（string?）。
		/// </summary>
		public string?   ColumnType;
		/// <summary>
		/// 字段 Length（int?）。
		/// </summary>
		public int?      Length;
		/// <summary>
		/// 字段 Precision（int?）。
		/// </summary>
		public int?      Precision;
		/// <summary>
		/// 字段 Scale（int?）。
		/// </summary>
		public int?      Scale;
		/// <summary>
		/// 字段 Description（string?）。
		/// </summary>
		public string?   Description;
		/// <summary>
		/// 字段 IsIdentity（bool）。
		/// </summary>
		public bool      IsIdentity;
		/// <summary>
		/// 字段 SkipOnInsert（bool）。
		/// </summary>
		public bool      SkipOnInsert;
		/// <summary>
		/// 字段 SkipOnUpdate（bool）。
		/// </summary>
		public bool      SkipOnUpdate;
		/// <summary>
		/// 字段 Type（DataFam?）。
		/// </summary>
		public DataFam? Type;
	}
}