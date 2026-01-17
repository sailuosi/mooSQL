using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;



namespace mooSQL.data.model
{
	public class RowWord : Clause, IExpWord
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitRowWord(this);
        }
        public RowWord(IExpWord[] values) : base(ClauseType.SqlRow, null)
        {
			Values = values;
		}

		public IExpWord[] Values { get; }

		//public bool CanBeNullable(NullabilityContext nullability)
		//{
		//	// SqlRow doesn't exactly have its own type and nullability, being a collection of values.
		//	// But it can be null in the sense that `(1, 2) IS NULL` can be true (when all values are null).
		//	return QueryHelper.CalcCanBeNull(null, ParametersNullabilityType.IfAllParametersNullable, Values.Select(v => v.CanBeNullable(nullability)));
		//}

		public int Precedence => PrecedenceLv.Primary;

		public Type? SystemType => null;


		public ClauseType NodeType => ClauseType.SqlRow;

		public bool Equals(IExpWord other, Func<IExpWord, IExpWord, bool> comparer)
			=> other is RowWord row && Values.Zip(row.Values, comparer).All(x => x);

		public bool Equals( IExpWord other)
			=> other is RowWord row && Values.SequenceEqual(row.Values);

#if OVERRIDETOSTRING
		public override string ToString()
		{
			return this.ToDebugString();
		}
#endif


	}
}
