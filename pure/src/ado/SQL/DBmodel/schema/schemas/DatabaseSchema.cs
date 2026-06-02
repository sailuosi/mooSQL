using System.Collections.Generic;
using System.Data;



namespace mooSQL.data
{
	
	/// <summary>
	/// 类型 DatabaseSchema。
	/// </summary>
	public class DatabaseSchema
	{
		/// <summary>
		/// 属性 DataSource（string）。
		/// </summary>
		public string                DataSource                    { get; set; } = null!;
		/// <summary>
		/// 属性 Database（string）。
		/// </summary>
		public string                Database                      { get; set; } = null!;
		/// <summary>
		/// 属性 ServerVersion（string）。
		/// </summary>
		public string                ServerVersion                 { get; set; } = null!;
		/// <summary>
		/// 属性 Tables（List<TableSchema>）。
		/// </summary>
		public List<TableSchema>     Tables                        { get; set; } = null!;
		/// <summary>
		/// 属性 Procedures（List<ProcedureSchema>）。
		/// </summary>
		public List<ProcedureSchema> Procedures                    { get; set; } = null!;
		/// <summary>
		/// 属性 DataTypesSchema（DataTable?）。
		/// </summary>
		public DataTable?            DataTypesSchema               { get; set; }
		/// <summary>
		/// 属性 ProviderSpecificTypeNamespace（string?）。
		/// </summary>
		public string?               ProviderSpecificTypeNamespace { get; set; }
	}
}