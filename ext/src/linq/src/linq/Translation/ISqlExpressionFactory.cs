using System;
using mooSQL.data;
using mooSQL.data.model;
using mooSQL.linq.utils;
using mooSQL.linq.clause;

namespace mooSQL.linq.translator
{
	// Empty, but should be extended
	public interface ISqlExpressionFactory
	{

		DBInstance DBLive {  get; }
		DbDataType  GetDbDataType(IExpWord expression);
		DbDataType  GetDbDataType(Type           type);
	}
}
