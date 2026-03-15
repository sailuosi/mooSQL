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
	internal interface IBuildContext
	{
#if DEBUG
		string? SqlQueryText  { get; }
		string  Path          { get; }
		int     ContextId     { get; }
#endif

		ExpressionBuilder Builder       { get; }

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
		IBuildContext?    Parent        { get; set; } // TODO: probably not needed

		Type ElementType { get; }

		Expression    MakeExpression(Expression path, ProjectFlags flags);
		/// <summary>
		/// Optional cardinality for associations
		/// </summary>
		bool          IsOptional { get; }
		IBuildContext Clone(CloningContext      context);

		void           SetRunQuery<T>(SentenceBag<T>   query,      Expression   expr);
		IBuildContext? GetContext(Expression     expression, BuildInfo    buildInfo);
		void           SetAlias(string?          alias);
		BaseSentence   GetResultStatement();
		void           CompleteColumns();
	}
}
