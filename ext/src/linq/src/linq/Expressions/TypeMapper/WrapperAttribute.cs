using System;

namespace mooSQL.linq.expressions
{
	/// <summary>
	/// WrapperAttribute type.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Delegate)]
	public class WrapperAttribute : Attribute
	{
		/// <summary>
		/// WrapperAttribute type.
		/// </summary>
		/// <summary>
		/// WrapperAttribute type.
		/// </summary>
		public WrapperAttribute() { }
		/// <summary>
		/// WrapperAttribute type.
		/// </summary>

		public WrapperAttribute(string typeName)
		{
			TypeName = typeName;
		}
		/// <summary>
		/// Gets or sets TypeName.
		/// </summary>

		public string? TypeName { get; }
	}
}
