using System;
using System.Linq;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using Extensions;
	using mooSQL.linq.Expressions;
    using mooSQL.linq.ext;

    [BuildsMethodCall(nameof(LinqExtensions.QueryName))]
	sealed class QueryNameBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable();

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence    = builder.BuildSequence(new(buildInfo, methodCall.Arguments[0]));

			sequence.SelectQuery.QueryName = (string?)builder.EvaluateExpression(methodCall.Arguments[1]);
			sequence = new SubQueryContext(sequence);

			return BuildSequenceResult.FromContext(sequence);
		}
	}
}
