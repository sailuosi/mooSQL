using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace mooSQL.linq.expressions
{
	/// <summary>
	/// SqlDefaultIfEmptyExpression type.
	/// </summary>
	/// <summary>
	/// SqlDefaultIfEmptyExpression type.
	/// </summary>
	public class SqlDefaultIfEmptyExpression : Expression
	{
		/// <summary>
		/// InnerExpression
		/// </summary>
		/// <summary>
		/// Gets or sets InnerExpression.
		/// </summary>
		public Expression                     InnerExpression    { get; }
		/// <summary>
		/// NotNullExpressions
		/// </summary>
		/// <summary>
		/// Gets or sets NotNullExpressions.
		/// </summary>
		public ReadOnlyCollection<Expression> NotNullExpressions { get; }
		/// <summary>
		/// SqlDefaultIfEmptyExpression type.
		/// </summary>

		public SqlDefaultIfEmptyExpression(Expression innerExpression, ReadOnlyCollection<Expression> notNullExpressions)
		{
			InnerExpression    = innerExpression;
			NotNullExpressions = notNullExpressions;
		}
/// <summary>
/// NodeType
/// </summary>
	

		public override ExpressionType NodeType  => ExpressionType.Extension;
	/// <summary>
	/// Type
	/// </summary>
	
		public override Type           Type      => InnerExpression.Type;
	/// <summary>
	/// CanReduce
	/// </summary>
	
		public override bool           CanReduce => true;
/// <summary>
/// Reduce
/// </summary>
	

		public override Expression Reduce() => InnerExpression;
		/// <summary>
		/// Applies pending changes for the edit table.
		/// </summary>

		public SqlDefaultIfEmptyExpression Update(Expression innerExpression, ReadOnlyCollection<Expression> notNullExpressions)
		{
			if (ReferenceEquals(InnerExpression, innerExpression) && 
			    (ReferenceEquals(NotNullExpressions, notNullExpressions) || NotNullExpressions.SequenceEqual(notNullExpressions, ExpressionEqualityComparer.Instance)))
			{
				return this;
			}

			return new SqlDefaultIfEmptyExpression(innerExpression, notNullExpressions);
		}
/// <summary>
/// Accept
/// </summary>
	

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitSqlDefaultIfEmptyExpression(this);
			return base.Accept(visitor);
		}
/// <summary>
/// ToString
/// </summary>
	

		public override string ToString()
		{
			return $"DFT({InnerExpression})";
		}
	}
}
