using System.Linq.Expressions;

namespace mooSQL.linq.expressions
{
	public interface ICustomMapper
	{
		bool CanMap(Expression expression);
		Expression Map(Expression expression);
	}
}
