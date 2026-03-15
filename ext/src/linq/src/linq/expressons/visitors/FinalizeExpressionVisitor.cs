using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace mooSQL.linq.Linq.Builder
{
	using Extensions;
	using Common;
	using mooSQL.linq.Expressions;
	using Mapping;
	using SqlQuery;
	using Reflection;

	class FinalizeExpressionVisitor : ExpressionVisitorBase
	{
		HashSet<Expression>?                                                           _visited;
		HashSet<Expression>?                                                           _duplicates;
		Dictionary<Expression, Expression>?                                            _constructed;
		Dictionary<Expression, (ParameterExpression variable, Expression assignment)>? _constructedAssignments;

		ExpressionGenerator _generator = default!;
		IBuildContext       _context   = default!;
		bool                _constructRun;

		internal override Expression VisitSqlReaderIsNullExpression(SqlReaderIsNullExpression node)
		{
			return node;
		}

		internal override Expression VisitSqlEagerLoadExpression(SqlEagerLoadExpression node)
		{
			return node;
		}

		public override Expression VisitSqlGenericConstructorExpression(SqlGenericConstructorExpression node)
		{
			if (!_constructRun)
			{
				_visited ??= new(ExpressionEqualityComparer.Instance);
				if (!_visited.Add(node))
				{
					_duplicates ??= new(ExpressionEqualityComparer.Instance);
					_duplicates.Add(node);
				}
				else
				{
					var local = ConstructObject(node);
					local = TranslateExpression(local);

					// collecting recursively
					var collect = Visit(local);
				}

				return node;
			}

			_constructed ??= new(ExpressionEqualityComparer.Instance);
			if (!_constructed.TryGetValue(node, out var constructed))
			{
				constructed = ConstructObject(node);
				constructed = TranslateExpression(constructed);
				constructed = Visit(constructed);

				_constructed.Add(node, constructed);
			}

			if (_duplicates != null && _duplicates.Contains(node))
			{
				_constructedAssignments ??= new(ExpressionEqualityComparer.Instance);
				if (!_constructedAssignments.TryGetValue(node, out var assignmentPair))
				{
					var variable = _generator.AssignToVariable(Expression.Default(node.Type));
					var assign   = Expression.Assign(variable, Expression.Coalesce(variable, constructed));
					assignmentPair = (variable, assign);
					_constructedAssignments.Add(node, assignmentPair);
				}

				return assignmentPair.assignment;
			}

			return constructed;
		}

		Expression TranslateExpression(Expression local)
		{
			return _context.Builder.BuildSqlExpression(_context, local, ProjectFlags.Expression, buildFlags: BuildFlags.ForceDefaultIfEmpty);
		}

		Expression ConstructObject(SqlGenericConstructorExpression node)
		{
			return _context.Builder.Construct(_context.Builder.DBLive, node, ProjectFlags.Expression);
		}

		public Expression Finalize(Expression expression, IBuildContext context, ExpressionGenerator generator)
		{
			_visited = new HashSet<Expression>(ExpressionEqualityComparer.Instance);
			_duplicates = default;
			_constructed = default;
			_constructedAssignments = default;
			_generator = generator;
			_context = context;

			var result = expression;
			while (true)
			{
				_visited.Clear();

				_constructRun = false;
				Visit(result);

				_constructRun = true;
				var current = result;
				result = Visit(current);

				result = TranslateExpression(result);

				if (ReferenceEquals(current, result))
					break;
			}

			return result;
		}

		public override void Cleanup()
		{
			base.Cleanup();

			_visited = default!;
			_generator = default!;
			_context = default!;

			_duplicates = default;
			_constructed = default;
			_constructedAssignments = default;
		}
	}

}
