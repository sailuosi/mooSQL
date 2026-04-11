using System;

using mooSQL.data.model;

namespace mooSQL.linq.SqlQuery
{
	/// <summary>
	/// 表示“参数占位 + 内联表达式”的 SQL 片段基类（用于参数化与内联求值）。
	/// </summary>
	public abstract class BaseInlinedWord : ExpWordBase
    {
		/// <summary>
		/// 使用参数词、内联子表达式与从句类型构造。
		/// </summary>
		protected BaseInlinedWord(ParameterWord parameter, IExpWord inlinedValue,ClauseType clauseType, Type type = null) : base(clauseType, type)
        {
			Parameter    = parameter;
			InlinedValue = inlinedValue;
		}

		/// <summary>
		/// 在求值上下文中解析得到最终参与生成 SQL 的表达式节点。
		/// </summary>
		public abstract IExpWord GetSqlExpression(EvaluateContext evaluationContext);

		/// <summary>关联的参数占位符。</summary>
		public ParameterWord Parameter    { get; private set; }
		/// <summary>内联的子表达式。</summary>
		public IExpWord InlinedValue { get; private set; }

		/// <inheritdoc />
		public override int   Precedence => InlinedValue.Precedence;

        /// <inheritdoc />
        public override Type? SystemType => InlinedValue.SystemType;
        /// <summary>替换参数与内联值。</summary>
        public void Modify(ParameterWord parameter, ExpWordBase inlinedValue)
		{
			Parameter    = parameter;
			InlinedValue = inlinedValue;
		}
	}
}
