using System;

namespace mooSQL.data.model
{
	/// <summary>
	/// 编译期锚点占位（如 OUTPUT 的 inserted/deleted 伪表引用），用于后续展开。
	/// </summary>
	public class AnchorWord : ExpWordBase
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitAnchorWord(this);
        }

		/// <summary>锚点语义类别。</summary>
		public enum AnchorKindEnum
		{
			/// <summary>DELETED 伪表。</summary>
			Deleted,
			/// <summary>INSERTED 伪表。</summary>
			Inserted,
			/// <summary>普通表来源。</summary>
			TableSource,
			/// <summary>表名引用。</summary>
			TableName,
			/// <summary>自引用列上下文。</summary>
			TableAsSelfColumn,
			/// <summary>自引用列或字段。</summary>
			TableAsSelfColumnOrField,
		}

		/// <summary>锚点种类。</summary>
		public AnchorKindEnum AnchorKind { get; }
		/// <summary>被包裹的底层表达式。</summary>
		public IExpWord SqlExpression { get; private set; }

		/// <summary>创建锚点节点。</summary>
		public AnchorWord(IExpWord sqlExpression, AnchorKindEnum anchorKind,Type type=null) : base(ClauseType.SqlAnchor, type)
        {
			SqlExpression = sqlExpression;
			AnchorKind    = anchorKind;
		}

		/// <summary>替换内部表达式。</summary>
		public void Modify(IExpWord expression)
		{
			SqlExpression = expression;
		}



		/// <inheritdoc />
		public override bool Equals(IExpWord other, Func<IExpWord, IExpWord, bool> comparer)
		{
			if (other is not AnchorWord anchor)
				return false;

			return AnchorKind == anchor.AnchorKind && SqlExpression.Equals(anchor.SqlExpression);
		}

		/// <inheritdoc />
		public override ClauseType NodeType => ClauseType.SqlAnchor;

		/// <inheritdoc />
		public override int   Precedence => PrecedenceLv.Primary;

        /// <inheritdoc />
        public override Type? SystemType => SqlExpression.SystemType;
        /// <inheritdoc />
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
