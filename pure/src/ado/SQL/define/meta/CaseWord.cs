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
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitCaseExpression(this);
        }
		/// <summary>搜索式 CASE 中单个 WHEN 分支。</summary>
		public class CaseItem
		{
			/// <summary>构造一分支。</summary>
			public CaseItem(IAffirmWord condition, IExpWord resultExpression)
			{
				Condition        = condition;
				ResultExpression = resultExpression;
			}

			/// <summary>就地替换条件与结果。</summary>
			public void Modify(IAffirmWord condition, IExpWord resultExpression)
			{
				Condition        = condition;
				ResultExpression = resultExpression;
			}

			/// <summary>有变化则返回新实例，否则返回 <c>this</c>。</summary>
			public CaseItem Update(IAffirmWord condition, IExpWord resultExpression)
			{
				if (ReferenceEquals(Condition, condition) && ReferenceEquals(ResultExpression, resultExpression))
					return this;

				return new CaseItem(condition, resultExpression);
			}

			/// <summary>WHEN 谓词。</summary>
			public IAffirmWord  Condition        { get; set; }
			/// <summary>THEN 结果表达式。</summary>
			public IExpWord ResultExpression { get; set; }
		}

		/// <summary>构造搜索式 <c>CASE WHEN … THEN …</c>。</summary>
		public CaseWord(DbDataType dataType, IReadOnlyCollection<CaseItem> cases, IExpWord? elseExpression, Type type = null) : base(ClauseType.SqlSimpleCase, type)
        {
			_dataType      = dataType;
			_cases         = cases.ToList();
			ElseExpression = elseExpression;
		}

		/// <summary>分支列表（可变性由 <see cref="Modify"/> 维护）。</summary>
		public List<CaseItem> _cases;
		readonly DbDataType     _dataType;

		/// <summary>ELSE 分支。</summary>
		public IExpWord?         ElseExpression { get; private set; }
		/// <summary>只读视图。</summary>
		public IReadOnlyList<CaseItem> Cases          => _cases;
		/// <summary>结果 SQL 类型。</summary>
		public DbDataType              Type           => _dataType;

		/// <inheritdoc />
		public override int              Precedence  => PrecedenceLv.Primary;
        /// <inheritdoc />
        public override Type? SystemType => _dataType.SystemType;
        /// <inheritdoc />
        public override ClauseType NodeType => ClauseType.SqlCase;

		/// <inheritdoc />
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

		/// <inheritdoc />
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



		/// <summary>替换全部分支与 ELSE。</summary>
		public void Modify(List<CaseItem> cases, IExpWord? resultExpression)
		{
			_cases         = cases;
			ElseExpression = resultExpression;
		}

		/// <summary>仅替换 ELSE。</summary>
		public void Modify(IExpWord? resultExpression)
		{
			ElseExpression = resultExpression;
		}
	}
}
