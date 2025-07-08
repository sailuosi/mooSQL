using System;
using System.Collections.Generic;
using System.Linq;

namespace mooSQL.data.model
{
	using Common;
	/// <summary>
	/// case 表达式
	/// </summary>
	public class CaseWord : ExpWordBase
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitCaseExpression(this);
        }
		public class CaseItem
		{
			public CaseItem(IAffirmWord condition, IExpWord resultExpression)
			{
				Condition        = condition;
				ResultExpression = resultExpression;
			}

			public void Modify(IAffirmWord condition, IExpWord resultExpression)
			{
				Condition        = condition;
				ResultExpression = resultExpression;
			}

			public CaseItem Update(IAffirmWord condition, IExpWord resultExpression)
			{
				if (ReferenceEquals(Condition, condition) && ReferenceEquals(ResultExpression, resultExpression))
					return this;

				return new CaseItem(condition, resultExpression);
			}

			public IAffirmWord  Condition        { get; set; }
			public IExpWord ResultExpression { get; set; }
		}

		public CaseWord(DbDataType dataType, IReadOnlyCollection<CaseItem> cases, IExpWord? elseExpression, Type type = null) : base(ClauseType.SqlSimpleCase, type)
        {
			_dataType      = dataType;
			_cases         = cases.ToList();
			ElseExpression = elseExpression;
		}

		public List<CaseItem> _cases;
		readonly DbDataType     _dataType;

		public IExpWord?         ElseExpression { get; private set; }
		public IReadOnlyList<CaseItem> Cases          => _cases;
		public DbDataType              Type           => _dataType;

		public override int              Precedence  => PrecedenceLv.Primary;
        public override Type? SystemType => _dataType.SystemType;
        public override ClauseType NodeType => ClauseType.SqlCase;

		public  IElementWriter ToString(IElementWriter writer)
		{
			writer
				.Append("$CASE$")
				.AppendLine();

			using (writer.IndentScope())
			{
				foreach (var c in Cases)
				{
					writer
						.Append("WHEN ")
						.AppendElement(c.Condition)
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
			if (other is not CaseWord caseOther)
				return false;

			if (ElseExpression != null && caseOther.ElseExpression == null)
				return false;

			if (ElseExpression == null && caseOther.ElseExpression != null)
				return false;

			if (ElseExpression != null && caseOther.ElseExpression != null && !comparer(ElseExpression, caseOther.ElseExpression))
				return false;

			if (Cases.Count != caseOther.Cases.Count) 
				return false;

			for (var index = 0; index < Cases.Count; index++)
			{
				var c = Cases[index];
				var o = caseOther.Cases[index];
				if (!c.Condition.Equals(o.Condition))
					return false;

				if (!c.ResultExpression.Equals(o.ResultExpression, comparer)) 
					return false;
			}

			return true;
		}



		public void Modify(List<CaseItem> cases, IExpWord? resultExpression)
		{
			_cases         = cases;
			ElseExpression = resultExpression;
		}

		public void Modify(IExpWord? resultExpression)
		{
			ElseExpression = resultExpression;
		}
	}
}
