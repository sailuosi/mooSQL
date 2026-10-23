using System;

namespace mooSQL.linq
{
	/// <summary>
	/// ExprParameterAttribute type.
	/// </summary>
	/// <summary>
	/// ExprParameterAttribute enum.
	/// </summary>
	public enum ExprParameterKind
	{
		/// <summary>
		/// Default
		/// </summary>
		/// <summary>
		/// Default
		/// </summary>
		Default,
		/// <summary>
		/// Sequence
		/// </summary>
		/// <summary>
		/// Sequence
		/// </summary>
		Sequence,
		/// <summary>
		/// Values
		/// </summary>
		/// <summary>
		/// Values
		/// </summary>
		Values
	}
	/// <summary>
	/// ExprParameterAttribute type.
	/// </summary>

	[AttributeUsage(AttributeTargets.Parameter)]
	public class ExprParameterAttribute : Attribute
	{
		/// <summary>
		/// Name
		/// </summary>
		/// <summary>
		/// Gets or sets Name.
		/// </summary>
		public string?           Name              { get; set; }
		/// <summary>
		/// ParameterKind
		/// </summary>
		/// <summary>
		/// Gets or sets ParameterKind.
		/// </summary>
		public ExprParameterKind ParameterKind     { get; set; }
		/// <summary>
		/// DoNotParameterize
		/// </summary>
		/// <summary>
		/// Gets or sets DoNotParameterize.
		/// </summary>
		public bool              DoNotParameterize { get; set; }
		/// <summary>
		/// ExprParameterAttribute type.
		/// </summary>

		public ExprParameterAttribute(string name)
		{
			Name = name;
		}
		/// <summary>
		/// ExprParameterAttribute type.
		/// </summary>

		public ExprParameterAttribute()
		{
		}
	}
}
