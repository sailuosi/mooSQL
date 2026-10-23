using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace mooSQL.linq.expressions
{
	/// <summary>
	/// ExpressionVisitorBase type.
	/// </summary>
	/// <summary>
	/// ExpressionVisitorBase type.
	/// </summary>
	public class ExpressionVisitorBase : ExpressionVisitor
	{
		/// <summary>
		/// Visit
		/// </summary>
	
		[DebuggerStepThrough]
		[return: NotNullIfNotNull(nameof(node))]
		public override Expression? Visit(Expression? node)
		{
			return base.Visit(node);
		}
		/// <summary>
		/// VisitSqlPlaceholderExpression
		/// </summary>

		public virtual Expression VisitSqlPlaceholderExpression(SqlPlaceholderExpression node)
		{
			return node;
		}
		/// <summary>
		/// VisitChangeTypeExpression
		/// </summary>

		public virtual Expression VisitChangeTypeExpression(ChangeTypeExpression node)
		{
			return node.Update(Visit(node.Expression)!);
		}

		internal virtual Expression VisitContextRefExpression(ContextRefExpression node)
		{
			return node;
		}

		internal virtual Expression VisitStatementExpression(StatementExpression node)
		{
			return node;
		}
		/// <summary>
		/// VisitDefaultValueExpression
		/// </summary>

		public virtual Expression VisitDefaultValueExpression(DefaultValueExpression node)
		{
			return node;
		}

		internal virtual Expression VisitSqlAdjustTypeExpression(SqlAdjustTypeExpression node)
		{
			return node.Update(Visit(node.Expression)!);
		}

		internal virtual Expression VisitSqlEagerLoadExpression(SqlEagerLoadExpression node)
		{
			return node.Update(Visit(node.SequenceExpression), Visit(node.Predicate));
		}

		internal virtual Expression VisitSqlErrorExpression(SqlErrorExpression node)
		{
			return node;
		}

		internal virtual SqlGenericConstructorExpression.Assignment VisitSqlGenericAssignment(
			SqlGenericConstructorExpression.Assignment assignment)
		{
			return assignment.WithExpression(Visit(assignment.Expression));
		}

		internal virtual SqlGenericConstructorExpression.Parameter VisitSqlGenericParameter(
			SqlGenericConstructorExpression.Parameter parameter)
		{
			return parameter.WithExpression(Visit(parameter.Expression));
		}
		/// <summary>
		/// VisitSqlGenericConstructorExpression
		/// </summary>

		public virtual Expression VisitSqlGenericConstructorExpression(SqlGenericConstructorExpression node)
		{
			var assignments = Visit(node.Assignments, VisitSqlGenericAssignment);

			if (!ReferenceEquals(assignments, node.Assignments))
			{
				node = node.ReplaceAssignments(assignments.ToList());
			}

			var parameters = Visit(node.Parameters, VisitSqlGenericParameter);

			if (!ReferenceEquals(parameters, node.Parameters))
			{
				node = node.ReplaceParameters(parameters.ToList());
			}

			return node;
		}

		internal virtual Expression VisitSqlGenericParamAccessExpression(SqlGenericParamAccessExpression node)
		{
			return node;
		}

		internal virtual Expression VisitSqlReaderIsNullExpression(SqlReaderIsNullExpression node)
		{
			return node.Update((SqlPlaceholderExpression)Visit(node.Placeholder)!);
		}

		internal virtual Expression VisitSqlPathExpression(SqlPathExpression node)
		{
			return node;
		}
		/// <summary>
		/// Cleanup
		/// </summary>

		public virtual void Cleanup()
		{
		}
		/// <summary>
		/// VisitPlaceholderExpression
		/// </summary>

		public virtual Expression VisitPlaceholderExpression(PlaceholderExpression node)
		{
			return node.Update(Visit(node.InnerExpression));
		}
		/// <summary>
		/// VisitSqlDefaultIfEmptyExpression
		/// </summary>

		public virtual Expression VisitSqlDefaultIfEmptyExpression(SqlDefaultIfEmptyExpression node)
		{
			return node.Update(Visit(node.InnerExpression), VisitAndConvert(node.NotNullExpressions, nameof(VisitSqlDefaultIfEmptyExpression)));
		}
		/// <summary>
		/// VisitSqlQueryRootExpression
		/// </summary>

		public virtual Expression VisitSqlQueryRootExpression(SqlQueryRootExpression node)
		{
			return node;
		}
	}
}
