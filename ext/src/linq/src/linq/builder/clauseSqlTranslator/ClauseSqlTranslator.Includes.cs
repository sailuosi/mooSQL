using System.Collections.Generic;
using System.Linq;

namespace mooSQL.linq.Linq.Builder
{
	using Common;
	using mooSQL.linq.Reflection;

	internal partial class ClauseSqlTranslator
	{
		public List<IncludeInfo> GetTableIncludes(ITableContext table)
		{
			var loadWith = table.IncludeRoot;
			if (table.IncludePath != null)
			{
				foreach (var memberInfo in table.IncludePath)
				{
					var found = loadWith.NextInfos?.FirstOrDefault(li =>
						MemberInfoEqualityComparer.Default.Equals(li.MemberInfo, memberInfo));

					found ??= loadWith.NextInfos?.FirstOrDefault(li => li.MemberInfo?.Name == memberInfo.Name);

					if (found != null)
					{
						loadWith = found;
					}
					else
					{
						loadWith.NextInfos ??= new();
						var newInfo = new IncludeInfo(memberInfo, false);
						loadWith.NextInfos.Add(newInfo);

						loadWith = newInfo;
					}
				}

				if (loadWith.NextInfos != null)
					return loadWith.NextInfos;
			}

			loadWith.NextInfos ??= new();

			// ToList() is important here
			return loadWith.NextInfos.ToList();
		}
	}
}
