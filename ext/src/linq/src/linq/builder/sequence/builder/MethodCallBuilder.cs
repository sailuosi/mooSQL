using System.Linq;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.linq.Expressions;

	abstract class MethodCallBuilder : ISequenceBuilder
	{
		/// <summary>
		/// 顺序构建
		/// </summary>
		/// <param name="builder"></param>
		/// <param name="buildInfo"></param>
		/// <returns></returns>
		public BuildSequenceResult BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
			=> BuildMethodCall(builder, (MethodCallExpression)buildInfo.Expression, buildInfo);
		/// <summary>
		/// 是否顺序
		/// </summary>
		/// <param name="builder"></param>
		/// <param name="buildInfo"></param>
		/// <returns></returns>
		public virtual bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var mc = (MethodCallExpression)buildInfo.Expression;
			return mc.IsQueryable()
				? builder.IsSequence(new BuildInfo(buildInfo, mc.Arguments[0]))
				: false;
		}
		/// <summary>
		/// 是否聚合
		/// </summary>
		/// <param name="builder"></param>
		/// <param name="buildInfo"></param>
		/// <returns></returns>
		public virtual bool IsAggregationContext(ExpressionBuilder builder, BuildInfo buildInfo) 
			=> false;
		/// <summary>
		/// 执行构建
		/// </summary>
		/// <param name="builder"></param>
		/// <param name="methodCall"></param>
		/// <param name="buildInfo"></param>
		/// <returns></returns>
		protected abstract BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo);
	}
}
