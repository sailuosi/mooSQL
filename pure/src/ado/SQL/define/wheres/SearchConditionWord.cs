using System;
using System.Collections.Generic;
using System.Linq;

namespace mooSQL.data.model
{
	/// <summary>
	/// 用 AND/OR 组合的搜索条件（一组 <see cref="IAffirmWord"/> 谓词）。
	/// </summary>
	public class SearchConditionWord : ExpWordBase, IAffirmWord
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitSearchCondition(this);
        }


		/// <summary>
		/// 创建空条件组；<paramref name="isOr"/> 为 true 时子谓词以 OR 连接，否则为 AND。
		/// </summary>
		public SearchConditionWord(bool isOr = false, Type type = null) : base(ClauseType.SearchCondition, type)
        {
			IsOr = isOr;
		}

		/// <summary>单谓词组成的条件组。</summary>
		public SearchConditionWord(bool isOr, IAffirmWord predicate) : this(isOr)
		{
			Predicates.Add(predicate);
		}

		/// <summary>两个谓词组成的条件组。</summary>
		public SearchConditionWord(bool isOr, IAffirmWord predicate1, IAffirmWord predicate2) : this(isOr)
		{
			Predicates.Add(predicate1);
			Predicates.Add(predicate2);
		}

		/// <summary>多个谓词组成的条件组。</summary>
		public SearchConditionWord(bool isOr, IEnumerable<IAffirmWord> predicates) : this(isOr)
		{
			Predicates.AddRange(predicates);
		}

		/// <summary>本组内的谓词列表（顺序有意义）。</summary>
		public List<IAffirmWord> Predicates { get; } = new();

		/// <summary>追加一个谓词并返回本实例以链式调用。</summary>
		public SearchConditionWord Add(IAffirmWord predicate)
		{
			Predicates.Add(predicate);
			return this;
		}

		/// <summary>批量追加谓词。</summary>
		public SearchConditionWord AddRange(IEnumerable<IAffirmWord> predicates)
		{
			Predicates.AddRange(predicates);
			return this;
		}

		/// <summary>为 true 时子谓词以 OR 连接。</summary>
		public bool IsOr  { get; set; }
		/// <summary>为 true 时子谓词以 AND 连接（与 <see cref="IsOr"/> 互斥）。</summary>
		public bool IsAnd { get => !IsOr; set => IsOr = !value; }

		#region Overrides

		/// <inheritdoc />
		public override ClauseType NodeType => ClauseType.SearchCondition;

		/// <inheritdoc />
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

		/// <inheritdoc />
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

		/// <inheritdoc />
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

		/// <inheritdoc />
		public IAffirmWord Invert(ISQLNode nullability)
		{
			if (Predicates.Count == 0)
			{
				return new SearchConditionWord(!IsOr);
			}

			var newPredicates = Predicates.Select(p => new affirms.Not(p));

			return new SearchConditionWord(!IsOr, newPredicates);
		}

		/// <summary>是否等价于恒真（空组或单真谓词）。</summary>
		public bool IsTrue()
		{
			if (Predicates.Count == 0)
				return true;

			if (Predicates.Count==1 && Predicates[0].NodeType == ClauseType.TruePredicate )
				return true;

			return false;
		}

		/// <summary>是否等价于恒假。</summary>
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



		/// <inheritdoc />
		public bool CanBeNull => false;

		/// <inheritdoc />
		public override bool Equals(IExpWord other, Func<IExpWord, IExpWord, bool> comparer)
		{
			return other is IAffirmWord otherPredicate
				&& Equals(otherPredicate, comparer);
		}

		/// <inheritdoc />
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
        /// <inheritdoc />
        public override Type SystemType => typeof(bool);
        /// <summary>解构为谓词列表（与 <see cref="Predicates"/> 同一引用）。</summary>
        public void Deconstruct(out List<IAffirmWord> predicates)
		{
			predicates = Predicates;
		}
	}
}
