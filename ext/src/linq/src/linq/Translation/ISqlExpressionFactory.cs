using System;
using mooSQL.data;
using mooSQL.data.model;
using mooSQL.linq.Common;
using mooSQL.linq.SqlQuery;

namespace mooSQL.linq.Linq.Translation
{
	// Empty, but should be extended
	public interface ISqlExpressionFactory
	{

		DBInstance DBLive {  get; }
		DbDataType  GetDbDataType(IExpWord expression);
		DbDataType  GetDbDataType(Type           type);
	}
}
