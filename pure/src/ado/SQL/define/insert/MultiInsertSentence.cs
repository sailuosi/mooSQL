using System;
using System.Collections.Generic;

namespace mooSQL.data.model
{
	/// <summary>
	/// 多重插入
	/// </summary>
	public class MultiInsertSentence : BaseSentence
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitMultiInsertSentence(this);
        }
		/// <summary>源子查询或表（INSERT ALL/FIRST 的数据来源）。</summary>
		public ITableNode               Source     { get; private  set; }
		/// <summary>各条件分支插入。</summary>
		public List<ConditionalInsertClause> Inserts    { get; private  set; }
		/// <summary><c>FIRST</c> 或 <c>ALL</c> 语义。</summary>
		public MultiInsertType                  InsertType { get;  set; }

		/// <summary>指定源表或子查询。</summary>
		public MultiInsertSentence(ITableNode source) : base(ClauseType.MultiInsertStatement, null)
        {
			Source = source;
			Inserts = new List<ConditionalInsertClause>();
		}

        /// <summary>完整指定类型、源与分支列表。</summary>
        public MultiInsertSentence(MultiInsertType type, ITableNode source, List<ConditionalInsertClause> inserts) : base(ClauseType.MultiInsertStatement, null)
        {
			InsertType = type;
			Source     = source;
			Inserts    = inserts;
		}

		/// <summary>追加一条 WHEN/THEN 插入分支。</summary>
		public void Add(SearchConditionWord? when, InsertClause insert)
			=> Inserts.Add(new ConditionalInsertClause(insert, when));

		/// <summary>替换源节点。</summary>
		public void Update(ITableNode source)
		{
			Source  = source;
		}

		/// <inheritdoc />
		public override QueryType          QueryType   => QueryType.MultiInsert;
		/// <inheritdoc />
		public override ClauseType   NodeType => ClauseType.MultiInsertStatement;

		/// <inheritdoc />
		public override IElementWriter ToString(IElementWriter writer)
		{
			writer.AppendLine(InsertType == MultiInsertType.First ? "INSERT FIRST " : "INSERT ALL ");

			foreach (var insert in Inserts)
				writer.AppendElement(insert);

			writer.AppendElement(Source);

			return writer;
		}




		/// <inheritdoc />
		public override SelectQueryClause? SelectQuery
		{
			get => null;
			set => throw new InvalidOperationException();
		}
        /// <inheritdoc />
        public override bool IsParameterDependent { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
