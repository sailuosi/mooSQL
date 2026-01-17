using System;

namespace mooSQL.data.model
{
	public class AnchorWord : ExpWordBase
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitAnchorWord(this);
        }

		public enum AnchorKindEnum
		{
			Deleted,
			Inserted,
			TableSource,
			TableName,
			TableAsSelfColumn,
			TableAsSelfColumnOrField,
		}

		public AnchorKindEnum AnchorKind { get; }
		public IExpWord SqlExpression { get; private set; }

		public AnchorWord(IExpWord sqlExpression, AnchorKindEnum anchorKind,Type type=null) : base(ClauseType.SqlAnchor, type)
        {
			SqlExpression = sqlExpression;
			AnchorKind    = anchorKind;
		}

		public void Modify(IExpWord expression)
		{
			SqlExpression = expression;
		}



		public override bool Equals(IExpWord other, Func<IExpWord, IExpWord, bool> comparer)
		{
			if (other is not AnchorWord anchor)
				return false;

			return AnchorKind == anchor.AnchorKind && SqlExpression.Equals(anchor.SqlExpression);
		}

		public override ClauseType NodeType => ClauseType.SqlAnchor;

		public override int   Precedence => PrecedenceLv.Primary;

        public override Type? SystemType => SqlExpression.SystemType;
        public  IElementWriter ToString(IElementWriter writer)
		{
			writer.Append('$')
				.Append(AnchorKind.ToString())
				.Append("$.")
				.AppendElement(SqlExpression);

			return writer;
		}
	}
}
