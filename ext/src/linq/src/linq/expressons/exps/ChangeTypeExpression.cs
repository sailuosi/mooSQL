using System;
using System.Linq.Expressions;

namespace mooSQL.linq.expressions
{
	/// <summary>
	/// ChangeTypeExpression type.
	/// </summary>
	/// <summary>
	/// ChangeTypeExpression type.
	/// </summary>
	public sealed class ChangeTypeExpression : Expression
	{
		/// <summary>
		/// ChangeTypeType
		/// </summary>
		/// <summary>
		/// ChangeTypeType
		/// </summary>
		public const ExpressionType ChangeTypeType = (ExpressionType)1000;
		/// <summary>
		/// ChangeTypeExpression type.
		/// </summary>

		public ChangeTypeExpression(Expression expression, Type type)
		{
			Expression = expression;
			_type       = type;
		}

		readonly Type _type;
/// <summary>
/// Type
/// </summary>
	

		public override Type           Type     { get { return _type;          } }
	/// <summary>
	/// NodeType
	/// </summary>
	
		public override ExpressionType NodeType { get { return ChangeTypeType; } }
		/// <summary>
		/// Gets or sets Expression.
		/// </summary>

		public Expression Expression { get; private set; }
/// <summary>
/// ToString
/// </summary>
	

		public override string ToString() => $"(({Type}){Expression})";

		bool Equals(ChangeTypeExpression other)
		{
			return _type == other._type && Expression.Equals(other.Expression);
		}
		/// <summary>
		/// Applies pending changes for the edit table.
		/// </summary>

		public ChangeTypeExpression Update(Expression expression)
		{
			if (ReferenceEquals(Expression, expression))
				return this;

			return new ChangeTypeExpression(expression, _type);
		}
/// <summary>
/// Equals
/// </summary>
	

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals((ChangeTypeExpression)obj);
		}
/// <summary>
/// Gets GetHashCode.
/// </summary>
	

		public override int GetHashCode()
		{
			unchecked
			{
				return (_type.GetHashCode() * 397) ^ Expression.GetHashCode();
			}
		}
/// <summary>
/// Accept
/// </summary>
	

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitChangeTypeExpression(this);
			return base.Accept(visitor);
		}
	}
}
