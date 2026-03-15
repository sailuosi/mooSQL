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

	class LambdaResolveVisitor : ExpressionVisitorBase
	{
		readonly IBuildContext _context;
		bool _inLambda;

		public ExpressionBuilder Builder => _context.Builder;

		public LambdaResolveVisitor(IBuildContext context)
		{
			_context = context;
		}

		protected override Expression VisitMember(MemberExpression node)
		{
			if (_inLambda)
			{
				if (null != node.Find(1, (_, e) => e is ContextRefExpression))
				{
					var expr = Builder.BuildSqlExpression(_context, node, ProjectFlags.SQL,
						buildFlags : BuildFlags.ForceAssignments);

					if (expr is SqlPlaceholderExpression)
						return expr;
				}

				return node;
			}

			return base.VisitMember(node);
		}

		protected override Expression VisitLambda<T>(Expression<T> node)
		{
			var save = _inLambda;
			_inLambda = true;

			var newNode = base.VisitLambda(node);

			_inLambda = save;

			return newNode;
		}
	}

}
