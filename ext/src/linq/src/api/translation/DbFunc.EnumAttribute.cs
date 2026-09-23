using System;

// ReSharper disable CheckNamespace

namespace mooSQL.linq
{
	using mooSQL.linq.mapping;
    using mooSQL.data.Mapping;

    partial class DbFunc
	{
		[AttributeUsage(AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
		public class EnumAttribute : MappingAttribute
		{
			public override string GetObjectID() => "..";
		}
	}
}
