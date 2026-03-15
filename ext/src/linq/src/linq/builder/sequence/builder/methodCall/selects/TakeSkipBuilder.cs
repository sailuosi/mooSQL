using System;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
    using mooSQL.data;
    using mooSQL.data.model;
	using mooSQL.linq.Expressions;
	using SqlQuery;

	[BuildsMethodCall("Skip", "Take")]
	sealed class TakeSkipBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable();

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			if (buildResult.BuildContext == null)
				return buildResult;

			var sequence = buildResult.BuildContext;

			var arg      = methodCall.Arguments[1].Unwrap();

            IExpWord expr;

			var linqOptions = builder.DBLive.dialect.Option;
			var parameterize = !buildInfo.IsSubQuery && linqOptions.ParameterizeTakeSkip;

			if (arg.NodeType == ExpressionType.Lambda)
			{
				arg  = ((LambdaExpression)arg).Body.Unwrap();
				expr = builder.ConvertToSql(sequence, arg);
			}
			else
			{
				// revert unwrap
				arg  = methodCall.Arguments[1];
				expr = builder.ConvertToSql(sequence, arg);

				if (expr.NodeType == ClauseType.SqlValue && builder.CanBeCompiled(methodCall.Arguments[1], false))
				{
					var param = builder.ParametersContext.BuildParameter(sequence, methodCall.Arguments[1], null, forceConstant : true, forceNew : true)!.SqlParameter;
					param.Name             = methodCall.Method.Name == "Take" ? "take" : "skip";
					param.IsQueryParameter = param.IsQueryParameter && parameterize;
					expr                   = param;
				}
			}

			if (methodCall.Method.Name == "Take")
			{
				TakeHintType? hints = null;
				if (methodCall.Arguments.Count == 3 && methodCall.Arguments[2].Type == typeof(TakeHintType))
					hints = (TakeHintType)builder.EvaluateExpression(methodCall.Arguments[2])!;

				builder.BuildTake(sequence, expr, hints);
			}
			else
			{
				builder.BuildSkip(sequence, expr);
			}

			return BuildSequenceResult.FromContext(new TakeSkipContext(sequence));
		}

		class TakeSkipContext : PassThroughContext
		{
			public TakeSkipContext(IBuildContext context) : base(context)
			{
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				return base.MakeExpression(path, flags);
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new TakeSkipContext(context.CloneContext(Context));
			}
		}
	}
}
