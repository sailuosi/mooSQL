using System;
using System.Collections.Generic;

namespace mooSQL.data.model
{
	/// <summary>
	/// 多重插入
	/// </summary>
	public class MultiInsertSentence : BaseSentence
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitMultiInsertSentence(this);
        }
		public ITableNode               Source     { get; private  set; }
		public List<ConditionalInsertClause> Inserts    { get; private  set; }
		public MultiInsertType                  InsertType { get;  set; }

		public MultiInsertSentence(ITableNode source) : base(ClauseType.MultiInsertStatement, null)
        {
			Source = source;
			Inserts = new List<ConditionalInsertClause>();
		}

        public MultiInsertSentence(MultiInsertType type, ITableNode source, List<ConditionalInsertClause> inserts) : base(ClauseType.MultiInsertStatement, null)
        {
			InsertType = type;
			Source     = source;
			Inserts    = inserts;
		}

		public void Add(SearchConditionWord? when, InsertClause insert)
			=> Inserts.Add(new ConditionalInsertClause(insert, when));

		public void Update(ITableNode source)
		{
			Source  = source;
		}

		public override QueryType          QueryType   => QueryType.MultiInsert;
		public override ClauseType   NodeType => ClauseType.MultiInsertStatement;

		public override IElementWriter ToString(IElementWriter writer)
		{
			writer.AppendLine(InsertType == MultiInsertType.First ? "INSERT FIRST " : "INSERT ALL ");

			foreach (var insert in Inserts)
				writer.AppendElement(insert);

			writer.AppendElement(Source);

			return writer;
		}




		public override SelectQueryClause? SelectQuery
		{
			get => null;
			set => throw new InvalidOperationException();
		}
        public override bool IsParameterDependent { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
