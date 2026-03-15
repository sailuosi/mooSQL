using mooSQL.data;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq
{
	public interface IExpressionQuery
	{
		Expression   Expression  { get; }
		string       SqlText     { get; }
		DBInstance DBLive { get; }

	}
}
