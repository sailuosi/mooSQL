using System.Linq.Expressions;

namespace mooSQL.linq.Expressions
{
	public interface ICustomMapper
	{
		bool CanMap(Expression expression);
		Expression Map(Expression expression);
	}
}
