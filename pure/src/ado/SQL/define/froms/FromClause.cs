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

        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitFromClause(this);
        }
        public List<ITableNode> Tables { get; } = new List<ITableNode>();

		public BoxTable focus;



        #region Join的快捷方法

        public ITableNode Add( ITableNode table, string? alias)
        {
            var t= focus.Join(JoinKind.Auto, table, alias, null);
            Tables.Add(t);
            return t;
        }

        public FromClause Join(JoinKind joinType, ITableNode table, string? alias, JoinOnWord joinOn)
        {
            var t= focus.Join(joinType, table,alias, joinOn);
            Tables.Add(t);
            return this;
        }


        public FromClause InnerJoin(ITableNode table, string? alias, JoinOnWord joinOn)
        {
            return Join(JoinKind.Inner, table, alias, joinOn);
        }

        public FromClause LeftJoin(ITableNode table, string? alias, JoinOnWord joinOn)
        {
            return Join(JoinKind.Left, table, alias, joinOn);
        }



        public FromClause CrossApply(ITableNode table, string? alias, JoinOnWord joinOn)
        {
            return Join(JoinKind.CrossApply, table, alias, joinOn);
        }



        public FromClause OuterApply(ITableNode table, string? alias, JoinOnWord joinOn)
        {
            return Join(JoinKind.OuterApply, table, alias, joinOn);
        }


        public FromClause WeakInnerJoin(ITableNode table, string? alias, JoinOnWord joinOn)
        {
            return Join(JoinKind.Inner, table, alias, joinOn);
        }




        public FromClause WeakLeftJoin(ITableNode table, string? alias, JoinOnWord joinOn)
        {
            return Join(JoinKind.Left, table, alias, joinOn);
        }



        public FromClause RightJoin(ITableNode table, string? alias, JoinOnWord joinOn)
        {
            return Join(JoinKind.Right, table, alias, joinOn);
        }



        public FromClause   FullJoin(ITableNode table, string? alias, JoinOnWord joinOn)
        {
            return Join(JoinKind.Full, table, alias, joinOn);
        }


        #endregion

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

        public ITableNode AddOrGetTable(ITableNode table, string? alias)
		{
			var ts = GetTable(table, alias);

			if (ts != null)
				return ts;


			var t = Add(table, alias);
			return t;
		}


		#region QueryElement Members

		public override ClauseType NodeType => ClauseType.FromClause;

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

		public void Cleanup()
		{
			Tables.Clear();
		}


	}
}
