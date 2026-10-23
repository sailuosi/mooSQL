using System;

namespace mooSQL.linq.expressions
{
	// could allow more targets later if needed
	/// <summary>
	/// TypeWrapperNameAttribute type.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class TypeWrapperNameAttribute : Attribute
	{
		/// <summary>
		/// TypeWrapperNameAttribute type.
		/// </summary>
		/// <summary>
		/// TypeWrapperNameAttribute type.
		/// </summary>
		public TypeWrapperNameAttribute(string name)
		{
			Name = name;
		}

		internal string Name { get; }
	}
}
