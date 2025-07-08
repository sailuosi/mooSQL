using System;

namespace mooSQL.linq.DataProvider.Oracle
{
    using mooSQL.data.model;
    using mooSQL.data.model.affirms;
    using mooSQL.linq.Extensions;
    using mooSQL.utils;
    using SqlProvider;
    using SqlQuery;

	public class OracleSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public OracleSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		#region LIKE

		protected static string[] OracleLikeCharactersToEscape = {"%", "_"};

		public override string[] LikeCharactersToEscape => OracleLikeCharactersToEscape;

		#endregion

		public override ISQLNode ConvertExprExprPredicate(ExprExpr predicate)
		{
			var expr = predicate;

			// Oracle saves empty string as null to database, so we need predicate modification before sending query
			//
			if (expr is
				{
					WithNull: true,
					Operator: AffirmWord.Operator.Equal
						or AffirmWord.Operator.NotEqual
						or AffirmWord.Operator.GreaterOrEqual
						or AffirmWord.Operator.LessOrEqual,
				})
			{
				if (expr.Expr1.SystemType == typeof(string) &&
				    expr.Expr1.TryEvaluateExpression(EvaluationContext, out var value1) && value1 is string string1)
				{
					if (string1.Length == 0)
					{
						// Add 'AND [col] IS NOT NULL' when checking Not Equal to Empty String,
						// else add 'OR [col] IS NULL'

						if (expr.Operator == AffirmWord.Operator.NotEqual)
						{
							var sc = new SearchConditionWord(true,
								new ExprExpr(expr.Expr2, AffirmWord.Operator.NotEqual, expr.Expr1, null),
								new IsNull(expr.Expr2, true));

							return sc;
						}
						else
						{
							var sc = new SearchConditionWord(true,
								new ExprExpr(expr.Expr2, expr.Operator, expr.Expr1, null),
								new IsNull(expr.Expr2, false));

							return sc;
						}
					}
				}

				if (expr.Expr2.SystemType == typeof(string)                             &&
				    expr.Expr2.TryEvaluateExpression(EvaluationContext, out var value2) && value2 is string string2)
				{
					if (string2.Length == 0)
					{
						// Add 'AND [col] IS NOT NULL' when checking Not Equal to Empty String,
						// else add 'OR [col] IS NULL'

						if (expr.Operator == AffirmWord.Operator.NotEqual)
						{
							var sc = new SearchConditionWord(true, 
								new ExprExpr(expr.Expr1, AffirmWord.Operator.NotEqual, expr.Expr2, null),
								new IsNull(expr.Expr1, true));

							return sc;
						}
						else
						{
							var sc = new SearchConditionWord(true,
								new ExprExpr(expr.Expr1, expr.Operator, expr.Expr2, null),
								new IsNull(expr.Expr1, false));

							return sc;
						}
					}
				}
			}

			return base.ConvertExprExprPredicate(predicate);
		}

		public override Clause ConvertSqlBinaryExpression(BinaryWord element)
		{
			switch (element.Operation)
			{
				case "%": return new FunctionWord(element.SystemType, "MOD", element.Expr1, element.Expr2);
				case "&": return new FunctionWord(element.SystemType, "BITAND", element.Expr1, element.Expr2);
				case "|": // (a + b) - BITAND(a, b)
					return Sub(
						Add(element.Expr1, element.Expr2, element.SystemType),
						new FunctionWord(element.SystemType, "BITAND", element.Expr1, element.Expr2),
						element.SystemType);

				case "^": // (a + b) - BITAND(a, b) * 2
					return Sub(
						Add(element.Expr1, element.Expr2, element.SystemType),
						Mul(new FunctionWord(element.SystemType, "BITAND", element.Expr1, element.Expr2), 2),
						element.SystemType);
				case "+": return element.SystemType == typeof(string) ? new BinaryWord(element.SystemType, element.Expr1, "||", element.Expr2, element.Precedence) : element;
			}

			return base.ConvertSqlBinaryExpression(element);
		}

		public override Clause ConvertSqlExpression(ExpressionWord element)
		{
			if (element.Expr.StartsWith("To_Number(To_Char(") && element.Expr.EndsWith(", 'FF'))"))
				return Div(new ExpressionWord(element.SystemType, element.Expr.Replace("To_Number(To_Char(", "to_Number(To_Char("), element.Parameters), 1000);

			return base.ConvertSqlExpression(element);
		}

		public override Clause VisitFunctionWord(FunctionWord func)
		{


			if (func.Name == "Coalesce" && func.Parameters.Length == 2) {
				return ConvertCoalesceToBinaryFunc(func, "Nvl") as Clause;
			}
			else if(
					func.Name== "CharIndex" &&
					func.Parameters.Length==2
				){
				return new FunctionWord(func.SystemType, "InStr", func.Parameters[1], func.Parameters[0]);
			}
            else if (
				func.Name == "CharIndex" &&
				func.Parameters.Length == 3
			)
            {
                return new FunctionWord(func.SystemType, "InStr", func.Parameters[1], func.Parameters[0], func.Parameters[2]);
            }
			else { 
				return base.VisitFunctionWord(func);
			}
		}

		protected override IExpWord ConvertConversion(CastWord cast)
		{
			var ftype = cast.SystemType.UnwrapNullable();

			var toType   = cast.ToType;
			var argument = cast.Expression;

			if (ftype == typeof(bool) && ReferenceEquals(cast, IsForPredicate))
			{
				return ConvertToBooleanSearchCondition(cast.Expression);
			}

			if (ftype == typeof(DateTime) || ftype == typeof(DateTimeOffset)
#if NET6_0_OR_GREATER
				|| ftype == typeof(DateOnly)
#endif
			   )
			{
				if (IsTimeDataType(toType))
				{
					if (argument.SystemType == typeof(string))
						return argument;

					return new FunctionWord(cast.SystemType, "To_Char", argument, new ValueWord("HH24:MI:SS"));
				}

				if (IsDateDataType(toType, "Date"))
				{
					if (argument.SystemType!.UnwrapNullable() == typeof(DateTime)
						|| argument.SystemType!.UnwrapNullable() == typeof(DateTimeOffset))
					{
						return new FunctionWord(cast.SystemType, "Trunc", argument, new ValueWord("DD"));
					}

					return new FunctionWord(cast.SystemType, "TO_DATE", argument, new ValueWord("YYYY-MM-DD"));
				}
				else if (IsDateDataOffsetType(toType))
				{
					if (ftype == typeof(DateTimeOffset))
						return argument;

					return new FunctionWord(cast.SystemType, "TO_TIMESTAMP_TZ", argument, new ValueWord("YYYY-MM-DD HH24:MI:SS"));
				}

				return new FunctionWord(cast.SystemType, "TO_TIMESTAMP", argument, new ValueWord("YYYY-MM-DD HH24:MI:SS"));
			}
			else if (ftype == typeof(string))
			{
				var stype = argument.SystemType!.UnwrapNullable();

				if (stype == typeof(DateTimeOffset))
				{
					return new FunctionWord(cast.SystemType, "To_Char", argument, new ValueWord("YYYY-MM-DD HH24:MI:SS TZH:TZM"));
				}
				else if (stype == typeof(DateTime))
				{
					return new FunctionWord(cast.SystemType, "To_Char", argument, new ValueWord("YYYY-MM-DD HH24:MI:SS"));
				}
#if NET6_0_OR_GREATER
				else if (stype == typeof(DateOnly))
				{
					return new FunctionWord(cast.SystemType, "To_Char", argument, new ValueWord("YYYY-MM-DD"));
				}
#endif
			}

			return FloorBeforeConvert(cast);
		}
	}
}
