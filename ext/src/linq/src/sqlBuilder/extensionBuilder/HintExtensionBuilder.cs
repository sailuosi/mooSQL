using System;
using System.Text;

namespace mooSQL.linq.SqlProvider
{
    using mooSQL.data.model;
    using SqlQuery;

	sealed class HintExtensionBuilder : ISqlQueryExtensionBuilder
	{
		void ISqlQueryExtensionBuilder.Build(NullabilityContext nullability, ISqlBuilder sqlBuilder, StringBuilder stringBuilder, QueryExtension sqlQueryExtension)
		{
			var hint = (ValueWord)sqlQueryExtension.Arguments["hint"];
			stringBuilder.Append((string)hint.Value!);
		}
	}
}
