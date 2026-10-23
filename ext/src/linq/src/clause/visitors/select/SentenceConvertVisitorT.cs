using mooSQL.data.model;
using System;

namespace mooSQL.linq.clause
{
	/// <summary>
	/// SentenceConvertVisitorT type.
	/// </summary>
	/// <summary>
	/// SentenceConvertVisitorT type.
	/// </summary>
	public class SqlQueryConvertVisitor<TContext> : SentenceConvertVisitorBase
	{
		Func<SqlQueryConvertVisitor<TContext>, Clause, Clause> _convertFunc = default!;
		/// <summary>
		/// Gets or sets Context.
		/// </summary>

		public TContext Context { get; private set; } = default!;
		/// <summary>
		/// SqlQueryConvertVisitor
		/// </summary>

		public SqlQueryConvertVisitor(bool allowMutation) : base(allowMutation, null)
		{
		}
		/// <summary>
		/// Gets or sets ColumnExpression.
		/// </summary>

		public IExpWord? ColumnExpression { get; private set; }
		/// <summary>
		/// Convert
		/// </summary>

		public Clause Convert(Clause element, TContext context, Func<SqlQueryConvertVisitor<TContext>, Clause, Clause> convertFunc, bool withStack)
		{
			Context      = context;
			_convertFunc = convertFunc;
			WithStack    = withStack;

			Stack?.Clear();

			return PerformConvert(element);
		}
/// <summary>
/// Cleanup
/// </summary>
	

		public override void Cleanup()
		{
			base.Cleanup();

			_convertFunc = null!;
			Context      = default!;
			WithStack    = false;
			Stack?.Clear();
		}
/// <summary>
/// ConvertElement
/// </summary>
	

		public override Clause ConvertElement(Clause element)
		{
			var newElement = _convertFunc(this, element);

			return newElement;
		}
		/// <summary>
		/// VisitColumnWord
		/// </summary>

		protected  Clause VisitColumnWord(ColumnWord column, Clause expression)
		{
			//ColumnExpression = expression;
			//var newExpression = base.VisitColumnWord(column, expression);
			//ColumnExpression = null;

			return column;
		}
	}
}
