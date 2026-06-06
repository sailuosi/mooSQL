using System;
using mooSQL.data.model;
using mooSQL.data.model.affirms;

namespace mooSQL.linq
{
	public static class ExtensionBuilderExtensions
	{
		public static DbFunc.SqlExtensionParam AddParameter(this DbFunc.ISqExtensionBuilder builder, string name, string value)
		{
			return builder.AddParameter(name, new ValueWord(value));
		}

		public static DbFunc.SqlExtensionParam AddExpression(this DbFunc.ISqExtensionBuilder builder, string name, string expr)
		{
			return builder.AddParameter(name, new ExpressionWord(expr, PrecedenceLv.Primary));
		}

		public static IExpWord Add(this DbFunc.ISqExtensionBuilder builder, IExpWord left, IExpWord right, Type type)
		{
			return new BinaryWord(type, left, "+", right, PrecedenceLv.Additive);
		}

		public static IExpWord Add<T>(this DbFunc.ISqExtensionBuilder builder, IExpWord left, IExpWord right)
		{
			return builder.Add(left, right, typeof(T));
		}

		public static IExpWord Add(this DbFunc.ISqExtensionBuilder builder, IExpWord left, int value)
		{
			return builder.Add<int>(left, new ValueWord(value));
		}

		public static IExpWord Inc(this DbFunc.ISqExtensionBuilder builder, IExpWord expr)
		{
			return builder.Add(expr, 1);
		}

		public static IExpWord Sub(this DbFunc.ISqExtensionBuilder builder, IExpWord left, IExpWord right, Type type)
		{
			return new BinaryWord(type, left, "-", right, PrecedenceLv.Subtraction);
		}

		public static IExpWord Sub<T>(this DbFunc.ISqExtensionBuilder builder, IExpWord left, IExpWord right)
		{
			return builder.Sub(left, right, typeof(T));
		}

		public static IExpWord Sub(this DbFunc.ISqExtensionBuilder builder, IExpWord left, int value)
		{
			return builder.Sub<int>(left, new ValueWord(value));
		}

		public static IExpWord Dec(this DbFunc.ISqExtensionBuilder builder, IExpWord expr)
		{
			return builder.Sub(expr, 1);
		}

		public static IExpWord Mul(this DbFunc.ISqExtensionBuilder builder, IExpWord left, IExpWord right, Type type)
		{
			return new BinaryWord(type, left, "*", right, PrecedenceLv.Multiplicative);
		}

		public static IExpWord Mul<T>(this DbFunc.ISqExtensionBuilder builder, IExpWord left, IExpWord right)
		{
			return builder.Mul(left, right, typeof(T));
		}

		public static IExpWord Mul(this DbFunc.ISqExtensionBuilder builder, IExpWord expr1, int value)
		{
			return builder.Mul<int>(expr1, new ValueWord(value));
		}

		public static IExpWord Div(this DbFunc.ISqExtensionBuilder builder, IExpWord expr1, IExpWord expr2, Type type)
		{
			return new BinaryWord(type, expr1, "/", expr2, PrecedenceLv.Multiplicative);
		}

		public static IExpWord Div<T>(this DbFunc.ISqExtensionBuilder builder, IExpWord expr1, IExpWord expr2)
		{
			return builder.Div(expr1, expr2, typeof(T));
		}

		public static IExpWord Div(this DbFunc.ISqExtensionBuilder builder, IExpWord expr1, int value)
		{
			return builder.Div<int>(expr1, new ValueWord(value));
		}
	}
}
