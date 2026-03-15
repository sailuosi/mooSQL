using mooSQL.data.model;
using System;

namespace mooSQL.linq.SqlQuery.Visitors
{
	public class SqlQueryConvertVisitor<TContext> : SqlQueryConvertVisitorBase
	{
		Func<SqlQueryConvertVisitor<TContext>, Clause, Clause> _convertFunc = default!;

		public TContext Context { get; private set; } = default!;

		public SqlQueryConvertVisitor(bool allowMutation) : base(allowMutation, null)
		{
		}

		public IExpWord? ColumnExpression { get; private set; }

		public Clause Convert(Clause element, TContext context, Func<SqlQueryConvertVisitor<TContext>, Clause, Clause> convertFunc, bool withStack)
		{
			Context      = context;
			_convertFunc = convertFunc;
			WithStack    = withStack;

			Stack?.Clear();

			return PerformConvert(element);
		}

		public override void Cleanup()
		{
			base.Cleanup();

			_convertFunc = null!;
			Context      = default!;
			WithStack    = false;
			Stack?.Clear();
		}

		public override Clause ConvertElement(Clause element)
		{
			var newElement = _convertFunc(this, element);

			return newElement;
		}

		protected  Clause VisitColumnWord(ColumnWord column, Clause expression)
		{
			//ColumnExpression = expression;
			//var newExpression = base.VisitColumnWord(column, expression);
			//ColumnExpression = null;

			return column;
		}
	}
}
