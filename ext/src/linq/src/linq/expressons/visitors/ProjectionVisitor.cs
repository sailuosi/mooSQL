using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq.Linq.Builder
{
	using Common.Internal;
	using Extensions;
	using mooSQL.linq.Expressions;
	using Mapping;
	using SqlQuery;
	using System.Linq.Expressions;

	class ProjectionVisitor : ExpressionVisitorBase
	{
		readonly IBuildContext _context;

		public ExpressionBuilder Builder => _context.Builder;

		public ProjectionVisitor(IBuildContext context)
		{
			_context = context;
		}

		Expression ParseGenericConstructor(Expression expression)
		{
			return Builder.ParseGenericConstructor(expression, ProjectFlags.ExtractProjection, null);
		}

		protected override Expression VisitMemberInit(MemberInitExpression node)
		{
			return Visit(ParseGenericConstructor(node));
		}

		protected override Expression VisitMember(MemberExpression node)
		{
			var newNode = Builder.MakeExpression(_context, node, ProjectFlags.ExtractProjection);
			if (!ExpressionEqualityComparer.Instance.Equals(newNode, node))
				return Visit(newNode);
			return node;
		}

		protected override Expression VisitMethodCall(MethodCallExpression node)
		{
			var newNode = Builder.MakeExpression(_context, node, ProjectFlags.ExtractProjection);

			if (!ExpressionEqualityComparer.Instance.Equals(newNode, node))
				return Visit(newNode);

			var parsed = ParseGenericConstructor(node);

			if (!ReferenceEquals(parsed, node))
				return Visit(parsed);

			return base.VisitMethodCall(node);
		}

		internal override Expression VisitContextRefExpression(ContextRefExpression node)
		{
			var newNode = Builder.MakeExpression(_context, node, ProjectFlags.ExtractProjection);
			if (!ExpressionEqualityComparer.Instance.Equals(newNode, node))
				return Visit(newNode);
			return base.VisitContextRefExpression(node);
		}

		public override Expression VisitSqlGenericConstructorExpression(SqlGenericConstructorExpression node)
		{
			var newNode = base.VisitSqlGenericConstructorExpression(node);
			return newNode;
		}
	}

}
