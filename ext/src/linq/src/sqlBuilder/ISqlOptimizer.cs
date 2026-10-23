using System;
using System.Diagnostics.CodeAnalysis;

namespace mooSQL.linq.provider
{
	using mooSQL.linq.mapping;
    using mooSQL.data;
    using mooSQL.data.model;
    using mooSQL.linq.clause;
	/// <summary>
	/// ISqlOptimizer type.
	/// </summary>
	/// <summary>
	/// ISqlOptimizer interface.
	/// </summary>
	public interface ISqlOptimizer
	{
		/// <summary>
		/// 结束查询
		/// </summary>
		/// <returns>Query which is ready for optimization.</returns>
		BaseSentence Finalize(DBInstance mappingSchema, BaseSentence statement);
		/// <summary>
		/// Creates CreateConvertVisitor.
		/// </summary>



		SqlExpressionConvertVisitor   CreateConvertVisitor(bool   allowModify);
	}
}
