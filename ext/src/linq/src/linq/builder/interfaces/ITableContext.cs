using System;
using System.Reflection;

namespace mooSQL.linq.Linq.Builder
{
    using mooSQL.data.model;
    using mooSQL.linq.SqlQuery;

	interface ITableContext : IClauseContext
	{
		public Type     ObjectType { get; }
		public TableWord SqlTable { get; }

		public IncludeInfo  IncludeRoot { get; set; }
		public MemberInfo[]? IncludePath { get; set; }
	}
}
