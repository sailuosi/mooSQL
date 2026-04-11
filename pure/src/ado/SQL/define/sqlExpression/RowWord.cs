using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;



namespace mooSQL.data.model
{
	/// <summary>
	/// 行值构造表达式，对应 SQL 中 <c>(expr1, expr2, …)</c> 形式。
	/// </summary>
	public class RowWord : Clause, IExpWord
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitRowWord(this);
        }
        /// <summary>由各列/字段表达式组成的一行。</summary>
        public RowWord(IExpWord[] values) : base(ClauseType.SqlRow, null)
        {
			Values = values;
		}

		/// <summary>行中各位置的表达式（顺序与 SQL 中一致）。</summary>
		public IExpWord[] Values { get; }

		//public bool CanBeNullable(NullabilityContext nullability)
		//{
		//	// SqlRow doesn't exactly have its own type and nullability, being a collection of values.
		//	// But it can be null in the sense that `(1, 2) IS NULL` can be true (when all values are null).
		//	return QueryHelper.CalcCanBeNull(null, ParametersNullabilityType.IfAllParametersNullable, Values.Select(v => v.CanBeNullable(nullability)));
		//}

		/// <inheritdoc />
		public int Precedence => PrecedenceLv.Primary;

		/// <inheritdoc />
		public Type? SystemType => null;


		/// <inheritdoc />
		public ClauseType NodeType => ClauseType.SqlRow;

		/// <inheritdoc />
		public bool Equals(IExpWord other, Func<IExpWord, IExpWord, bool> comparer)
			=> other is RowWord row && Values.Zip(row.Values, comparer).All(x => x);

		/// <inheritdoc />
		public bool Equals( IExpWord other)
			=> other is RowWord row && Values.SequenceEqual(row.Values);

#if OVERRIDETOSTRING
		/// <inheritdoc />
		public override string ToString()
		{
			return this.ToDebugString();
		}
#endif


	}
}
