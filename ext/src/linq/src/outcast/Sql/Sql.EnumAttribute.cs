using System;

// ReSharper disable CheckNamespace

namespace mooSQL.linq
{
	using Mapping;
    using mooSQL.data.Mapping;

    partial class Sql
	{
		[AttributeUsage(AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
		public class EnumAttribute : MappingAttribute
		{
			public override string GetObjectID() => "..";
		}
	}
}
