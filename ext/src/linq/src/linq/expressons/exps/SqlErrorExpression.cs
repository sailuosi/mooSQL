using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace mooSQL.linq.expressions
{
	using mooSQL.linq.utils;
	using mooSQL.linq;
	/// <summary>
	/// SqlErrorExpression type.
	/// </summary>
	/// <summary>
	/// SqlErrorExpression type.
	/// </summary>
	public class SqlErrorExpression : Expression
	{
		/// <summary>
		/// SqlErrorExpression type.
		/// </summary>
		/// <summary>
		/// SqlErrorExpression type.
		/// </summary>
		public SqlErrorExpression(object? buildContext, Expression? expression, string? message, Type resultType, bool isCritical)
		{
			BuildContext = buildContext;
			Expression   = expression;
			Message      = message;
			ResultType   = resultType;
			IsCritical   = isCritical;
		}
		/// <summary>
		/// SqlErrorExpression type.
		/// </summary>

		public SqlErrorExpression(object? buildContext, Expression expression) : this(buildContext, expression, null, expression.Type, false)
		{
		}
		/// <summary>
		/// SqlErrorExpression type.
		/// </summary>

		public SqlErrorExpression(string message, Type resultType) : this(null, null, message, resultType, false)
		{
		}
		/// <summary>
		/// SqlErrorExpression type.
		/// </summary>

		public SqlErrorExpression(Expression? expression, string? message, Type resultType) : this(null, expression, message, resultType, false)
		{
		}
		/// <summary>
		/// Gets or sets BuildContext.
		/// </summary>

		public object?     BuildContext { get; }
		/// <summary>
		/// Expression
		/// </summary>
		/// <summary>
		/// Gets or sets Expression.
		/// </summary>
		public Expression? Expression   { get; }
		/// <summary>
		/// ResultType
		/// </summary>
		/// <summary>
		/// Gets or sets ResultType.
		/// </summary>
		public Type        ResultType   { get; }
		/// <summary>
		/// Message
		/// </summary>
		/// <summary>
		/// Gets or sets Message.
		/// </summary>
		public string?     Message      { get; }
		/// <summary>
		/// IsCritical
		/// </summary>
		/// <summary>
		/// Gets or sets IsCritical.
		/// </summary>
		public bool        IsCritical   { get; }
/// <summary>
/// NodeType
/// </summary>
	

		public override ExpressionType NodeType  => ExpressionType.Extension;
	/// <summary>
	/// Type
	/// </summary>
	
		public override Type           Type      => ResultType;
	/// <summary>
	/// CanReduce
	/// </summary>
	
		public override bool           CanReduce => true;
/// <summary>
/// Reduce
/// </summary>
	

		public override Expression Reduce()
		{
			throw CreateException(Expression, Message);
		}
		/// <summary>
		/// WithType
		/// </summary>

		public SqlErrorExpression WithType(Type type)
		{
			if (ResultType == type)
				return this;
			return new SqlErrorExpression(BuildContext, Expression, Message, type, IsCritical);
		}
		/// <summary>
		/// Creates CreateException.
		/// </summary>

		public Exception CreateException()
		{
			return CreateException(Expression, Message);
		}
		/// <summary>
		/// EnsureError
		/// </summary>

		public static SqlErrorExpression EnsureError(object? context, Expression expression)
		{
			if (expression is SqlErrorExpression error)
				return error.WithType(expression.Type);
			return new SqlErrorExpression(context, expression);
		}
		/// <summary>
		/// EnsureError
		/// </summary>

		public static SqlErrorExpression EnsureError(Expression expression, Type resultType)
		{
			if (expression is SqlErrorExpression error)
				return error.WithType(resultType);
			return new SqlErrorExpression(null, expression, null, resultType, false);
		}
		/// <summary>
		/// Creates CreateException.
		/// </summary>

		public static Exception CreateException(string message)
		{
			return new LinqException(message);
		}
		/// <summary>
		/// Creates CreateException.
		/// </summary>

		public static Exception CreateException(Expression? expression, string? message)
		{
			string messageText;

			if (expression != null)
			{
				if (expression is SqlErrorExpression sqlError)
				{
					expression = PrepareExpression(sqlError.Expression);
				}

				var expressionMessage = PrepareExpressionString(expression);
				if (expressionMessage.Contains("\n"))
				{
					messageText = $"The LINQ expression could not be converted to SQL.\nExpression:\n{expressionMessage}";
				}
				else
				{
					messageText = $"The LINQ expression '{expressionMessage}' could not be converted to SQL.";
				}

				if (message != null)
				{
					messageText += $"\nAdditional details: '{message}'";
				}
			}
			else if (message != null)
			{
				messageText = $"Translation error: '{message}'";
			}
			else
			{
				messageText = "Unknown translation error.";
			}

			return new LinqException(messageText);
		}
		/// <summary>
		/// ThrowError
		/// </summary>

		public static void ThrowError(Expression expression, string? message)
		{
			throw CreateException(expression, message);
		}
		/// <summary>
		/// PrepareExpressionString
		/// </summary>

		public static string PrepareExpressionString(Expression? expression)
		{
			var printer  = new ExpressionPrinter();
			var prepared = PrepareExpression(expression);
			var str      = prepared == null ? "null" : printer.PrintExpression(prepared);
			return str;
		}

		[return: NotNullIfNotNull(nameof(expression))]
		static Expression? PrepareExpression(Expression? expression)
		{
			Dictionary<Expression, string> usedNames = new (ExpressionEqualityComparer.Instance);

			var transformed = expression.Transform(e =>
			{
				if (e is ContextRefExpression contextRef)
				{
					return Parameter(e.Type, contextRef.Alias ?? "x");
				}

				if (e is SqlEagerLoadExpression eagerLoad)
				{
					return PrepareExpression(eagerLoad.SequenceExpression);
				}

				if (e is SqlQueryRootExpression)
				{
					if (!usedNames.TryGetValue(e, out var name))
					{
						Utils.MakeUniqueNames(new Expression[] { e }, usedNames.Values, _ => "db", (_, n, _) => name = n);
						usedNames.Add(e, name!);
					}

					return Parameter(e.Type, name);
				}

				return e;
			});

			return transformed;
		}
/// <summary>
/// ToString
/// </summary>
	

		public override string ToString()
		{
			return $"Error: {Expression}";
		}
/// <summary>
/// Accept
/// </summary>
	

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitSqlErrorExpression(this);
			return base.Accept(visitor);
		}

	}
}
