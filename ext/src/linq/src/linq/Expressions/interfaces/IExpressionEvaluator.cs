using System.Linq.Expressions;

namespace mooSQL.linq.expressions
{
	public interface IExpressionEvaluator
	{
		bool    CanBeEvaluated(Expression expression);
		object? Evaluate(Expression       expression);
	}
}
