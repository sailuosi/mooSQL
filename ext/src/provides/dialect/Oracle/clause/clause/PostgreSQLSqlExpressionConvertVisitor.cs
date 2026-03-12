namespace mooSQL.linq.DataProvider.PostgreSQL
{
	using Extensions;
    using mooSQL.data.model;
    using mooSQL.data.model.affirms;
    using mooSQL.utils;
    using SqlProvider;
	using SqlQuery;

	public class PostgreSQLSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public PostgreSQLSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		protected override bool SupportsNullInColumn       => false;

		public override IAffirmWord ConvertSearchStringPredicate(SearchString predicate)
		{
			var searchPredicate = ConvertSearchStringPredicateViaLike(predicate);

			if (false == predicate.CaseSensitive.EvaluateBoolExpression(EvaluationContext) && searchPredicate is Like likePredicate)
			{
				searchPredicate = new Like(likePredicate.Expr1, likePredicate.IsNot, likePredicate.Expr2, likePredicate.Escape, "ILIKE");
			}

			return searchPredicate;
		}

		public override Clause ConvertSqlBinaryExpression(BinaryWord element)
		{
			switch (element.Operation)
			{
				case "^": return new BinaryWord(element.SystemType, element.Expr1, "#", element.Expr2);
				case "+": return element.SystemType == typeof(string) ? new BinaryWord(element.SystemType, element.Expr1, "||", element.Expr2, element.Precedence) : element;
				case "%":
				{
					// PostgreSQL '%' operator supports only decimal and numeric types

					var fromType = QueryHelper.GetDbDataType(element.Expr1, DBLive);
					if (ReflectionExtensions.UnwrapNullable(fromType.SystemType) != typeof(decimal))
					{
						var toType          = DBLive.dialect.mapping .GetDbDataType(typeof(decimal));
						var newExpr1        = PseudoFunctions.MakeCast(element.Expr1, toType);
						var systemType      = typeof(decimal);
						if (fromType.SystemType.IsNullable())
							systemType = ReflectionExtensions.WrapNullable(systemType);

						var newExpr = PseudoFunctions.MakeMandatoryCast(new BinaryWord(systemType, newExpr1, element.Operation, element.Expr2), toType);
						return base.Visit(Optimize(newExpr));
					}
					break;
				}
			}

			return base.ConvertSqlBinaryExpression(element);
		}

		public override Clause ConvertSqlFunction(FunctionWord func)
		{
			if (func.Name == "CharIndex") {
				if (func.Parameters.Length == 2) {
                    return new ExpressionWord(func.SystemType, "Position({0} in {1})", PrecedenceLv.Primary, func.Parameters[0], func.Parameters[1]);
                }
				else if (func.Parameters.Length == 3)
                {
					var p0= func.Parameters[0];
					var p1= func.Parameters[1];
					var p2= func.Parameters[2];
                    return (Clause)Add<int>(
					new ExpressionWord(func.SystemType, "Position({0} in {1})", PrecedenceLv.Primary,
						p0,
						(IExpWord)Visit(
							new FunctionWord(typeof(string), "Substring",
								p1,
								p2,
								Sub<int>(
									(IExpWord)Visit(
										new FunctionWord(typeof(int), "Length", p1)),
									p2) as IExpWord)
						)),
					Sub(p2, 1) as IExpWord);
                }

			};
            return base.ConvertSqlFunction(func);
        }

		protected override IExpWord ConvertConversion(CastWord cast)
		{
			if (NullTypeExtensions.UnwrapNullable(cast.SystemType) == typeof(bool))
			{
				if (cast.Expression is not SearchConditionWord and not CaseWord)
				{
					return ConvertBooleanToCase(cast.Expression, cast.ToType);
				}
			}
			cast = FloorBeforeConvert(cast);
			return base.ConvertConversion(cast);
		}
	}
}
