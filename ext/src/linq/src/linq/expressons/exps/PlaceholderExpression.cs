using System;
using System.Linq.Expressions;

namespace mooSQL.linq.expressions
{
	/// <summary>
	/// PlaceholderExpression type.
	/// </summary>
	/// <summary>
	/// PlaceholderExpression enum.
	/// </summary>
	public enum PlaceholderType
	{
		/// <summary>
		/// Closure
		/// </summary>
		/// <summary>
		/// Closure
		/// </summary>
		Closure,
		/// <summary>
		/// Converted
		/// </summary>
		/// <summary>
		/// Converted
		/// </summary>
		Converted
	}
	/// <summary>
	/// PlaceholderExpression type.
	/// </summary>

	public class PlaceholderExpression : Expression
	{
		/// <summary>
		/// PlaceholderType
		/// </summary>
		/// <summary>
		/// Gets or sets PlaceholderType.
		/// </summary>
		public PlaceholderType PlaceholderType   { get; }
		/// <summary>
		/// InnerExpression
		/// </summary>
		/// <summary>
		/// Gets or sets InnerExpression.
		/// </summary>
		public Expression      InnerExpression { get; }
		/// <summary>
		/// PlaceholderExpression type.
		/// </summary>

		public PlaceholderExpression(Expression innerExpression, PlaceholderType placeholderType)
		{
			PlaceholderType = placeholderType;
			InnerExpression = innerExpression;
		}
		/// <summary>
		/// Closure
		/// </summary>

		public static PlaceholderExpression Closure(Expression innerExpression) => new(innerExpression, PlaceholderType.Closure);
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
	

		public override Expression     Reduce() => InnerExpression;
		/// <summary>
		/// Applies pending changes for the edit table.
		/// </summary>

		public PlaceholderExpression Update(Expression closureExpression)
		{
			if (ReferenceEquals(InnerExpression, closureExpression))
				return this;
			return new PlaceholderExpression(closureExpression, PlaceholderType);
		}
/// <summary>
/// Accept
/// </summary>
	

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitPlaceholderExpression(this);
			return base.Accept(visitor);
		}
/// <summary>
/// ToString
/// </summary>
	

		public override string ToString()
		{
			return $"$({InnerExpression})";
		}
	}
}
