using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace mooSQL.data.model
{
	using Common;


	/// <summary>
	/// 持有SQL 、SQL类型 
	/// </summary>
	[DebuggerDisplay("SQL = {" + nameof(DebugSqlText) + "}")]
	public abstract class BaseSentence :Clause, ISQLNode
	{
        protected BaseSentence(ClauseType nodeType, Type type) : base(nodeType, type)
        {
        }

        public string SqlText => this.ToDebugString(SelectQuery);

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		protected string DebugSqlText =>SqlText;

		public abstract QueryType QueryType { get; }

		public abstract bool IsParameterDependent { get; set; }

		/// <summary>
		/// Used internally for SQL Builder
		/// </summary>
		public BaseSentence? ParentStatement { get; set; }



		public abstract SelectQueryClause? SelectQuery { get; set; }

		public CommentWord?              Tag                { get; set; }
		public List<QueryExtension>? SqlQueryExtensions { get; set; }

		#region IQueryElement

#if DEBUG
		public virtual string DebugText => this.ToString();
#endif

		public abstract ClauseType       NodeType { get; }
		public abstract IElementWriter ToString(IElementWriter writer);

		#endregion

		public ITableNode? GetTableSrc(ITableNode table, out bool noAlias) {
			noAlias = false;
			return null;
		}

		/// <summary>
		/// Indicates when optimizer can not remove reference for particular table
		/// </summary>
		/// <param name="table"></param>
		/// <returns></returns>
		public virtual bool IsDependedOn(ITableNode table)
		{
			return false;
		}

#if OVERRIDETOSTRING
		public override string ToString()
		{
			return this.ToDebugString(SelectQuery);
		}
#endif

    }
}
