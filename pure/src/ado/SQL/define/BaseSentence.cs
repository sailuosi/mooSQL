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
        /// <summary>由子类指定语句种类与 CLR 类型。</summary>
        protected BaseSentence(ClauseType nodeType, Type type) : base(nodeType, type)
        {
        }

        /// <summary>当前语句的调试/文本化 SQL。</summary>
        public string SqlText => this.ToDebugString(SelectQuery);

		/// <summary>调试器展示的 SQL 文本（同 <see cref="SqlText"/>）。</summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		protected string DebugSqlText =>SqlText;

		/// <inheritdoc />
		public abstract QueryType QueryType { get; }

		/// <summary>是否依赖运行时参数（影响缓存键等）。</summary>
		public abstract bool IsParameterDependent { get; set; }

		/// <summary>
		/// SQL Builder 内部使用的父语句引用。
		/// </summary>
		public BaseSentence? ParentStatement { get; set; }



		/// <summary>附带的 SELECT 查询体（WHERE/ORDER 等）。</summary>
		public abstract SelectQueryClause? SelectQuery { get; set; }

		/// <summary>语句前可选注释块。</summary>
		public CommentWord?              Tag                { get; set; }
		/// <summary>方言或扩展插件列表。</summary>
		public List<QueryExtension>? SqlQueryExtensions { get; set; }

		#region IQueryElement

#if DEBUG
		/// <summary>调试输出文本。</summary>
		public virtual string DebugText => this.ToString();
#endif

		/// <inheritdoc />
		public abstract ClauseType       NodeType { get; }
		/// <inheritdoc />
		public abstract IElementWriter ToString(IElementWriter writer);

		#endregion

		/// <summary>解析表来源（默认无实现）。</summary>
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
