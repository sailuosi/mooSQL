using System;
using System.Reflection;

namespace mooSQL.linq.builder
{
    using mooSQL.data.model;
    using mooSQL.linq.clause;

	interface ITableContext : IClauseContext
	{
		public Type     ObjectType { get; }
		public TableWord SqlTable { get; }

		public IncludeInfo  IncludeRoot { get; set; }
		public MemberInfo[]? IncludePath { get; set; }
	}
}
