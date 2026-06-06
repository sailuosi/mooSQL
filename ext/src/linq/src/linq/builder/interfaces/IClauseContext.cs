using System;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
	using Mapping;
    using mooSQL.data.model;
    using SqlQuery;
	/// <summary>
	/// 表达式编译环境
	/// </summary>
	internal interface IClauseContext
	{
#if DEBUG
		string? SqlQueryText  { get; }
		string  Path          { get; }
		int     ContextId     { get; }
#endif

		ClauseSqlTranslator Builder       { get; }

		/// <summary>
		/// 输入物
		/// </summary>
		Expression?       Expression    { get; }
		/// <summary>
		/// 输出物
		/// </summary>
		SelectQueryClause       SelectQuery   { get; }
		/// <summary>
		/// 父级
		/// </summary>
		IClauseContext?    Parent        { get; set; } // TODO: probably not needed

		Type ElementType { get; }

		Expression    BuildProjection(Expression path, ProjectFlags flags);
		/// <summary>
		/// Optional cardinality for associations
		/// </summary>
		bool          IsOptional { get; }
		IClauseContext Clone(CloningContext      context);

		IClauseContext? GetContext(Expression     expression, BuildInfo    buildInfo);
		void           SetAlias(string?          alias);
		BaseSentence   GetResultStatement();
		void           CompleteColumns();
	}
}
