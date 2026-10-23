using System;

using mooSQL.data.model;
using mooSQL.linq.utils;
using mooSQL.linq.clause;

namespace mooSQL.linq.translator
{
	/// <summary>
	/// SqlExpressionFactoryExtensions type.
	/// </summary>
	/// <summary>
	/// SqlExpressionFactoryExtensions type.
	/// </summary>
	public static class SqlExpressionFactoryExtensions
	{
		/// <summary>
		/// Fragment
		/// </summary>
		/// <summary>
		/// Fragment
		/// </summary>
		public static IExpWord Fragment(this ISqlExpressionFactory factory, DbDataType dataType, string fragmentText, params IExpWord[] parameters)
		{
			return factory.Fragment(dataType, PrecedenceLv.Primary, fragmentText, parameters);
		}
		/// <summary>
		/// NonPureFragment
		/// </summary>

		public static IExpWord NonPureFragment(this ISqlExpressionFactory factory, DbDataType dataType, string fragmentText, params IExpWord[] parameters)
		{
			return new ExpressionWord(dataType.SystemType, fragmentText, PrecedenceLv.Primary, SqlFlags.None, ParametersNullabilityType.IfAnyParameterNullable, null, parameters);
		}
		/// <summary>
		/// Fragment
		/// </summary>

		public static IExpWord Fragment(this ISqlExpressionFactory factory, DbDataType dataType, int precedence, string fragmentText, params IExpWord[] parameters)
		{
			return new ExpressionWord(dataType.SystemType, fragmentText, precedence, SqlFlags.None, ParametersNullabilityType.Undefined, null, parameters);
		}
		/// <summary>
		/// Function
		/// </summary>

		public static IExpWord Function(this ISqlExpressionFactory factory, DbDataType dataType, string functionName, params IExpWord[] parameters)
		{
			return new FunctionWord(dataType, functionName,null, parameters);
		}
		/// <summary>
		/// FuncLikePredicate
		/// </summary>

		public static IAffirmWord FuncLikePredicate(this ISqlExpressionFactory factory, IExpWord function)
		{
			if (function is not FunctionWord func)
				throw new InvalidOperationException("Function must be of type FunctionWord.");

			return new data.model.affirms.FuncLike(func);
		}
		/// <summary>
		/// ExprPredicate
		/// </summary>

		public static IAffirmWord ExprPredicate(this ISqlExpressionFactory factory, IExpWord expression)
		{
			return new mooSQL.data.model.affirms.Expr(expression);
		}
		/// <summary>
		/// SearchCondition
		/// </summary>

		public static SearchConditionWord SearchCondition(this ISqlExpressionFactory factory, bool isOr = false)
		{
			return new SearchConditionWord(isOr);
		}
		/// <summary>
		/// NonPureFunction
		/// </summary>

		public static IExpWord NonPureFunction(this ISqlExpressionFactory factory, DbDataType dataType, string functionName, params IExpWord[] parameters)
		{
			return new FunctionWord(dataType, functionName, false, false, null,parameters);
		}
		/// <summary>
		/// Value&lt;T&gt;
		/// </summary>

		public static IExpWord Value<T>(this ISqlExpressionFactory factory, DbDataType dataType, T value)
		{
			return new ValueWord(dataType, value);
		}
		/// <summary>
		/// Value&lt;T&gt;
		/// </summary>

		public static IExpWord Value<T>(this ISqlExpressionFactory factory, T value)
		{
			return factory.Value(factory.GetDbDataType(typeof(T)), value);
		}
		/// <summary>
		/// Cast
		/// </summary>

		public static IExpWord Cast(this ISqlExpressionFactory factory, IExpWord expression, DbDataType toDbDataType, bool isMandatory = false)
		{
			return new CastWord(expression, toDbDataType, null, isMandatory);
		}
		/// <summary>
		/// Div
		/// </summary>

		public static IExpWord Div(this ISqlExpressionFactory factory, DbDataType dbDataType, IExpWord x, IExpWord y)
		{
			return new BinaryWord(dbDataType, x, "/", y, PrecedenceLv.Multiplicative);
		}
		/// <summary>
		/// Div&lt;T&gt;
		/// </summary>

		public static IExpWord Div<T>(this ISqlExpressionFactory factory, DbDataType dbDataType, IExpWord x, T value)
			where T : struct
		{
			return factory.Div(dbDataType, x, factory.Value(dbDataType, value));
		}
		/// <summary>
		/// Multiply
		/// </summary>

		public static IExpWord Multiply(this ISqlExpressionFactory factory, DbDataType dbDataType, IExpWord x, IExpWord y)
		{
			return new BinaryWord(dbDataType, x, "*", y, PrecedenceLv.Multiplicative);
		}
		/// <summary>
		/// Multiply&lt;T&gt;
		/// </summary>

		public static IExpWord Multiply<T>(this ISqlExpressionFactory factory, DbDataType dbDataType, IExpWord x, T value)
			where T : struct
		{
			return factory.Multiply(dbDataType, x, factory.Value(dbDataType, value));
		}
		/// <summary>
		/// Multiply&lt;T&gt;
		/// </summary>

		public static IExpWord Multiply<T>(this ISqlExpressionFactory factory, IExpWord x, T value)
			where T : struct
		{
			var dbDataType = factory.GetDbDataType(x);
			return factory.Multiply(dbDataType, x, factory.Value(dbDataType, value));
		}
		/// <summary>
		/// Sub
		/// </summary>

		public static IExpWord Sub(this ISqlExpressionFactory factory, DbDataType dbDataType, IExpWord x, IExpWord y)
		{
			return new BinaryWord(dbDataType, x, "-", y, PrecedenceLv.Additive);
		}
		/// <summary>
		/// Add
		/// </summary>

		public static IExpWord Add(this ISqlExpressionFactory factory, DbDataType dbDataType, IExpWord x, IExpWord y)
		{
			return new BinaryWord(dbDataType, x, "+", y, PrecedenceLv.Additive);
		}
		/// <summary>
		/// Binary
		/// </summary>

		public static IExpWord Binary(this ISqlExpressionFactory factory, DbDataType dbDataType, IExpWord x, string operation, IExpWord y)
		{
			return new BinaryWord(dbDataType, x, operation, y, PrecedenceLv.Additive);
		}
		/// <summary>
		/// Builds a string-concatenation expression.
		/// </summary>

		public static IExpWord Concat(this ISqlExpressionFactory factory, IExpWord x, IExpWord y)
		{
			var dbDataType = factory.GetDbDataType(x);
			return new BinaryWord(dbDataType, x, "+", y, PrecedenceLv.Additive);
		}
		/// <summary>
		/// Builds a string-concatenation expression.
		/// </summary>

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
		/// <summary>
		/// Condition
		/// </summary>

		public static IExpWord Condition(this ISqlExpressionFactory factory, IAffirmWord condition, IExpWord trueExpression, IExpWord falseExpression)
		{
			return new ConditionWord(condition, trueExpression, falseExpression);
		}
		/// <summary>
		/// Builds a string-concatenation expression.
		/// </summary>

		public static IExpWord Concat(this ISqlExpressionFactory factory, DbDataType dbDataType, IExpWord x, IExpWord y)
		{
			return new BinaryWord(dbDataType, x, "+", y, PrecedenceLv.Additive);
		}
		/// <summary>
		/// Builds a string-concatenation expression.
		/// </summary>

		public static IExpWord Concat(this ISqlExpressionFactory factory, DbDataType dbDataType, IExpWord x, string value)
		{
			return factory.Concat(dbDataType, x, factory.Value(dbDataType, value));
		}
		/// <summary>
		/// Builds a string-concatenation expression.
		/// </summary>

		public static IExpWord Concat(this ISqlExpressionFactory factory, IExpWord x, string value)
		{
			var dbDataType = factory.GetDbDataType(x);
			return new BinaryWord(dbDataType, x, "+", factory.Value(dbDataType, value), PrecedenceLv.Additive);
		}
		/// <summary>
		/// Increment&lt;T&gt;
		/// </summary>

		public static IExpWord Increment<T>(this ISqlExpressionFactory factory, IExpWord x, T value)
			where T : struct
		{
			var dbDataType = factory.GetDbDataType(x);
			return factory.Add(dbDataType, x, factory.Value(dbDataType, value));
		}
		/// <summary>
		/// Increment
		/// </summary>

		public static IExpWord Increment(this ISqlExpressionFactory factory, IExpWord x)
		{
			return factory.Increment(x, 1);
		}
		/// <summary>
		/// Decrement&lt;T&gt;
		/// </summary>

		public static IExpWord Decrement<T>(this ISqlExpressionFactory factory, IExpWord x, T value)
			where T : struct
		{
			var dbDataType = factory.GetDbDataType(x);
			return factory.Sub(dbDataType, x, factory.Value(dbDataType, value));
		}
		/// <summary>
		/// Decrement
		/// </summary>

		public static IExpWord Decrement(this ISqlExpressionFactory factory, IExpWord x)
		{
			return factory.Decrement(x, 1);
		}
		/// <summary>
		/// Mod
		/// </summary>

		public static IExpWord Mod(this ISqlExpressionFactory factory, IExpWord x, IExpWord value)
		{
			return new BinaryWord(factory.GetDbDataType(x).SystemType, x, "%", value);
		}
		/// <summary>
		/// Mod&lt;T&gt;
		/// </summary>

		public static IExpWord Mod<T>(this ISqlExpressionFactory factory, IExpWord x, T value)
			where T : struct
		{
			var dbDataType = factory.GetDbDataType(x);
			return factory.Mod(x, new ValueWord(dbDataType, value));
		}
		/// <summary>
		/// TypeExpression
		/// </summary>

		public static IExpWord TypeExpression(this ISqlExpressionFactory factory, DbDataType dbDataType)
		{
			return new DataTypeWord(dbDataType);
		}

		#region Predicates
		/// <summary>
		/// Greater
		/// </summary>

		public static IAffirmWord Greater(this ISqlExpressionFactory factory, IExpWord expr1, IExpWord expr2)
		{
			return new mooSQL.data.model.affirms.ExprExpr(expr1, AffirmWord.Operator.Greater, expr2, factory.DBLive.dialect.Option.CompareNullsAsValues ? false : null);
		}
		/// <summary>
		/// GreaterOrEqual
		/// </summary>

		public static IAffirmWord GreaterOrEqual(this ISqlExpressionFactory factory, IExpWord expr1, IExpWord expr2)
		{
			return new mooSQL.data.model.affirms.ExprExpr(expr1, AffirmWord.Operator.GreaterOrEqual, expr2, factory.DBLive.dialect.Option.CompareNullsAsValues ? true : null);
		}
		/// <summary>
		/// Less
		/// </summary>

		public static IAffirmWord Less(this ISqlExpressionFactory factory, IExpWord expr1, IExpWord expr2)
		{
			return new mooSQL.data.model.affirms.ExprExpr(expr1, AffirmWord.Operator.Less, expr2, factory.DBLive.dialect.Option.CompareNullsAsValues ? false : null);
		}
		/// <summary>
		/// LessOrEqual
		/// </summary>

		public static IAffirmWord LessOrEqual(this ISqlExpressionFactory factory, IExpWord expr1, IExpWord expr2)
		{
			return new mooSQL.data.model.affirms.ExprExpr(expr1, AffirmWord.Operator.LessOrEqual, expr2, factory.DBLive.dialect.Option.CompareNullsAsValues ? true : null);
		}

		#endregion
	}
}
