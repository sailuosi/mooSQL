using System;
using System.Collections.Generic;
using System.Linq;

namespace mooSQL.data.model
{
	using Common;

	/// <summary>简单 CASE：<c>CASE expr WHEN … THEN …</c>。</summary>
	public class SimpleCaseWord : ExpWordBase
	{
		/// <summary>单个 WHEN 分支：匹配值与结果。</summary>
		public class CaseExpression
		{
			/// <summary>构造一分支。</summary>
			public CaseExpression(IExpWord matchValue, IExpWord resultExpression)
			{
				MatchValue       = matchValue;
				ResultExpression = resultExpression;
			}

			/// <summary>就地替换。</summary>
			public void Modify(IExpWord matchValue, IExpWord resultExpression)
			{
				MatchValue       = matchValue;
				ResultExpression = resultExpression;
			}

			/// <summary>有变化则返回新实例。</summary>
			public CaseExpression Update(IExpWord matchValue, IExpWord resultExpression)
			{
				if (ReferenceEquals(MatchValue, matchValue) && ReferenceEquals(ResultExpression, resultExpression))
					return this;

				return new CaseExpression(matchValue, resultExpression);
			}

			/// <summary>与主表达式比较的常量/子表达式。</summary>
			public IExpWord MatchValue       { get; set; }
			/// <summary>THEN 结果。</summary>
			public IExpWord ResultExpression { get; set; }
		}

		/// <summary>构造简单 CASE。</summary>
		public SimpleCaseWord(DbDataType dataType, IExpWord primaryExpression, IReadOnlyCollection<CaseExpression> cases, IExpWord? elseExpression, Type type = null) 
			: base(ClauseType.SqlSimpleCase, type)
        {
			_dataType         = dataType;
			PrimaryExpression = primaryExpression;
			_cases            = cases.ToList();
			ElseExpression    = elseExpression;
		}

		internal List<CaseExpression> _cases;
		readonly DbDataType           _dataType;

		/// <summary><c>CASE</c> 后跟的主表达式。</summary>
		public IExpWord                PrimaryExpression { get; private set; }
		/// <summary>ELSE 分支。</summary>
		public IExpWord?               ElseExpression    { get; private set; }
		/// <summary>WHEN 分支列表。</summary>
		public IReadOnlyList<CaseExpression> Cases             => _cases;

		/// <inheritdoc />
		public override int              Precedence  => PrecedenceLv.Primary;
        /// <inheritdoc />
        public override Type? SystemType => _dataType.SystemType;
        /// <inheritdoc />
        public override ClauseType NodeType => ClauseType.SqlSimpleCase;

		/// <inheritdoc />
		public IElementWriter ToString(IElementWriter writer)
		{
			writer
				.Append("$CASE$ ")
				.AppendElement(PrimaryExpression)
				.AppendLine();

			using (writer.IndentScope())
			{
				foreach (var c in Cases)
				{
					writer
						.Append("WHEN ")
						.AppendElement(c.MatchValue)
						.Append(" THEN ")
						.AppendElement(c.ResultExpression)
						.AppendLine();
				}

				if (ElseExpression != null)
				{
					writer
						.Append("ELSE ")
						.AppendElement(ElseExpression)
						.AppendLine();
				}
			}

			writer.AppendLine("END");

			return writer;
		}

		/// <inheritdoc />
		public override bool Equals(IExpWord other, Func<IExpWord, IExpWord, bool> comparer)
		{
			if (other is not SimpleCaseWord caseOther)
				return false;

			if (!comparer(PrimaryExpression, caseOther.PrimaryExpression))
				return false;

			if (ElseExpression != null && caseOther.ElseExpression == null)
				return false;

			if (ElseExpression == null && caseOther.ElseExpression != null)
				return false;

			if (ElseExpression != null && caseOther.ElseExpression != null && !ElseExpression.Equals(caseOther.ElseExpression, comparer))
				return false;

			if (Cases.Count != caseOther.Cases.Count) 
				return false;

			for (var index = 0; index < Cases.Count; index++)
			{
				var c = Cases[index];
				var o = caseOther.Cases[index];

				if (!c.MatchValue.Equals(o.MatchValue))
					return false;

				if (!c.ResultExpression.Equals(o.ResultExpression, comparer)) 
					return false;
			}

			return true;
		}



		/// <summary>替换主表达式、分支与 ELSE。</summary>
		public void Modify(IExpWord primaryExpression, List<CaseExpression> cases, IExpWord? resultExpression)
		{
			PrimaryExpression = primaryExpression;
			_cases            = cases;
			ElseExpression    = resultExpression;
		}
	}
}
