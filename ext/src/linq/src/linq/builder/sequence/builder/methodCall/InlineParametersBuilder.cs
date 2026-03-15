using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.linq.Expressions;
    using mooSQL.linq.ext;
    using Reflection;

	[BuildsMethodCall(nameof(LinqExtensions.InlineParameters))]
	sealed class InlineParametersBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsSameGenericMethod(Methods.LinqToDB.InlineParameters);

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{


			var sequence = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));



			return sequence;
		}
	}
}
