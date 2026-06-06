using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;



// ReSharper disable CheckNamespace
// ReSharper disable RedundantNameQualifier

namespace mooSQL.linq
{
	using Mapping;
	using Expressions;
	using Linq;
	using SqlQuery;
	using mooSQL.linq.Common;
	using mooSQL.data.model;
	using mooSQL.data.model.affirms;
	using mooSQL.data;

	public static partial class DbFunc
	{
		#region Common Functions

		/// <summary>
		/// Generates '*'.
		/// </summary>
		/// <returns></returns>
		[Expression("*", ServerSideOnly = true, CanBeNull = false, Precedence = PrecedenceLv.Primary)]
		public static object?[] AllColumns()
		{
			throw new LinqException("'AllColumns' is only server-side method.");
		}

		/// <summary>
		/// Generates 'DEFAULT' keyword, usable in inserts.
		/// </summary>
		[Expression("DEFAULT", ServerSideOnly = true)]
		public static T Default<T>() => throw new LinqException($"Default is only server-side method.");

		/// <summary>
		/// Enforces generating SQL even if an expression can be calculated locally.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj">Expression to generate SQL.</param>
		/// <returns>Returns 'obj'.</returns>
		[CLSCompliant(false)]
		[Expression("{0}", 0, ServerSideOnly = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static T AsSql<T>(T obj)
		{
			return obj;
		}

		[CLSCompliant(false)]
		[Expression("{0}", 0, ServerSideOnly = true, InlineParameters = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static T ToSql<T>(T obj)
		{
			return obj;
		}

		[Extension("{array, ', '}", ServerSideOnly = true)]
		internal static T[] Spread<T>([ExprParameter] T[] array)
		{
			throw new InvalidOperationException();
		}

		[CLSCompliant(false)]
		[Expression("{0}", 0, CanBeNull = true)]
		public static T AsNullable<T>(T value)
		{
			return value;
		}

		[CLSCompliant(false)]
		[Expression("{0}", 0, CanBeNull = false)]
		public static T AsNotNull<T>(T value)
		{
			return value;
		}

		[CLSCompliant(false)]
		[Expression("{0}", 0, CanBeNull = false)]
		public static T AsNotNullable<T>(T value)
		{
			return value;
		}

		[CLSCompliant(false)]
		[Expression("{0}", 0, IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static T? ToNullable<T>(T value)
			where T : struct
		{
			return value;
		}

		[CLSCompliant(false)]
		[Expression("{0}", 0, IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static T ToNotNull<T>(T? value)
			where T : struct
		{
			return value ?? default;
		}

		[CLSCompliant(false)]
		[Expression("{0}", 0, IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static T ToNotNullable<T>(T? value)
			where T : struct
		{
			return value ?? default;
		}

		[Extension(typeof(IsDistinctBuilder), ServerSideOnly = false, PreferServerSide = false)]
		public static bool IsDistinctFrom<T>(this T value, T other) => !EqualityComparer<T>.Default.Equals(value, other);

		[Extension(typeof(IsDistinctBuilder), ServerSideOnly = false, PreferServerSide = false)]
		public static bool IsDistinctFrom<T>(this T value, T? other) where T: struct => !EqualityComparer<T?>.Default.Equals(value, other);

		[Extension(typeof(IsDistinctBuilder), Expression = "NOT", ServerSideOnly = false, PreferServerSide = false)]
		public static bool IsNotDistinctFrom<T>(this T value, T other) => EqualityComparer<T>.Default.Equals(value, other);

		[Extension(typeof(IsDistinctBuilder), Expression= "NOT", ServerSideOnly = false, PreferServerSide = false)]
		public static bool IsNotDistinctFrom<T>(this T value, T? other) where T: struct => EqualityComparer<T?>.Default.Equals(value, other);

		sealed class IsDistinctBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var left  = builder.GetExpression(0)!;
				var right = builder.GetExpression(1)!;
				var isNot = builder.Expression == "NOT";

				var nullability = new NullabilityContext(builder.Query);

                AffirmWord predicate = left.CanBeNullable(nullability) || right.CanBeNullable(nullability)
					? new IsDistinct(left, isNot, right)
					: new mooSQL.data.model.affirms.ExprExpr(left, isNot ? AffirmWord.Operator.Equal : AffirmWord.Operator.NotEqual, right, withNull: null);

				builder.ResultExpression = new SearchConditionWord(false, predicate);
			}
		}

		/// <summary>
		/// Allows access to entity property via name. Property can be dynamic or non-dynamic.
		/// </summary>
		/// <typeparam name="T">Property type.</typeparam>
		/// <param name="entity">The entity.</param>
		/// <param name="propertyName">Name of the property.</param>
		/// <returns></returns>
		/// <exception cref="LinqException">'Property' is only server-side method.</exception>
		public static T Property<T>(object? entity, [SqlQueryDependent] string propertyName)
		{
			throw new LinqException("'Property' is only server-side method.");
		}

		/// <summary>
		/// Used internally for keeping Alias information with expression.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj"></param>
		/// <param name="alias"></param>
		/// <returns></returns>
		internal static T Alias<T>(T obj, [SqlQueryDependent] string alias)
		{
			return obj;
		}

		public static T? NullIf<T>(T? value, T? compareTo) where T : class
		{
			return value != null && compareTo != null && EqualityComparer<T>.Default.Equals(value, compareTo) ? null : value;
		}

		public static T? NullIf<T>(T? value, T compareTo) where T : struct
		{
			return value.HasValue && EqualityComparer<T>.Default.Equals(value.Value, compareTo) ? null : value;
		}

		public static T? NullIf<T>(T? value, T? compareTo) where T : struct
		{
			return value.HasValue && compareTo.HasValue && EqualityComparer<T>.Default.Equals(value.Value, compareTo.Value) ? null : value;
		}

		/// <summary>registry-first（Bootstrap <see cref="mooSQL.data.translation.DbFuncRegistry"/>）；无 <see cref="ExpressionAttribute"/>。</summary>
		public static T? Coalesce<T>(T? a, T b) where T : class => a ?? b;

		public static T Coalesce<T>(T? a, T b) where T : struct => a ?? b;

		/// <summary>registry-first（Bootstrap）；无 <see cref="ExtensionAttribute"/>。</summary>
		public static bool Between<T>(this T value, T low, T high)
			where T : IComparable
		{
			return value != null && value.CompareTo(low) >= 0 && value.CompareTo(high) <= 0;
		}

		/// <summary>registry-first（Bootstrap）；无 <see cref="ExtensionAttribute"/>。</summary>
		public static bool Between<T>(this T? value, T? low, T? high)
			where T : struct, IComparable
		{
			return value != null && value.Value.CompareTo(low) >= 0 && value.Value.CompareTo(high) <= 0;
		}

		/// <summary>registry-first（Bootstrap）；无 <see cref="ExtensionAttribute"/>。</summary>
		public static bool NotBetween<T>(this T value, T low, T high)
			where T : IComparable
		{
			return value != null && (value.CompareTo(low) < 0 || value.CompareTo(high) > 0);
		}

		/// <summary>registry-first（Bootstrap）；无 <see cref="ExtensionAttribute"/>。</summary>
		public static bool NotBetween<T>(this T? value, T? low, T? high)
			where T : struct, IComparable
		{
			return value != null && (value.Value.CompareTo(low) < 0 || value.Value.CompareTo(high) > 0);
		}

		#endregion
		#region Types

		public static class Types
		{
			public static bool     Bit => false;
			public static long     BigInt => 0;
			public static int      Int => 0;
			public static short    SmallInt => 0;
			public static byte     TinyInt => 0;
			public static decimal  DefaultDecimal => 0m;
			public static decimal  Decimal(int precision) => 0m;
			public static decimal  Decimal([SqlQueryDependent] int precision, [SqlQueryDependent] int scale) => 0m;
			public static decimal  Money => 0m;
			public static decimal  SmallMoney => 0m;
			public static double   Float => 0.0;
			public static float    Real => 0f;
			public static DateTime DateTime => DateTime.Now;
			public static DateTime DateTime2 => DateTime.Now;
			public static DateTime SmallDateTime => DateTime.Now;
			public static DateTime Date => DateTime.Now;
#if NET6_0_OR_GREATER
			public static DateOnly DateOnly => DateOnly.FromDateTime(DateTime.Now);
#endif
			public static DateTime Time => DateTime.Now;
			public static DateTimeOffset DateTimeOffset => DateTimeOffset.Now;
			public static string  Char(int length) => "";
			public static string  DefaultChar => "";
			public static string  VarChar(int length) => "";
			public static string  DefaultVarChar => "";
			public static string  NChar(int length) => "";
			public static string  DefaultNChar => "";
			public static string  NVarChar(int length) => "";
			public static string  DefaultNVarChar => "";
		}

		#endregion

		#region Ordinal

		/// <summary>
		/// Forces LINQ translator to generate column ordinal for <paramref name="expression"/> column (1-base column index in select statement).
		/// Currently it is supported only for <b>ORDER BY</b> clause.
		/// </summary>
		public static T Ordinal<T>(T expression) => expression;

		#endregion
	}
}
