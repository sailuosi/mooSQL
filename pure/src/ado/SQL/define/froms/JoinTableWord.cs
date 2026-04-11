using System;
using System.Collections.Generic;

namespace mooSQL.data.model
{
	/// <summary>
	/// join后的table
	/// </summary>
	public class JoinTableWord : Clause, ITableNode
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitJoinedTable(this);
        }
		/// <summary>JOIN 种类。</summary>
		public JoinKind                 JoinType           { get; set; }
		/// <summary>右侧表或派生表。</summary>
		public ITableNode           Table              { get; set; }
		/// <summary>ON 条件（CROSS 时可为空）。</summary>
		public SearchConditionWord       Condition          { get; set; }
		/// <summary>弱 JOIN（优化/语义提示）。</summary>
		public bool                     IsWeak             { get; set; }
		/// <summary>是否允许转换为 APPLY 形式。</summary>
		public bool                     CanConvertApply    { get; set; }
		/// <summary>方言扩展（hint 等）。</summary>
		public List<QueryExtension>? SqlQueryExtensions { get; set; }
		/// <summary>估计的关联基数。</summary>
		public SourceCardinality        Cardinality        { get; set; }

		/// <summary>完整指定 JOIN 与条件。</summary>
		public JoinTableWord(JoinKind joinType, ITableNode table, bool isWeak, SearchConditionWord searchCondition) : base(ClauseType.JoinedTable, null)
        {
			JoinType        = joinType;
			Table           = table;
			IsWeak          = isWeak;
			Condition       = searchCondition;
			CanConvertApply = true;
		}

		/// <summary>使用空 ON 条件构造。</summary>
		public JoinTableWord(JoinKind joinType, ITableNode table, bool isWeak)
			: this(joinType, table, isWeak, new SearchConditionWord())
		{
		}

		/// <summary>为右侧表自动包一层 <see cref="DerivatedTableWord"/> 别名。</summary>
		public JoinTableWord(JoinKind joinType, ITableNode table, string? alias, bool isWeak)
			: this(joinType, new DerivatedTableWord(table, alias), isWeak)
		{
		}


		/// <inheritdoc />
		public override ClauseType NodeType => ClauseType.JoinedTable;

        /// <inheritdoc />
        public string Name => throw new NotImplementedException();

        /// <inheritdoc />
        public FieldWord All => throw new NotImplementedException();

        /// <inheritdoc />
        public int SourceID => throw new NotImplementedException();

        /// <inheritdoc />
        public SqlTableType SqlTableType => throw new NotImplementedException();

        /// <inheritdoc />
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

        /// <inheritdoc />
        public IList<IExpWord>? GetKeys(bool allIfEmpty)
        {
            throw new NotImplementedException();
        }
    }
}
