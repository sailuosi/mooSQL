using System;
using System.Globalization;
using System.Text;

namespace mooSQL.linq.SqlProvider
{
    using mooSQL.data.model;
    using SqlQuery;

	sealed class HintWithParameterExtensionBuilder : ISqlQueryExtensionBuilder
	{
		void ISqlQueryExtensionBuilder.Build(NullabilityContext nullability, ISqlBuilder sqlBuilder, StringBuilder stringBuilder, QueryExtension sqlQueryExtension)
		{
			var hint  = ((ValueWord)sqlQueryExtension.Arguments["hint"]).    Value;
			var param = GetValue((ValueWord)sqlQueryExtension.Arguments["hintParameter"]);

			stringBuilder.Append(CultureInfo.InvariantCulture, $"{hint}({param})");

			object? GetValue(ValueWord value)
			{
				return value.Value is Sql.SqlID id ? sqlBuilder.BuildSqlID(id) : value.Value;
			}
		}
	}
}
