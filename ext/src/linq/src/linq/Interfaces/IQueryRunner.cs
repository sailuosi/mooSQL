using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace mooSQL.linq.Linq
{
	using Data;
    using mooSQL.data;

    public interface IQueryRunner: IDisposable, IAsyncDisposable
	{


		Expression     Expression       { get; }

		DBInstance     DBLive              { get; }
		object?[]?     Parameters       { get; }
		object?[]?     Preambles        { get; }
		Expression?    MapperExpression { get; set; }
		int            RowsCount        { get; set; }
		int            QueryNumber      { get; set; }
	}
}
