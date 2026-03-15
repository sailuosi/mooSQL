using System;
using System.Reflection;

namespace mooSQL.linq.Linq.Builder
{
    using mooSQL.data.model;
    using mooSQL.linq.SqlQuery;

	interface ITableContext : IBuildContext
	{
		public Type     ObjectType { get; }
		public TableWord SqlTable { get; }

		public LoadWithInfo  LoadWithRoot { get; set; }
		public MemberInfo[]? LoadWithPath { get; set; }
	}
}
