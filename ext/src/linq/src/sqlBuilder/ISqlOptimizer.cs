using System;
using System.Diagnostics.CodeAnalysis;

namespace mooSQL.linq.provider
{
	using mooSQL.linq.mapping;
    using mooSQL.data;
    using mooSQL.data.model;
    using mooSQL.linq.clause;
	public interface ISqlOptimizer
	{
		/// <summary>
		/// 结束查询
		/// </summary>
		/// <returns>Query which is ready for optimization.</returns>
		BaseSentence Finalize(DBInstance mappingSchema, BaseSentence statement);



		SqlExpressionConvertVisitor   CreateConvertVisitor(bool   allowModify);
	}
}
