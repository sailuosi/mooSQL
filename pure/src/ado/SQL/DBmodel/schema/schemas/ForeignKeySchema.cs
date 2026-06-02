using System.Collections.Generic;

namespace mooSQL.data
{
	/// <summary>
	/// 类型 ForeignKeySchema。
	/// </summary>
	public class ForeignKeySchema
	{
		/// <summary>
		/// 属性 KeyName（string）。
		/// </summary>
		public string             KeyName       { get; set; } = null!;
		/// <summary>
		/// 属性 ThisTable（TableSchema?）。
		/// </summary>
		public TableSchema?       ThisTable     { get; set; }
		/// <summary>
		/// 属性 OtherTable（TableSchema）。
		/// </summary>
		public TableSchema        OtherTable    { get; set; } = null!;
		/// <summary>
		/// 属性 ThisColumns（List<ColumnSchema>）。
		/// </summary>
		public List<ColumnSchema> ThisColumns   { get; set; } = null!;
		/// <summary>
		/// 属性 OtherColumns（List<ColumnSchema>）。
		/// </summary>
		public List<ColumnSchema> OtherColumns  { get; set; } = null!;
		/// <summary>
		/// 属性 CanBeNull（bool）。
		/// </summary>
		public bool               CanBeNull     { get; set; }
		/// <summary>
		/// 属性 BackReference（ForeignKeySchema?）。
		/// </summary>
		public ForeignKeySchema?  BackReference { get; set; }
		/// <summary>
		/// 属性 MemberName（string）。
		/// </summary>
		public string             MemberName    { get; set; } = null!;

		private AssociationType _associationType = AssociationType.Auto;
		/// <summary>
		/// 属性 AssociationType（AssociationType）。
		/// </summary>
		public  AssociationType  AssociationType
		{
			get => _associationType;
			set
			{
				_associationType = value;

				if (BackReference != null)
				{
					switch (value)
					{
						case AssociationType.Auto      : BackReference.AssociationType = AssociationType.Auto;      break;
						case AssociationType.OneToOne  : BackReference.AssociationType = AssociationType.OneToOne;  break;
						case AssociationType.OneToMany : BackReference.AssociationType = AssociationType.ManyToOne; break;
						case AssociationType.ManyToOne : BackReference.AssociationType = AssociationType.OneToMany; break;
					}
				}
			}
		}
	}
}