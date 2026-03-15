using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using Common.Internal;
	using Extensions;
	using mooSQL.linq.Expressions;
	using Mapping;
	using SqlQuery;
    using mooSQL.data;

    partial class ExpressionBuilder
	{
		static ObjectPool<BuildVisitor> _buildVisitorPool = new(() => new BuildVisitor(), v => v.Cleanup(), 100);

		static ObjectPool<FinalizeExpressionVisitor> _finalizeVisitorPool = new(() => new FinalizeExpressionVisitor(), v => v.Cleanup(), 100);




		public Expression BuildSqlExpression(IBuildContext context, Expression expression, ProjectFlags flags, string? alias = null, BuildFlags buildFlags = BuildFlags.None)
		{
			using var visitor =  _buildVisitorPool.Allocate();

			var result = visitor.Value.Build(context, expression, flags, buildFlags);
			return result;
		}

		public Expression ExtractProjection(IBuildContext context, Expression expression)
		{
			var projectVisitor = new ProjectionVisitor(context);
			var projected      = projectVisitor.Visit(expression);

			return projected;
		}

		bool _handlingAlias;

		Expression CheckForAlias(IBuildContext context, MemberExpression memberExpression, EntityInfo entityDescriptor, string alias, ProjectFlags flags)
		{
			if (_handlingAlias)
				return memberExpression;

			var otherProp = entityDescriptor.Type.GetField(alias);

			if (otherProp == null)
				return memberExpression;

			var newPath     = Expression.MakeMemberAccess(memberExpression.Expression, otherProp);

			_handlingAlias = true;
			var aliasResult = MakeExpression(context, newPath, flags);
			_handlingAlias = false;

			if (aliasResult is not SqlErrorExpression && aliasResult is not DefaultValueExpression)
			{
				return aliasResult;
			}

			return memberExpression;
		}

		public bool HandleAlias(IBuildContext context, Expression expression, ProjectFlags flags, [NotNullWhen(true)] out Expression? result)
		{
			result = null;

			if (expression is not MemberExpression memberExpression)
				return false;

			var ed = DBLive.client.EntityCash.getEntityInfo(memberExpression.Expression!.Type);

			if (ed.Alias == null)
				return false;

			var testedColumn = ed.Columns.FirstOrDefault(c =>
				MemberInfoComparer.Instance.Equals(c.PropertyInfo, memberExpression.Member));

			if (testedColumn != null)
			{
				//var otherColumns = ed.Aliases.Where(a =>
				//	a.Value == testedColumn.MemberName);

				//foreach (var other in otherColumns)
				//{
				//	var newResult = CheckForAlias(context, memberExpression, ed, other.Key, flags);
				//	if (!ReferenceEquals(newResult, memberExpression))
				//	{
				//		result = newResult;
				//		return true;
				//	}
				//}
			}
			else
			{
				//if (ed.Aliases.TryGetValue(memberExpression.Member.Name, out var alias))
				//{
				//	var newResult = CheckForAlias(context, memberExpression, ed, alias, flags);
				//	if (!ReferenceEquals(newResult, memberExpression))
				//	{
				//		result = newResult;
				//		return true;
				//	}
				//}
			}

			return false;
		}
	}
}
