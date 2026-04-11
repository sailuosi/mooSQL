using System;
using System.Collections.Generic;
using System.Linq;

namespace mooSQL.data.model
{
	/// <summary>
	/// from部分词组，持有Tables
	/// </summary>
	public class FromClause : ClauseBase
	{

        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitFromClause(this);
        }
        /// <summary>FROM 列表中出现的表节点（含派生/连接包装）。</summary>
        public List<ITableNode> Tables { get; } = new List<ITableNode>();

		/// <summary>维护 JOIN 图与追加顺序的容器。</summary>
		public BoxTable focus;



        #region Join的快捷方法

        /// <summary>以自动 JOIN 方式添加表并返回派生节点。</summary>
        public ITableNode Add( ITableNode table, string? alias)
        {
            var t= focus.Join(JoinKind.Auto, table, alias, null);
            Tables.Add(t);
            return t;
        }

        /// <summary>追加显式 JOIN。</summary>
        public FromClause Join(JoinKind joinType, ITableNode table, string? alias, JoinOnWord joinOn)
        {
            var t= focus.Join(joinType, table,alias, joinOn);
            Tables.Add(t);
            return this;
        }


        /// <summary>内连接。</summary>
        public FromClause InnerJoin(ITableNode table, string? alias, JoinOnWord joinOn)
        {
            return Join(JoinKind.Inner, table, alias, joinOn);
        }

        /// <summary>左外连接。</summary>
        public FromClause LeftJoin(ITableNode table, string? alias, JoinOnWord joinOn)
        {
            return Join(JoinKind.Left, table, alias, joinOn);
        }



        /// <summary>交叉 APPLY。</summary>
        public FromClause CrossApply(ITableNode table, string? alias, JoinOnWord joinOn)
        {
            return Join(JoinKind.CrossApply, table, alias, joinOn);
        }



        /// <summary>外部 APPLY。</summary>
        public FromClause OuterApply(ITableNode table, string? alias, JoinOnWord joinOn)
        {
            return Join(JoinKind.OuterApply, table, alias, joinOn);
        }


        /// <summary>弱语义内连接（优化提示）。</summary>
        public FromClause WeakInnerJoin(ITableNode table, string? alias, JoinOnWord joinOn)
        {
            return Join(JoinKind.Inner, table, alias, joinOn);
        }




        /// <summary>弱语义左连接。</summary>
        public FromClause WeakLeftJoin(ITableNode table, string? alias, JoinOnWord joinOn)
        {
            return Join(JoinKind.Left, table, alias, joinOn);
        }



        /// <summary>右外连接。</summary>
        public FromClause RightJoin(ITableNode table, string? alias, JoinOnWord joinOn)
        {
            return Join(JoinKind.Right, table, alias, joinOn);
        }



        /// <summary>全外连接。</summary>
        public FromClause   FullJoin(ITableNode table, string? alias, JoinOnWord joinOn)
        {
            return Join(JoinKind.Full, table, alias, joinOn);
        }


        #endregion

        /// <summary>绑定到所属 SELECT 查询体。</summary>
        public FromClause(SelectQueryClause selectQuery, Type type = null) : base(selectQuery, ClauseType.FromClause, type)
        {
            this.focus = new BoxTable();
		}



        ITableNode? GetTable(ITableNode table, string? alias)
		{
			foreach (var ts in Tables)
				if (ts == table)
					if (alias == null || ts.Name == alias)
						return ts;
					else
						throw new ArgumentException($"Invalid alias: '{ts.Name}' != '{alias}'");

			return null;
		}

        /// <summary>若已存在则返回，否则添加。</summary>
        public ITableNode AddOrGetTable(ITableNode table, string? alias)
		{
			var ts = GetTable(table, alias);

			if (ts != null)
				return ts;


			var t = Add(table, alias);
			return t;
		}


		#region QueryElement Members

		/// <inheritdoc />
		public override ClauseType NodeType => ClauseType.FromClause;

		/// <inheritdoc />
		public IElementWriter ToString(IElementWriter writer)
		{
			writer
				.Append("FROM ");

			if (Tables.Count > 0)
			{
				using (writer.IndentScope())
				{
					for (var index = 0; index < Tables.Count; index++)
					{
						var ts = Tables[index];
						writer.AppendElement(ts);

						if (index < Tables.Count - 1)
							writer.AppendLine(",");
					}
				}
			}

			return writer;
		}

		#endregion

		/// <summary>清空表列表并重置连接图。</summary>
		public void Cleanup()
		{
			Tables.Clear();
		}


	}
}
