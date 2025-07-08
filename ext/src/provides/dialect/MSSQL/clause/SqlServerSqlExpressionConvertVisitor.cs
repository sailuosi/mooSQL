using System;

namespace mooSQL.linq.DataProvider.SqlServer
{
    using mooSQL.data.model;
    using mooSQL.data.model.affirms;
    using mooSQL.linq.Extensions;
    using mooSQL.utils;
    using SqlProvider;
    using SqlQuery;

	public class SqlServerSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{


		public SqlServerSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
			
		}

		public override IAffirmWord ConvertSearchStringPredicate(SearchString predicate)
		{
			var like = base.ConvertSearchStringPredicate(predicate);

			if (predicate.CaseSensitive.EvaluateBoolExpression(EvaluationContext) == true)
			{
				ExprExpr? subStrPredicate = null;

				switch (predicate.Kind)
				{
					case SearchString.SearchKind.StartsWith:
					{
						subStrPredicate =
							new ExprExpr(
								new FunctionWord(typeof(byte[]), "Convert", DataTypeWord.DbVarBinary, new FunctionWord(
									typeof(string), "LEFT", predicate.Expr1,
									new FunctionWord(typeof(int), "LEN", predicate.Expr2))),
								AffirmWord.Operator.Equal,
								new FunctionWord(typeof(byte[]), "Convert", DataTypeWord.DbVarBinary, predicate.Expr2),
								null
							);

						break;
					}

					case SearchString.SearchKind.EndsWith:
					{
						subStrPredicate =
							new ExprExpr(
								new FunctionWord(typeof(byte[]), "Convert", DataTypeWord.DbVarBinary, new FunctionWord(
									typeof(string), "RIGHT", predicate.Expr1,
									new FunctionWord(typeof(int), "LEN", predicate.Expr2))),
								AffirmWord.Operator.Equal,
								new FunctionWord(typeof(byte[]), "Convert", DataTypeWord.DbVarBinary, predicate.Expr2),
								null
							);

						break;
					}
					case SearchString.SearchKind.Contains:
					{
						subStrPredicate =
							new ExprExpr(
								new FunctionWord(typeof(int), "CHARINDEX",
									new FunctionWord(typeof(byte[]), "Convert", DataTypeWord.DbVarBinary,
										predicate.Expr2),
									new FunctionWord(typeof(byte[]), "Convert", DataTypeWord.DbVarBinary,
										predicate.Expr1)),
								AffirmWord.Operator.Greater,
								new ValueWord(0), null);

						break;
					}

				}

				if (subStrPredicate != null)
				{
					var result = new SearchConditionWord(predicate.IsNot, 
						like,
						subStrPredicate.MakeNot(predicate.IsNot));

					return result;
				}
			}

			return like;
		}

		public override Clause ConvertSqlBinaryExpression(BinaryWord element)
		{
			switch (element.Operation)
			{
				case "%":
				{
					var type1 = element.Expr1.SystemType!.UnwrapNullable();

					if (type1 == typeof(double) || type1 == typeof(float))
					{
						return new BinaryWord(
							element.Expr2.SystemType!,
							new FunctionWord(typeof(int), "Convert", DataTypeWord.Int32, element.Expr1),
							element.Operation,
							element.Expr2);
					}

					break;
				}
			}

			return base.ConvertSqlBinaryExpression(element);
		}

		protected override IExpWord ConvertConversion(CastWord cast)
		{
			cast = FloorBeforeConvert(cast);

			return base.ConvertConversion(cast);
		}
	}
}
