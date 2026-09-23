using System;

namespace mooSQL.linq.expressions
{
	// could allow more targets later if needed
	[AttributeUsage(AttributeTargets.Method)]
	public class TypeWrapperNameAttribute : Attribute
	{
		public TypeWrapperNameAttribute(string name)
		{
			Name = name;
		}

		internal string Name { get; }
	}
}
