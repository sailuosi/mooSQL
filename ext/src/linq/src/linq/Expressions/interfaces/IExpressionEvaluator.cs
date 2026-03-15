using System.Linq.Expressions;

namespace mooSQL.linq.Expressions
{
	public interface IExpressionEvaluator
	{
		bool    CanBeEvaluated(Expression expression);
		object? Evaluate(Expression       expression);
	}
}
