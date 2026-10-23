using System;

// ReSharper disable CheckNamespace

namespace mooSQL.linq
{
	using mooSQL.linq.mapping;
    using mooSQL.data.Mapping;

    partial class DbFunc
	{
		/// <summary>
		/// EnumAttribute
		/// </summary>
		[AttributeUsage(AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
		public class EnumAttribute : MappingAttribute
		{
	/// <summary>
	/// Gets GetObjectID.
	/// </summary>
	
			public override string GetObjectID() => "..";
		}
	}
}
