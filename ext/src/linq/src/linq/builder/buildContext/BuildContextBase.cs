using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
    using mooSQL.data;
    using mooSQL.data.model;
    using mooSQL.linq.Mapping;
	using SqlQuery;

	[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
	abstract class BuildContextBase : IBuildContext
	{

#if DEBUG
		public string SqlQueryText => SelectQuery?.SqlText ?? "";
		public string Path         => this.GetPath();
		public int    ContextId    { get; }
#endif

		protected BuildContextBase(ExpressionBuilder builder, Type elementType, SelectQueryClause selectQuery)
		{
			Builder     = builder;
			ElementType = elementType;
			SelectQuery = selectQuery;
#if DEBUG
			ContextId = builder.GenerateContextId();
#endif
		}

		public          ExpressionBuilder Builder       { get; }


        public  DBInstance DB { get; set; }
        public virtual  Expression?       Expression    => null;
		public          SelectQueryClause       SelectQuery   { get; protected set; }
		public          IBuildContext?    Parent        { get; set; }

		public virtual  Type       ElementType { get; }
		public abstract Expression MakeExpression(Expression path, ProjectFlags flags);

		public abstract void SetRunQuery<T>(SentenceBag<T> query, Expression expr);

		public abstract IBuildContext Clone(CloningContext context);

		public abstract BaseSentence GetResultStatement();

		public virtual void SetAlias(string? alias)
		{
		}

		public virtual IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
		{
			return this;
		}

		public virtual bool IsOptional => false;

		#region Obsolete

		public virtual void CompleteColumns()
		{
		}

		#endregion Obsolete

	}
}
