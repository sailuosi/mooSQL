using System;
using System.Linq;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.linq;
	using mooSQL.linq.Expressions;
	using mooSQL.utils;

	/// <summary>
	/// 将 <see cref="EntityQueryable{T}"/> 根常量编译为表查询上下文。
	/// </summary>
	[BuildsExpression(ExpressionType.Constant)]
	sealed class EntityBusBuilder : ISequenceBuilder
	{
		public static bool CanBuild(Expression expr, BuildInfo info, ExpressionBuilder builder)
		{
			if (expr.NodeType != ExpressionType.Constant)
				return false;

			var type = expr.Type;
			if (!type.IsGenericType)
				return false;

			var genericDef = type.GetGenericTypeDefinition();
			return genericDef == typeof(EntityQueryable<>);
		}

		public BuildSequenceResult BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var entityType = buildInfo.Expression.Type.GetGenericArguments()[0];
			var tableContext = new TableBuilder.TableContext(builder, buildInfo, entityType);
			builder.TablesInScope?.Add(tableContext);
			return BuildSequenceResult.FromContext(tableContext);
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo) => true;
	}

}
