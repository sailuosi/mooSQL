using System;
using mooSQL.data.model;
using mooSQL.data.model.affirms;

namespace mooSQL.linq
{
	/// <summary>
	/// ExtensionBuilderExtensions type.
	/// </summary>
	/// <summary>
	/// ExtensionBuilderExtensions type.
	/// </summary>
	public static class ExtensionBuilderExtensions
	{
		/// <summary>
		/// AddParameter
		/// </summary>
		/// <summary>
		/// AddParameter
		/// </summary>
		public static DbFunc.SqlExtensionParam AddParameter(this DbFunc.ISqExtensionBuilder builder, string name, string value)
		{
			return builder.AddParameter(name, new ValueWord(value));
		}
		/// <summary>
		/// AddExpression
		/// </summary>

		public static DbFunc.SqlExtensionParam AddExpression(this DbFunc.ISqExtensionBuilder builder, string name, string expr)
		{
			return builder.AddParameter(name, new ExpressionWord(expr, PrecedenceLv.Primary));
		}
		/// <summary>
		/// Add
		/// </summary>

		public static IExpWord Add(this DbFunc.ISqExtensionBuilder builder, IExpWord left, IExpWord right, Type type)
		{
			return new BinaryWord(type, left, "+", right, PrecedenceLv.Additive);
		}
		/// <summary>
		/// Add&lt;T&gt;
		/// </summary>

		public static IExpWord Add<T>(this DbFunc.ISqExtensionBuilder builder, IExpWord left, IExpWord right)
		{
			return builder.Add(left, right, typeof(T));
		}
		/// <summary>
		/// Add
		/// </summary>

		public static IExpWord Add(this DbFunc.ISqExtensionBuilder builder, IExpWord left, int value)
		{
			return builder.Add<int>(left, new ValueWord(value));
		}
		/// <summary>
		/// Inc
		/// </summary>

		public static IExpWord Inc(this DbFunc.ISqExtensionBuilder builder, IExpWord expr)
		{
			return builder.Add(expr, 1);
		}
		/// <summary>
		/// Sub
		/// </summary>

		public static IExpWord Sub(this DbFunc.ISqExtensionBuilder builder, IExpWord left, IExpWord right, Type type)
		{
			return new BinaryWord(type, left, "-", right, PrecedenceLv.Subtraction);
		}
		/// <summary>
		/// Sub&lt;T&gt;
		/// </summary>

		public static IExpWord Sub<T>(this DbFunc.ISqExtensionBuilder builder, IExpWord left, IExpWord right)
		{
			return builder.Sub(left, right, typeof(T));
		}
		/// <summary>
		/// Sub
		/// </summary>

		public static IExpWord Sub(this DbFunc.ISqExtensionBuilder builder, IExpWord left, int value)
		{
			return builder.Sub<int>(left, new ValueWord(value));
		}
		/// <summary>
		/// Dec
		/// </summary>

		public static IExpWord Dec(this DbFunc.ISqExtensionBuilder builder, IExpWord expr)
		{
			return builder.Sub(expr, 1);
		}
		/// <summary>
		/// Mul
		/// </summary>

		public static IExpWord Mul(this DbFunc.ISqExtensionBuilder builder, IExpWord left, IExpWord right, Type type)
		{
			return new BinaryWord(type, left, "*", right, PrecedenceLv.Multiplicative);
		}
		/// <summary>
		/// Mul&lt;T&gt;
		/// </summary>

		public static IExpWord Mul<T>(this DbFunc.ISqExtensionBuilder builder, IExpWord left, IExpWord right)
		{
			return builder.Mul(left, right, typeof(T));
		}
		/// <summary>
		/// Mul
		/// </summary>

		public static IExpWord Mul(this DbFunc.ISqExtensionBuilder builder, IExpWord expr1, int value)
		{
			return builder.Mul<int>(expr1, new ValueWord(value));
		}
		/// <summary>
		/// Div
		/// </summary>

		public static IExpWord Div(this DbFunc.ISqExtensionBuilder builder, IExpWord expr1, IExpWord expr2, Type type)
		{
			return new BinaryWord(type, expr1, "/", expr2, PrecedenceLv.Multiplicative);
		}
		/// <summary>
		/// Div&lt;T&gt;
		/// </summary>

		public static IExpWord Div<T>(this DbFunc.ISqExtensionBuilder builder, IExpWord expr1, IExpWord expr2)
		{
			return builder.Div(expr1, expr2, typeof(T));
		}
		/// <summary>
		/// Div
		/// </summary>

		public static IExpWord Div(this DbFunc.ISqExtensionBuilder builder, IExpWord expr1, int value)
		{
			return builder.Div<int>(expr1, new ValueWord(value));
		}
	}
}
