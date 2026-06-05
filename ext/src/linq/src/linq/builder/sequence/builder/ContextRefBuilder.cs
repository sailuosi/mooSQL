using System;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.linq.Expressions;

	[BuildsAny]
	sealed class ContextRefBuilder : ISequenceBuilder
	{
		public static bool CanBuild(BuildInfo buildInfo, ClauseSqlTranslator builder)
			=> buildInfo.Expression is ContextRefExpression contextRef;

		public BuildSequenceResult BuildSequence(ClauseSqlTranslator builder, BuildInfo buildInfo)
		{
			var contextRef = (ContextRefExpression)buildInfo.Expression;

			var context = contextRef.BuildContext;

			if (!buildInfo.CreateSubQuery)
				return BuildSequenceResult.FromContext(context);

			var elementContext = context.GetContext(buildInfo.Expression, buildInfo);

			if (elementContext != null)
				return BuildSequenceResult.FromContext(elementContext);

			return BuildSequenceResult.NotSupported();
		}
		
		public bool IsSequence(ClauseSqlTranslator builder, BuildInfo buildInfo)
		{
			return true;
		}
	}
}
