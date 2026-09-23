using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq.builder
{
	using mooSQL.linq.utils;
	using mooSQL.linq.expressions;
	using mooSQL.linq.mapping;
	using mooSQL.linq.clause;
	using System.Linq.Expressions;

	class LambdaResolveVisitor : ExpressionVisitorBase
	{
		readonly IClauseContext _context;
		bool _inLambda;

		public ClauseSqlTranslator Builder => _context.Builder;

		public LambdaResolveVisitor(IClauseContext context)
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
