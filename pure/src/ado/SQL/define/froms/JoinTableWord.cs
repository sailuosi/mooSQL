using System;
using System.Collections.Generic;

namespace mooSQL.data.model
{
	/// <summary>
	/// join后的table
	/// </summary>
	public class JoinTableWord : Clause, ITableNode
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitJoinedTable(this);
        }
		public JoinKind                 JoinType           { get; set; }
		public ITableNode           Table              { get; set; }
		public SearchConditionWord       Condition          { get; set; }
		public bool                     IsWeak             { get; set; }
		public bool                     CanConvertApply    { get; set; }
		public List<QueryExtension>? SqlQueryExtensions { get; set; }
		public SourceCardinality        Cardinality        { get; set; }

		public JoinTableWord(JoinKind joinType, ITableNode table, bool isWeak, SearchConditionWord searchCondition) : base(ClauseType.JoinedTable, null)
        {
			JoinType        = joinType;
			Table           = table;
			IsWeak          = isWeak;
			Condition       = searchCondition;
			CanConvertApply = true;
		}

		public JoinTableWord(JoinKind joinType, ITableNode table, bool isWeak)
			: this(joinType, table, isWeak, new SearchConditionWord())
		{
		}

		public JoinTableWord(JoinKind joinType, ITableNode table, string? alias, bool isWeak)
			: this(joinType, new DerivatedTableWord(table, alias), isWeak)
		{
		}


		public override ClauseType NodeType => ClauseType.JoinedTable;

        public string Name => throw new NotImplementedException();

        public FieldWord All => throw new NotImplementedException();

        public int SourceID => throw new NotImplementedException();

        public SqlTableType SqlTableType => throw new NotImplementedException();

        public  IElementWriter ToString(IElementWriter writer)
		{
			if (!writer.AddVisited(this))
				return writer.Append("...");

			if (IsWeak)
				writer.Append("WEAK ");

			switch (JoinType)
			{
				case JoinKind.Inner      : writer.Append("INNER JOIN ");  break;
				case JoinKind.Cross      : writer.Append("CROSS JOIN ");  break;
				case JoinKind.Left       : writer.Append("LEFT JOIN ");   break;
				case JoinKind.CrossApply : writer.Append("CROSS APPLY "); break;
				case JoinKind.OuterApply : writer.Append("OUTER APPLY "); break;
				case JoinKind.Right      : writer.Append("RIGHT JOIN ");  break;
				case JoinKind.Full       : writer.Append("FULL JOIN ");   break;
				case JoinKind.FullApply  : writer.Append("FULL APPLY ");  break;
				case JoinKind.RightApply : writer.Append("RIGHT APPLY "); break;
				default                  : writer.Append("SOME JOIN ");   break;
			}

			if (Cardinality != SourceCardinality.Unknown)
				writer.Append(" (" + Cardinality + ") ");


				writer
					.AppendElement(Table);
			

			if (JoinType != JoinKind.Cross)
			{
				writer
					.Append(" ON ")
					.AppendElement(Condition);
			}

			writer.RemoveVisited(this);

			return writer;
		}

        public IList<IExpWord>? GetKeys(bool allIfEmpty)
        {
            throw new NotImplementedException();
        }
    }
}
