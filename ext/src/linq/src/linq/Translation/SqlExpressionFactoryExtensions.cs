using System;

using mooSQL.data.model;
using mooSQL.linq.Common;
using mooSQL.linq.SqlQuery;

namespace mooSQL.linq.Linq.Translation
{
	public static class SqlExpressionFactoryExtensions
	{
		public static IExpWord Fragment(this ISqlExpressionFactory factory, DbDataType dataType, string fragmentText, params IExpWord[] parameters)
		{
			return factory.Fragment(dataType, PrecedenceLv.Primary, fragmentText, parameters);
		}

		public static IExpWord NonPureFragment(this ISqlExpressionFactory factory, DbDataType dataType, string fragmentText, params IExpWord[] parameters)
		{
			return new ExpressionWord(dataType.SystemType, fragmentText, PrecedenceLv.Primary, SqlFlags.None, ParametersNullabilityType.IfAnyParameterNullable, null, parameters);
		}

		public static IExpWord Fragment(this ISqlExpressionFactory factory, DbDataType dataType, int precedence, string fragmentText, params IExpWord[] parameters)
		{
			return new ExpressionWord(dataType.SystemType, fragmentText, precedence, SqlFlags.None, ParametersNullabilityType.Undefined, null, parameters);
		}

		public static IExpWord Function(this ISqlExpressionFactory factory, DbDataType dataType, string functionName, params IExpWord[] parameters)
		{
			return new FunctionWord(dataType, functionName,null, parameters);
		}

		public static IAffirmWord FuncLikePredicate(this ISqlExpressionFactory factory, IExpWord function)
		{
			if (function is not FunctionWord func)
				throw new InvalidOperationException("Function must be of type FunctionWord.");

			return new data.model.affirms.FuncLike(func);
		}

		public static IAffirmWord ExprPredicate(this ISqlExpressionFactory factory, IExpWord expression)
		{
			return new mooSQL.data.model.affirms.Expr(expression);
		}

		public static SearchConditionWord SearchCondition(this ISqlExpressionFactory factory, bool isOr = false)
		{
			return new SearchConditionWord(isOr);
		}

		public static IExpWord NonPureFunction(this ISqlExpressionFactory factory, DbDataType dataType, string functionName, params IExpWord[] parameters)
		{
			return new FunctionWord(dataType, functionName, false, false, null,parameters);
		}

		public static IExpWord Value<T>(this ISqlExpressionFactory factory, DbDataType dataType, T value)
		{
			return new ValueWord(dataType, value);
		}

		public static IExpWord Value<T>(this ISqlExpressionFactory factory, T value)
		{
			return factory.Value(factory.GetDbDataType(typeof(T)), value);
		}

		public static IExpWord Cast(this ISqlExpressionFactory factory, IExpWord expression, DbDataType toDbDataType, bool isMandatory = false)
		{
			return new CastWord(expression, toDbDataType, null, isMandatory);
		}

		public static IExpWord Div(this ISqlExpressionFactory factory, DbDataType dbDataType, IExpWord x, IExpWord y)
		{
			return new BinaryWord(dbDataType, x, "/", y, PrecedenceLv.Multiplicative);
		}

		public static IExpWord Div<T>(this ISqlExpressionFactory factory, DbDataType dbDataType, IExpWord x, T value)
			where T : struct
		{
			return factory.Div(dbDataType, x, factory.Value(dbDataType, value));
		}

		public static IExpWord Multiply(this ISqlExpressionFactory factory, DbDataType dbDataType, IExpWord x, IExpWord y)
		{
			return new BinaryWord(dbDataType, x, "*", y, PrecedenceLv.Multiplicative);
		}

		public static IExpWord Multiply<T>(this ISqlExpressionFactory factory, DbDataType dbDataType, IExpWord x, T value)
			where T : struct
		{
			return factory.Multiply(dbDataType, x, factory.Value(dbDataType, value));
		}

		public static IExpWord Multiply<T>(this ISqlExpressionFactory factory, IExpWord x, T value)
			where T : struct
		{
			var dbDataType = factory.GetDbDataType(x);
			return factory.Multiply(dbDataType, x, factory.Value(dbDataType, value));
		}

		public static IExpWord Sub(this ISqlExpressionFactory factory, DbDataType dbDataType, IExpWord x, IExpWord y)
		{
			return new BinaryWord(dbDataType, x, "-", y, PrecedenceLv.Additive);
		}

		public static IExpWord Add(this ISqlExpressionFactory factory, DbDataType dbDataType, IExpWord x, IExpWord y)
		{
			return new BinaryWord(dbDataType, x, "+", y, PrecedenceLv.Additive);
		}

		public static IExpWord Binary(this ISqlExpressionFactory factory, DbDataType dbDataType, IExpWord x, string operation, IExpWord y)
		{
			return new BinaryWord(dbDataType, x, operation, y, PrecedenceLv.Additive);
		}

		public static IExpWord Concat(this ISqlExpressionFactory factory, IExpWord x, IExpWord y)
		{
			var dbDataType = factory.GetDbDataType(x);
			return new BinaryWord(dbDataType, x, "+", y, PrecedenceLv.Additive);
		}

		public static IExpWord Concat(this ISqlExpressionFactory factory, params IExpWord[] expressions)
		{
			if (expressions.Length == 0)
				throw new InvalidOperationException("At least one expression must be provided for concatenation.");

			var result     = expressions[0];
			var dbDataType = factory.GetDbDataType(result);

			for (var i = 1; i < expressions.Length; i++)
			{
				result = factory.Concat(dbDataType, result, expressions[i]);
			}
			
			return result;
		}

		public static IExpWord Condition(this ISqlExpressionFactory factory, IAffirmWord condition, IExpWord trueExpression, IExpWord falseExpression)
		{
			return new ConditionWord(condition, trueExpression, falseExpression);
		}

		public static IExpWord Concat(this ISqlExpressionFactory factory, DbDataType dbDataType, IExpWord x, IExpWord y)
		{
			return new BinaryWord(dbDataType, x, "+", y, PrecedenceLv.Additive);
		}

		public static IExpWord Concat(this ISqlExpressionFactory factory, DbDataType dbDataType, IExpWord x, string value)
		{
			return factory.Concat(dbDataType, x, factory.Value(dbDataType, value));
		}

		public static IExpWord Concat(this ISqlExpressionFactory factory, IExpWord x, string value)
		{
			var dbDataType = factory.GetDbDataType(x);
			return new BinaryWord(dbDataType, x, "+", factory.Value(dbDataType, value), PrecedenceLv.Additive);
		}

		public static IExpWord Increment<T>(this ISqlExpressionFactory factory, IExpWord x, T value)
			where T : struct
		{
			var dbDataType = factory.GetDbDataType(x);
			return factory.Add(dbDataType, x, factory.Value(dbDataType, value));
		}

		public static IExpWord Increment(this ISqlExpressionFactory factory, IExpWord x)
		{
			return factory.Increment(x, 1);
		}

		public static IExpWord Decrement<T>(this ISqlExpressionFactory factory, IExpWord x, T value)
			where T : struct
		{
			var dbDataType = factory.GetDbDataType(x);
			return factory.Sub(dbDataType, x, factory.Value(dbDataType, value));
		}

		public static IExpWord Decrement(this ISqlExpressionFactory factory, IExpWord x)
		{
			return factory.Decrement(x, 1);
		}

		public static IExpWord Mod(this ISqlExpressionFactory factory, IExpWord x, IExpWord value)
		{
			return new BinaryWord(factory.GetDbDataType(x).SystemType, x, "%", value);
		}

		public static IExpWord Mod<T>(this ISqlExpressionFactory factory, IExpWord x, T value)
			where T : struct
		{
			var dbDataType = factory.GetDbDataType(x);
			return factory.Mod(x, new ValueWord(dbDataType, value));
		}

		public static IExpWord TypeExpression(this ISqlExpressionFactory factory, DbDataType dbDataType)
		{
			return new DataTypeWord(dbDataType);
		}

		#region Predicates

		public static IAffirmWord Greater(this ISqlExpressionFactory factory, IExpWord expr1, IExpWord expr2)
		{
			return new mooSQL.data.model.affirms.ExprExpr(expr1, AffirmWord.Operator.Greater, expr2, factory.DBLive.dialect.Option.CompareNullsAsValues ? false : null);
		}

		public static IAffirmWord GreaterOrEqual(this ISqlExpressionFactory factory, IExpWord expr1, IExpWord expr2)
		{
			return new mooSQL.data.model.affirms.ExprExpr(expr1, AffirmWord.Operator.GreaterOrEqual, expr2, factory.DBLive.dialect.Option.CompareNullsAsValues ? true : null);
		}

		public static IAffirmWord Less(this ISqlExpressionFactory factory, IExpWord expr1, IExpWord expr2)
		{
			return new mooSQL.data.model.affirms.ExprExpr(expr1, AffirmWord.Operator.Less, expr2, factory.DBLive.dialect.Option.CompareNullsAsValues ? false : null);
		}

		public static IAffirmWord LessOrEqual(this ISqlExpressionFactory factory, IExpWord expr1, IExpWord expr2)
		{
			return new mooSQL.data.model.affirms.ExprExpr(expr1, AffirmWord.Operator.LessOrEqual, expr2, factory.DBLive.dialect.Option.CompareNullsAsValues ? true : null);
		}

		#endregion
	}
}
