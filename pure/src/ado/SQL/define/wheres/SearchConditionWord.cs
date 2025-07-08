using System;
using System.Collections.Generic;
using System.Linq;

namespace mooSQL.data.model
{
	public class SearchConditionWord : ExpWordBase, IAffirmWord
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitSearchCondition(this);
        }


		public SearchConditionWord(bool isOr = false, Type type = null) : base(ClauseType.SearchCondition, type)
        {
			IsOr = isOr;
		}

		public SearchConditionWord(bool isOr, IAffirmWord predicate) : this(isOr)
		{
			Predicates.Add(predicate);
		}

		public SearchConditionWord(bool isOr, IAffirmWord predicate1, IAffirmWord predicate2) : this(isOr)
		{
			Predicates.Add(predicate1);
			Predicates.Add(predicate2);
		}

		public SearchConditionWord(bool isOr, IEnumerable<IAffirmWord> predicates) : this(isOr)
		{
			Predicates.AddRange(predicates);
		}

		public List<IAffirmWord> Predicates { get; } = new();

		public SearchConditionWord Add(IAffirmWord predicate)
		{
			Predicates.Add(predicate);
			return this;
		}

		public SearchConditionWord AddRange(IEnumerable<IAffirmWord> predicates)
		{
			Predicates.AddRange(predicates);
			return this;
		}

		public bool IsOr  { get; set; }
		public bool IsAnd { get => !IsOr; set => IsOr = !value; }

		#region Overrides

		public override ClauseType NodeType => ClauseType.SearchCondition;

		public  IElementWriter ToString(IElementWriter writer)
		{
			if (!writer.AddVisited(this))
				return writer.Append("...");

			// writer
			// 	//.Append("sc=")
			// 	.DebugAppendUniqueId(this);

			writer.Append('(');

			var isFirst = true;
			foreach (ISQLNode c in Predicates)
			{
				if (!isFirst)
				{
					if (IsOr)
						writer.Append(" OR ");
					else
						writer.Append(" AND ");
				}
				else
				{
					isFirst = false;
				}

				writer.AppendElement(c);
			}

			writer.RemoveVisited(this);

			writer.Append(')');

			return writer;
		}

		#endregion

		#region IPredicate Members

		public override int Precedence
		{
			get
			{
				if (Predicates.Count == 0) return PrecedenceLv.Unknown;

				return IsOr ? PrecedenceLv.LogicalDisjunction : PrecedenceLv.LogicalConjunction;
			}
		}



		#endregion

		#region IInvertibleElement Members

		public bool CanInvert(ISQLNode nullability)
		{
			var maxCount = Math.Max(Predicates.Count / 2, 2);
			if (Predicates.Count > maxCount)
				return false;

			if (Predicates.Count > 1 && IsAnd)
				return false;

			return Predicates.All(p =>
			{
				if (p is not SearchConditionWord)
					return false;

				if (p is affirms.ExprExpr exprExpr && (exprExpr.WithNull != null || exprExpr.WithNull == true))
				{
					return false;
				}

				return p.CanInvert(nullability);
			});
		}

		public IAffirmWord Invert(ISQLNode nullability)
		{
			if (Predicates.Count == 0)
			{
				return new SearchConditionWord(!IsOr);
			}

			var newPredicates = Predicates.Select(p => new affirms.Not(p));

			return new SearchConditionWord(!IsOr, newPredicates);
		}

		public bool IsTrue()
		{
			if (Predicates.Count == 0)
				return true;

			if (Predicates.Count==1 && Predicates[0].NodeType == ClauseType.TruePredicate )
				return true;

			return false;
		}

		public bool IsFalse()
		{
			if (Predicates.Count == 0)
				return false;

			if (Predicates.Count == 1 && Predicates[0].NodeType == ClauseType.FalsePredicate)
				return true;

			return false;
		}

		#endregion

		#region ISqlExpression Members



		public bool CanBeNull => false;

		public override bool Equals(IExpWord other, Func<IExpWord, IExpWord, bool> comparer)
		{
			return other is IAffirmWord otherPredicate
				&& Equals(otherPredicate, comparer);
		}

		public bool Equals(IAffirmWord other, Func<IExpWord, IExpWord, bool> comparer)
		{
			if (other is not SearchConditionWord otherCondition
				|| Predicates.Count != otherCondition.Predicates.Count || IsOr != otherCondition.IsOr)
			{
				return false;
			}

			for (var i = 0; i < Predicates.Count; i++)
				if (!Predicates[i].Equals(otherCondition.Predicates[i], comparer))
					return false;

			return true;
		}

        #endregion
        public override Type SystemType => typeof(bool);
        public void Deconstruct(out List<IAffirmWord> predicates)
		{
			predicates = Predicates;
		}
	}
}
