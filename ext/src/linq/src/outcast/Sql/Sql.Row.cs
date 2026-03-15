using System;
using System.Linq;

namespace mooSQL.linq
{
    using mooSQL.data.model;
    using mooSQL.data.model.affirms;
    using SqlQuery;

	partial class Sql
	{
		sealed class RowBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var args = Array.ConvertAll(builder.Arguments, x => builder.ConvertExpressionToSql(x));

				if (args.Any(a => a == null))
				{
					builder.IsConvertible = false;
					return;
				}

				builder.ResultExpression = new RowWord(args!);
			}
		}

		sealed class OverlapsBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var args = Array.ConvertAll(builder.Arguments, x => builder.ConvertExpressionToSql(x));

				if (args.Any(a => a == null))
				{
					builder.IsConvertible = false;
					return;
				}

				builder.ResultExpression = new SearchConditionWord(false, new ExprExpr(args[0]!, AffirmWord.Operator.Overlaps, args[1]!, false));
			}
		}
	}
}
