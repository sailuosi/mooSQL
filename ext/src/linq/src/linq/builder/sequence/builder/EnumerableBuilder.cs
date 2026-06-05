using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace mooSQL.linq.Linq.Builder
{
	using Extensions;
	using Reflection;
	using mooSQL.linq.Expressions;
    using mooSQL.utils;

    [BuildsExpression(ExpressionType.Constant, ExpressionType.MemberAccess, ExpressionType.NewArrayInit)]
	sealed class EnumerableBuilder : ISequenceBuilder
	{
		public static bool CanBuild(Expression expr, BuildInfo info, ClauseSqlTranslator builder)
		{
			if (expr.NodeType == ExpressionType.NewArrayInit)
				return true;

			// IQueryable（含 EntityQueryable）由 TableBuilder / EntityBusBuilder 处理，不是内存集合。
			if (typeof(IQueryable).IsAssignableFrom(expr.Type))
				return false;

			if (!typeof(IEnumerable<>).IsSameOrParentOf(expr.Type))
				return false;

			if (typeof(IEnumerable<>).GetGenericType(expr.Type) is null)
				return false;

			return expr.NodeType switch
			{
				ExpressionType.MemberAccess => CanBuildMemberChain(((MemberExpression)expr).Expression),
				ExpressionType.Constant => ((ConstantExpression)expr).Value is IEnumerable,
				_ => false,
			};

			static bool CanBuildMemberChain(Expression? expr)
			{
				while (expr is { NodeType: ExpressionType.MemberAccess })
					expr = ((MemberExpression)expr).Expression;
				
				return expr is null or { NodeType: ExpressionType.Constant };
			}
		}

		public BuildSequenceResult BuildSequence(ClauseSqlTranslator builder, BuildInfo buildInfo)
		{
			var collectionType = typeof(IEnumerable<>).GetGenericType(buildInfo.Expression.Type) ??
			                     throw new InvalidOperationException();

			var enumerableContext = new EnumerableContext(builder, buildInfo, buildInfo.SelectQuery, collectionType.GetGenericArguments()[0]);

			return BuildSequenceResult.FromContext(enumerableContext);
		}

		public bool IsSequence(ClauseSqlTranslator builder, BuildInfo buildInfo)
		{
			return true;
		}
	}
}
