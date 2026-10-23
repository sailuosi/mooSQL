using System;

namespace mooSQL.linq.expressions
{
	/// <summary>
	/// CustomMapperAttribute type.
	/// </summary>
	[AttributeUsage(AttributeTargets.ReturnValue)]
	public class CustomMapperAttribute : Attribute
	{
		/// <summary>
		/// CustomMapperAttribute type.
		/// </summary>
		/// <summary>
		/// CustomMapperAttribute type.
		/// </summary>
		public CustomMapperAttribute(Type mapper)
		{
			Mapper = mapper;
		}

		internal Type Mapper { get; }
	}
}
