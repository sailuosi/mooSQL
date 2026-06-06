using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace mooSQL.linq.Linq.Builder
{
    using mooSQL.data;
    using mooSQL.data.model;
    using mooSQL.linq.Mapping;
	using SqlQuery;

	[DebuggerDisplay("{ClauseContextDebuggingHelper.GetContextInfo(this)}")]
	abstract class ClauseContextBase : IClauseContext
	{

#if DEBUG
		public string SqlQueryText => SelectQuery?.SqlText ?? "";
		public string Path         => this.GetPath();
		public int    ContextId    { get; }
#endif

		protected ClauseContextBase(ClauseSqlTranslator builder, Type elementType, SelectQueryClause selectQuery)
		{
			Builder     = builder;
			ElementType = elementType;
			SelectQuery = selectQuery;
#if DEBUG
			ContextId = builder.GenerateContextId();
#endif
		}

		public          ClauseSqlTranslator Builder       { get; }


        public  DBInstance DB { get; set; }
        public virtual  Expression?       Expression    => null;
		public          SelectQueryClause       SelectQuery   { get; protected set; }
		public          IClauseContext?    Parent        { get; set; }

		public virtual  Type       ElementType { get; }
		public abstract Expression BuildProjection(Expression path, ProjectFlags flags);

		public abstract IClauseContext Clone(CloningContext context);

		public abstract BaseSentence GetResultStatement();

		public virtual void SetAlias(string? alias)
		{
		}

		public virtual IClauseContext? GetContext(Expression expression, BuildInfo buildInfo)
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
