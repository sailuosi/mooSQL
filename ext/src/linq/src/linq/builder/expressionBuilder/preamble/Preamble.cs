using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace mooSQL.linq.Linq
{
	/// <summary>
	/// 前导码
	/// </summary>
	abstract class Preamble
	{
        //IDataContext dataContext, Expression expression, object?[]? parameters, object[]? preambles
        public abstract object Execute(RunnerContext context);
		public abstract Task<object> ExecuteAsync(RunnerContext context);
	}
}
