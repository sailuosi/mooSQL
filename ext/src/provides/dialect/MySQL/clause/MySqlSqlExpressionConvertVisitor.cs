using System.Collections.Generic;

namespace mooSQL.linq.DataProvider.MySql
{
	using Extensions;
    using mooSQL.data.model;
    using mooSQL.data.model.affirms;
    using mooSQL.utils;
    using NPOI.SS.Formula.Functions;
    using SqlProvider;
	using SqlQuery;

	public class MySqlSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public MySqlSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		protected override IExpWord ConvertConversion(CastWord cast)
		{
			cast = FloorBeforeConvert(cast);

			var castType = cast.SystemType.UnwrapNullable();

			if ((castType == typeof(double) || castType == typeof(float)) && cast.Expression.SystemType == typeof(decimal))
				return cast.Expression;

			return base.ConvertConversion(cast);
		}

		public override Clause ConvertSqlBinaryExpression(BinaryWord element)
		{
			if (element is BinaryWord bir && bir.Operation == "+" && bir.SystemType== typeof(string)) {
                return ConvertFunc(new(bir.SystemType, "Concat", bir.Expr1, bir.Expr2));

                static FunctionWord ConvertFunc(FunctionWord func)
                {
                    for (var i = 0; i < func.Parameters.Length; i++)
                    {
                        var param = func.Parameters[i];
                        if (param is BinaryWord binaryWord && binaryWord.Operation == "+" && binaryWord.SystemType == typeof(string))
                        {
                            var ps = new List<IExpWord>(func.Parameters);

                            ps.RemoveAt(i);
                            ps.Insert(i, binaryWord.Expr1);
                            ps.Insert(i + 1, binaryWord.Expr2);

                            return ConvertFunc(new(binaryWord.Type, func.Name,null, ps.ToArray()));
                        }

                        if (param is FunctionWord functionWord && functionWord.Name == "Concat")
                        {
                            var ps = new List<IExpWord>(func.Parameters);

                            ps.RemoveAt(i);
                            ps.InsertRange(i, functionWord.Parameters);

                            return ConvertFunc(new(functionWord.SystemType, func.Name, ps.ToArray()));
                        }

                    }

                    return func;
                }
            }


			return base.ConvertSqlBinaryExpression(element);
		}

		public override IAffirmWord ConvertSearchStringPredicate(SearchString predicate)
		{
			var caseSensitive = predicate.CaseSensitive.EvaluateBoolExpression(EvaluationContext);

			if (caseSensitive == null || caseSensitive == false)
			{
				var searchExpr = predicate.Expr2;
				var dataExpr   = predicate.Expr1;

#pragma warning disable CA1508 // https://github.com/dotnet/roslyn-analyzers/issues/6868
				if (caseSensitive == false)
#pragma warning restore CA1508
				{
					searchExpr = PseudoFunctions.MakeToLower(searchExpr);
					dataExpr   = PseudoFunctions.MakeToLower(dataExpr);
				}

				IAffirmWord? newPredicate = null;
				switch (predicate.Kind)
				{
					case SearchString.SearchKind.Contains:
					{
						newPredicate = new ExprExpr(
							new FunctionWord(typeof(int), "LOCATE", searchExpr, dataExpr), AffirmWord.Operator.Greater,
							new ValueWord(0), null);
						break;
					}
				}

				if (newPredicate != null)
				{
					newPredicate = newPredicate.MakeNot(predicate.IsNot);

					return newPredicate;
				}

#pragma warning disable CA1508 // https://github.com/dotnet/roslyn-analyzers/issues/6868
				if (caseSensitive == false)
#pragma warning restore CA1508
				{
					predicate = new SearchString(
						dataExpr,
						predicate.IsNot,
						searchExpr,
						predicate.Kind,
						new ValueWord(false));
				}
			}
			else
			{
				predicate = new SearchString(
					new ExpressionWord(typeof(string), $"{{0}} COLLATE utf8_bin", PrecedenceLv.Primary, predicate.Expr1),
					predicate.IsNot,
					predicate.Expr2,
					predicate.Kind,
					new ValueWord(false));
			}

			return ConvertSearchStringPredicateViaLike(predicate);
		}

	}
}
