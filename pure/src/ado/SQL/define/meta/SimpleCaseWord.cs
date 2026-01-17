using System;
using System.Collections.Generic;
using System.Linq;

namespace mooSQL.data.model
{
	using Common;

	public class SimpleCaseWord : ExpWordBase
	{
		public class CaseExpression
		{
			public CaseExpression(IExpWord matchValue, IExpWord resultExpression)
			{
				MatchValue       = matchValue;
				ResultExpression = resultExpression;
			}

			public void Modify(IExpWord matchValue, IExpWord resultExpression)
			{
				MatchValue       = matchValue;
				ResultExpression = resultExpression;
			}

			public CaseExpression Update(IExpWord matchValue, IExpWord resultExpression)
			{
				if (ReferenceEquals(MatchValue, matchValue) && ReferenceEquals(ResultExpression, resultExpression))
					return this;

				return new CaseExpression(matchValue, resultExpression);
			}

			public IExpWord MatchValue       { get; set; }
			public IExpWord ResultExpression { get; set; }
		}

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

		public IExpWord                PrimaryExpression { get; private set; }
		public IExpWord?               ElseExpression    { get; private set; }
		public IReadOnlyList<CaseExpression> Cases             => _cases;

		public override int              Precedence  => PrecedenceLv.Primary;
        public override Type? SystemType => _dataType.SystemType;
        public override ClauseType NodeType => ClauseType.SqlSimpleCase;

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



		public void Modify(IExpWord primaryExpression, List<CaseExpression> cases, IExpWord? resultExpression)
		{
			PrimaryExpression = primaryExpression;
			_cases            = cases;
			ElseExpression    = resultExpression;
		}
	}
}
