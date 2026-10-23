using System.Linq.Expressions;

namespace mooSQL.linq.expressions
{
	/// <summary>
	/// IExpressionEvaluator type.
	/// </summary>
	/// <summary>
	/// IExpressionEvaluator interface.
	/// </summary>
	public interface IExpressionEvaluator
	{
		/// <summary>
		/// CanBeEvaluated
		/// </summary>
		/// <summary>
		/// CanBeEvaluated
		/// </summary>
		bool    CanBeEvaluated(Expression expression);
		/// <summary>
		/// Evaluate
		/// </summary>
		/// <summary>
		/// Evaluate
		/// </summary>
		object? Evaluate(Expression       expression);
	}
}
