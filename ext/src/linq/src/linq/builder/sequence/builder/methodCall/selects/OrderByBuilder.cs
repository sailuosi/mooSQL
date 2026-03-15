using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using SqlQuery;
	using mooSQL.linq.Expressions;
	using mooSQL.data.model;

	[BuildsMethodCall("OrderBy", "OrderByDescending", "ThenBy", "ThenByDescending", "ThenOrBy", "ThenOrByDescending")]
	sealed class OrderByBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
		{
			if (!call.IsQueryable())
				return false;

			var body = call.Arguments[1].UnwrapLambda().Body.Unwrap();
			if (body.NodeType == ExpressionType.MemberInit)
			{
				var mi = (MemberInitExpression)body;
				if (mi.NewExpression.Arguments.Count > 0 || 
					mi.Bindings.Count == 0 ||
					mi.Bindings.Any(b => b.BindingType != MemberBindingType.Assignment))
				{
					throw new NotSupportedException($"Explicit construction of entity type '{body.Type}' in order by is not allowed.");
				}
			}

			return true;
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			//排序参数---方法信息
			var sequenceArgument = methodCall.Arguments[0];

			var sequenceResult   = builder.TryBuildSequence(new BuildInfo(buildInfo, sequenceArgument));

			if (sequenceResult.BuildContext == null)
				return sequenceResult;

			var sequence = sequenceResult.BuildContext;

			var wrapped = false;

			if (sequence.SelectQuery.Select.HasModifier)
			{
				sequence = new SubQueryContext(sequence);
				wrapped = true;
			}

			var orderByProjectFlags = ProjectFlags.SQL | ProjectFlags.Keys;
			//连续order
			var isContinuousOrder   = !sequence.SelectQuery.OrderBy.IsEmpty && methodCall.Method.Name.StartsWith("Then");
			var lambda              = (LambdaExpression)methodCall.Arguments[1].Unwrap();

			var byIndex = false;

			List<SqlPlaceholderExpression> placeholders;
			while (true)
			{
				Expression sqlExpr;
				//表达式体
				var body = SequenceHelper.PrepareBody(lambda, sequence).Unwrap();
				//内建扩展支持
				if (body is MethodCallExpression mc && mc.Method.DeclaringType == typeof(Sql) && mc.Method.Name == nameof(Sql.Ordinal))
				{
					sqlExpr = builder.ConvertToSqlExpr(sequence, mc.Arguments[0], orderByProjectFlags);
					byIndex = true;
				}
				else
				{
					sqlExpr = builder.ConvertToSqlExpr(sequence, body, orderByProjectFlags);
					byIndex = false;
				}

				if (!SequenceHelper.IsSqlReady(sqlExpr))
				{
					if (sqlExpr is SqlErrorExpression errorExpr)
						return BuildSequenceResult.Error(methodCall, errorExpr.Message);
					return BuildSequenceResult.Error(methodCall);
				}

				placeholders = ExpressionBuilder.CollectDistinctPlaceholders(sqlExpr);

				// ThenByExtensions 不创建子查询
				//
				if (wrapped || isContinuousOrder)
					break;

				// 处理 order by 复杂属性的场景
				//
				var isComplex = false;

				foreach (var placeholder in placeholders)
				{
					// 不可变表达式稍后将被删除
					//
					var isImmutable = QueryHelper.IsConstant(placeholder.Sql);
					if (isImmutable)
						continue;

					// 可能我们需要延长这个列表
					//
					isComplex = null != placeholder.Sql.Find(e => e.NodeType == ClauseType.SqlQuery || e.NodeType == ClauseType.SqlFunction);
					if (isComplex)
						break;
				}

				if (!isComplex)
					break;

				sequence = new SubQueryContext(sequence);
				wrapped = true;
			}

			if (!isContinuousOrder && !builder.DBLive.dialect.Option.DoNotClearOrderBys)
				sequence.SelectQuery.OrderBy.Items.Clear();

			foreach (var placeholder in placeholders)
			{
				var orderSql = placeholder.Sql;

				var isPositioned = byIndex;

				//这里创建了orderByClause词组
				sequence.SelectQuery.OrderBy.Expr(orderSql, methodCall.Method.Name.EndsWith("Descending"), isPositioned);
			}

			return BuildSequenceResult.FromContext(sequence);
		}
	}
}
