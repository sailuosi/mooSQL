using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;



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

	public static partial class Sql
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

		[Expression("NULLIF({0}, {1})", PreferServerSide = true)]
		[Expression(ProviderName.Access, "IIF({0} = {1}, null, {0})", PreferServerSide = false)]
		[Expression(ProviderName.SqlCe,  "CASE WHEN {0} = {1} THEN NULL ELSE {0} END", PreferServerSide = false)]
		public static T? NullIf<T>(T? value, T? compareTo) where T : class
		{
			return value != null && compareTo != null && EqualityComparer<T>.Default.Equals(value, compareTo) ? null : value;
		}

		[Expression("NULLIF({0}, {1})", PreferServerSide = true)]
		[Expression(ProviderName.Access, "IIF({0} = {1}, null, {0})", PreferServerSide = false)]
		[Expression(ProviderName.SqlCe,  "CASE WHEN {0} = {1} THEN NULL ELSE {0} END", PreferServerSide = false)]
		public static T? NullIf<T>(T? value, T compareTo) where T : struct
		{
			return value.HasValue && EqualityComparer<T>.Default.Equals(value.Value, compareTo) ? null : value;
		}

		[Expression("NULLIF({0}, {1})", PreferServerSide = true)]
		[Expression(ProviderName.Access, "IIF({0} = {1}, null, {0})", PreferServerSide = false)]
		[Expression(ProviderName.SqlCe,  "CASE WHEN {0} = {1} THEN NULL ELSE {0} END", PreferServerSide = false)]
		public static T? NullIf<T>(T? value, T? compareTo) where T : struct
		{
			return value.HasValue && compareTo.HasValue && EqualityComparer<T>.Default.Equals(value.Value, compareTo.Value) ? null : value;
		}
		#endregion

		#region NoConvert

#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
		[Function(PseudoFunctions.REMOVE_CONVERT, 0, 2, ServerSideOnly = true)]
#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant
		static TR ConvertRemover<T, TR>(T input)
		{
			throw new NotImplementedException();
		}

		sealed class NoConvertBuilder : IExtensionCallBuilder
		{
			private static readonly MethodInfo _method = MethodHelper.GetMethodInfo(ConvertRemover<int, int>, 0).GetGenericMethodDefinition();

			private static readonly TransformVisitor<object?> _transformer = TransformVisitor<object?>.Create(Transform);

			private static Expression Transform(Expression e)
			{
				if (e.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
				{
					var unary  = (UnaryExpression)e;
					var method = _method.MakeGenericMethod(unary.Operand.Type, unary.Type);
					return Expression.Call(null, method, unary.Operand);
				}

				return e;
			}

			public void Build(ISqExtensionBuilder builder)
			{
				var expr    = builder.Arguments[0];
				var newExpr = _transformer.Transform(expr);

				if (newExpr == expr)
				{
					builder.ResultExpression = builder.GetExpression(0);
					return;
				}

				var sqlExpr = builder.ConvertExpressionToSql(newExpr)  as Clause;
				sqlExpr = sqlExpr.Convert(static (v, e) =>
				{
					if (e is FunctionWord func && func.Name == PseudoFunctions.REMOVE_CONVERT)
						return func.Parameters[0] as Clause;
					return e;
				}) ;

				builder.ResultExpression = sqlExpr as IExpWord;
			}
		}

		[Extension("", BuilderType = typeof(NoConvertBuilder), ServerSideOnly = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static T NoConvert<T>(T expr)
		{
			return expr;
		}

		#endregion

		#region Guid Functions

		public static Guid NewGuid()
		{
			return Guid.NewGuid();
		}

		#endregion

		#region Convert Functions

		class ConvertBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var from = builder.GetExpression("from");
				var to = builder.GetExpression("to");

				if (from is null || to is null)
				{
					builder.IsConvertible = false;
					return;
				}

				var toDataType = QueryHelper.GetDbDataType(to, builder.DBLive);

				builder.ResultExpression = new	CastWord(from, toDataType, null, true);
			}
		}

		[CLSCompliant(false)]
		[Extension("", BuilderType = typeof(ConvertBuilder))]
		public static TTo Convert<TTo,TFrom>(TTo to, TFrom from)
		{
			return Common.ConvertTo<TTo>.From(from);
		}

		[CLSCompliant(false)]
		[Function(PseudoFunctions.CONVERT_FORMAT, 0, 3, 1, 2, ServerSideOnly = true, IsNullable = IsNullableType.SameAsSecondParameter)]
		public static TTo Convert<TTo, TFrom>(TTo to, TFrom from, int format)
		{
			return Common.ConvertTo<TTo>.From(from);
		}

		class ConvertBuilderSimple : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var obj = builder.GetExpression("obj")!;

				var toType     = ((MethodInfo)builder.Member).GetGenericArguments()[0];
				var toDataType = builder.DBLive.dialect.mapping.GetDbDataType(toType);

				builder.ResultExpression = new CastWord(obj, toDataType, null, true);
			}
		}

		[CLSCompliant(false)]
		[Extension("", BuilderType = typeof(ConvertBuilderSimple))]
		public static TTo Convert<TTo,TFrom>(TFrom obj)
		{
			return Common.ConvertTo<TTo>.From(obj);
		}

		class ConvertBuilderInner : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var obj = builder.GetExpression("obj", unwrap: true)!;

				var toType     = ((MethodInfo)builder.Member).ReturnType;
				var toDataType = builder.DBLive.dialect.mapping.GetDbDataType(toType);

				builder.ResultExpression = new CastWord(obj, toDataType, null, false);
			}
		}

		public static class ConvertTo<TTo>
		{
			[CLSCompliant(false)]
			[Extension("", BuilderType = typeof(ConvertBuilderInner))]
			public static TTo From<TFrom>(TFrom obj)
			{
				return Common.ConvertTo<TTo>.From(obj);
			}
		}

		[Expression("{0}", IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static TimeSpan? DateToTime(DateTime? date)
		{
			return date == null ? null : new TimeSpan(date.Value.Ticks);
		}

		/// <summary>
		/// Performs value conversion to specified type. If conversion failed, returns <c>null</c>.
		/// Supported databases:
		/// <list type="bullet">
		/// <item>SQL Server 2012 or newer</item>
		/// <item>Oracle 12.2 or newer (not all conversions possible, check Oracle's documentation on CAST expression)</item>
		/// </list>
		/// </summary>
		/// <typeparam name="TFrom">Source value type.</typeparam>
		/// <typeparam name="TTo">Target value type.</typeparam>
		/// <param name="value">Value to convert.</param>
		/// <param name="_">Unused. Added to support method overloads.</param>
		/// <returns>Value, converted to target type or <c>null</c> if conversion failed.</returns>
		[CLSCompliant(false)]
		[Function(PseudoFunctions.TRY_CONVERT, 3, 2, 0, ServerSideOnly = true, IsPure = true, IsNullable = IsNullableType.Nullable)]
		public static TTo? TryConvert<TFrom, TTo>(TFrom value, TTo? _) where TTo : struct => throw new LinqException($"'{nameof(TryConvert)}' is only server-side method.");

		/// <summary>
		/// Performs value conversion to specified type. If conversion failed, returns <c>null</c>.
		/// Supported databases:
		/// <list type="bullet">
		/// <item>SQL Server 2012 or newer</item>
		/// <item>Oracle 12.2 or newer (not all conversions possible, check Oracle's documentation on CAST expression)</item>
		/// </list>
		/// </summary>
		/// <typeparam name="TFrom">Source value type.</typeparam>
		/// <typeparam name="TTo">Target value type.</typeparam>
		/// <param name="value">Value to convert.</param>
		/// <param name="_">Unused. Added to support method overloads.</param>
		/// <returns>Value, converted to target type or <c>null</c> if conversion failed.</returns>
		[CLSCompliant(false)]
		[Function(PseudoFunctions.TRY_CONVERT, 3, 2, 0, ServerSideOnly = true, IsPure = true, IsNullable = IsNullableType.Nullable)]
		public static TTo? TryConvert<TFrom, TTo>(TFrom value, TTo? _) where TTo : class => throw new LinqException($"'{nameof(TryConvert)}' is only server-side method.");

		/// <summary>
		/// Performs value conversion to specified type. If conversion failed, returns value, specified by <paramref name="defaultValue"/> parameter.
		/// Supported databases:
		/// <list type="bullet">
		/// <item>Oracle 12.2 or newer (not all conversions possible, check Oracle's documentation on CAST expression)</item>
		/// </list>
		/// </summary>
		/// <typeparam name="TFrom">Source value type.</typeparam>
		/// <typeparam name="TTo">Target value type.</typeparam>
		/// <param name="value">Value to convert.</param>
		/// <param name="defaultValue">Value, returned when conversion failed.</param>
		/// <returns>Value, converted to target type or <paramref name="defaultValue"/> if conversion failed.</returns>
		[CLSCompliant(false)]
		[Function(PseudoFunctions.TRY_CONVERT_OR_DEFAULT, 3, 2, 0, 1, ServerSideOnly = true, IsPure = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static TTo? TryConvertOrDefault<TFrom, TTo>(TFrom value, TTo? defaultValue) where TTo : struct => throw new LinqException($"'{nameof(TryConvertOrDefault)}' is only server-side method.");

		/// <summary>
		/// Performs value conversion to specified type. If conversion failed, returns value, specified by <paramref name="defaultValue"/> parameter.
		/// Supported databases:
		/// <list type="bullet">
		/// <item>Oracle 12.2 or newer (not all conversions possible, check Oracle's documentation on CAST expression)</item>
		/// </list>
		/// </summary>
		/// <typeparam name="TFrom">Source value type.</typeparam>
		/// <typeparam name="TTo">Target value type.</typeparam>
		/// <param name="value">Value to convert.</param>
		/// <param name="defaultValue">Value, returned when conversion failed.</param>
		/// <returns>Value, converted to target type or <paramref name="defaultValue"/> if conversion failed.</returns>
		[CLSCompliant(false)]
		[Function(PseudoFunctions.TRY_CONVERT_OR_DEFAULT, 3, 2, 0, 1, ServerSideOnly = true, IsPure = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static TTo? TryConvertOrDefault<TFrom, TTo>(TFrom value, TTo? defaultValue) where TTo : class => throw new LinqException($"'{nameof(TryConvertOrDefault)}' is only server-side method.");
		#endregion

		#region String Functions

		[Function  (                                                    PreferServerSide = true, IsNullable = IsNullableType.SameAsFirstParameter)]
		[Function  (ProviderName.Access,     "Len",                               PreferServerSide = true, IsNullable = IsNullableType.SameAsFirstParameter)]
		[Function  (ProviderName.Firebird,   "Char_Length",                       PreferServerSide = true, IsNullable = IsNullableType.SameAsFirstParameter)]
		[Function  (ProviderName.SqlServer,  "Len",                               PreferServerSide = true, IsNullable = IsNullableType.SameAsFirstParameter)]
		[Function  (ProviderName.SqlCe,      "Len",                               PreferServerSide = true, IsNullable = IsNullableType.SameAsFirstParameter)]
		[Function  (ProviderName.Sybase,     "Len",                               PreferServerSide = true, IsNullable = IsNullableType.SameAsFirstParameter)]
		[Function  (ProviderName.MySql,      "Char_Length",                       PreferServerSide = true, IsNullable = IsNullableType.SameAsFirstParameter)]
		[Function  (ProviderName.Informix,   "CHAR_LENGTH",                       PreferServerSide = true, IsNullable = IsNullableType.SameAsFirstParameter)]
		[Function  (ProviderName.ClickHouse, "CHAR_LENGTH",                       PreferServerSide = true, IsNullable = IsNullableType.SameAsFirstParameter)]
		[Expression(ProviderName.DB2LUW,     "CHARACTER_LENGTH({0},CODEUNITS32)", PreferServerSide = true, IsNullable = IsNullableType.SameAsFirstParameter)]
		public static int? Length(string? str)
		{
			return str?.Length;
		}

		[Function  (                                                PreferServerSide = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function  (ProviderName.Access,   "Mid",                             PreferServerSide = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function  (ProviderName.DB2,      "Substr",                          PreferServerSide = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function  (ProviderName.Informix, "Substr",                          PreferServerSide = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function  (ProviderName.Oracle,   "Substr",                          PreferServerSide = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function  (ProviderName.SQLite,   "Substr",                          PreferServerSide = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.Firebird, "Substring({0} from {1} for {2})", PreferServerSide = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static string? Substring(string? str, int? start, int? length)
		{
			if (str == null || start == null || length == null) return null;
			if (start.Value < 1 || start.Value > str.Length) return null;
			if (length.Value < 0) return null;

			var index = start.Value - 1;
			var maxAllowedLength = Math.Min(str.Length - index, length.Value);

			return str.Substring(index, maxAllowedLength);
		}

		[Function(ServerSideOnly = true, IsPredicate = true)]
		public static bool Like(string? matchExpression, string? pattern)
		{
#if !NETFRAMEWORK
			throw new InvalidOperationException();
#else
			return matchExpression != null && pattern != null &&
				System.Data.Linq.SqlClient.SqlMethods.Like(matchExpression, pattern);
#endif
		}

		[Function(ServerSideOnly = true, IsPredicate = true)]
		public static bool Like(string? matchExpression, string? pattern, char? escapeCharacter)
		{
#if !NETFRAMEWORK
			throw new InvalidOperationException();
#else
			return matchExpression != null && pattern != null && escapeCharacter != null &&
				System.Data.Linq.SqlClient.SqlMethods.Like(matchExpression, pattern, escapeCharacter.Value);
#endif
		}

		[CLSCompliant(false)]
		[Function(                                     IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.DB2,        "Locate",             IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.MySql,      "Locate",             IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.SapHana,    "Locate",       1, 0, IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.Firebird,   "Position",           IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.ClickHouse, "positionUTF8", 1, 0, IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static int? CharIndex(string? substring, string? str)
		{
			if (str == null || substring == null) return null;

			// Database CharIndex returns:
			//  1-based position, when sequence is found
			//  0 when substring is empty
			//  0 when substring is not found

			// IndexOf returns:
			//  0 when substring is empty <= this needs to handled special way to mimic behavior.
			//  -1 when substring is not found

			return substring.Length == 0 ? 0 : str.IndexOf(substring) + 1;
		}

		[Function(                                                             IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function  (ProviderName.DB2,        "Locate",                                   IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function  (ProviderName.MySql,      "Locate",                                   IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function  (ProviderName.Firebird,   "Position",                                 IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.ClickHouse, "positionUTF8({1}, {0}, toUInt32({2}))",    IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.SapHana,    "Locate(Substring({1},{2} + 1),{0}) + {2}", IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static int? CharIndex(string? substring, string? str, int? start)
		{
			if (str == null || substring == null || start == null) return null;

			var index = start.Value < 1 ? 0 : start.Value > str.Length ? str.Length - 1 : start.Value - 1;
			return substring.Length == 0 ? 0 : str.IndexOf(substring, index) + 1;
		}

		[Function(                                     IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.DB2,        "Locate",             IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.MySql,      "Locate",             IsNullable = IsNullableType.IfAnyParameterNullable)]
#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
		[Function(ProviderName.SapHana,    "Locate",       1, 0, IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.ClickHouse, "positionUTF8", 1, 0, IsNullable = IsNullableType.IfAnyParameterNullable)]
#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant
		[Function(ProviderName.Firebird,   "Position",           IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static int? CharIndex(char? value, string? str)
		{
			if (value == null || str == null) return null;

			return str.IndexOf(value.Value) + 1;
		}

		[Function(                                                          IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.DB2,        "Locate",                                  IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.MySql,      "Locate",                                  IsNullable = IsNullableType.IfAnyParameterNullable)]
#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
		[Function(ProviderName.SapHana,    "Locate",       1, 0, 2,                   IsNullable = IsNullableType.IfAnyParameterNullable)]
#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant
		[Expression(ProviderName.ClickHouse, "positionUTF8({1}, {0}, toUInt32({2}))", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.Firebird,   "Position",                                IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static int? CharIndex(char? value, string? str, int? start)
		{
			if (str == null || value == null || start == null) return null;

			var index = start.Value < 1 ? 0 : start.Value > str.Length ? str.Length - 1 : start.Value - 1;
			return str.IndexOf(value.Value, index) + 1;
		}

		[Function(                              IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.ClickHouse, "reverseUTF8", IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static string? Reverse(string? str)
		{
			if (string.IsNullOrEmpty(str)) return str;

			var chars = str!.ToCharArray();
			Array.Reverse(chars);
			return new string(chars);
		}

		[Function(                           PreferServerSide = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.SQLite,     "LeftStr",  PreferServerSide = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.ClickHouse, "leftUTF8", PreferServerSide = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static string? Left(string? str, int? length)
		{
			if (length == null || str == null) return null;
			if (length.Value < 0)              return null;
			if (length.Value > str.Length)     return str;

			return str.Substring(0, length.Value);
		}

		class OracleRightBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var stringExpr = builder.GetExpression(0);
				var lengthExpr = builder.GetExpression(1);

				if (stringExpr == null || lengthExpr == null)
				{
					builder.IsConvertible = false;
					return;
				}

				lengthExpr = new BinaryWord(lengthExpr.SystemType!, new ValueWord(-1), "*", lengthExpr, PrecedenceLv.Multiplicative);

				builder.ResultExpression = new FunctionWord(stringExpr.SystemType!, "substr", false, true, stringExpr, lengthExpr);
			}
		}

		class SqlCeRightBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var stringExpr = builder.GetExpression(0);
				var lengthExpr = builder.GetExpression(1);

				if (stringExpr == null || lengthExpr == null)
				{
					builder.IsConvertible = false;
					return;
				}

				// SUBSTRING(someStr, LEN(someStr) - (len - 1), len)

				var startExpr = new BinaryWord(lengthExpr.SystemType!,
					new FunctionWord(lengthExpr.SystemType!, "LEN", stringExpr), "-",
					new BinaryWord(lengthExpr.SystemType!, lengthExpr, "-", new ValueWord(1), PrecedenceLv.Subtraction),
                    PrecedenceLv.Subtraction);

				builder.ResultExpression = new FunctionWord(stringExpr.SystemType!, "SUBSTRING", false, true, stringExpr, startExpr, lengthExpr);
			}
		}

		[Function("RIGHT",                    PreferServerSide = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.SQLite,     "RightStr",  PreferServerSide = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.ClickHouse, "rightUTF8", PreferServerSide = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Extension(ProviderName.Oracle,    "",          PreferServerSide = true, IsNullable = IsNullableType.IfAnyParameterNullable, BuilderType = typeof(OracleRightBuilder))]
		[Extension(ProviderName.SqlCe,     "",          PreferServerSide = true, IsNullable = IsNullableType.IfAnyParameterNullable, BuilderType = typeof(SqlCeRightBuilder))]
		public static string? Right(string? str, int? length)
		{
			if (length == null || str == null) return null;
			if (length.Value < 0)              return null;
			if (length.Value > str.Length)     return str;

			return str.Substring(str.Length - length.Value);
		}

		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.ClickHouse, "concat(substringUTF8({0}, 1, {1} - 1), {3}, substringUTF8({0}, {1} + {2}))", PreferServerSide = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static string? Stuff(string? str, int? start, int? length, string? newString)
		{
			if (str == null || start == null || length == null || newString == null) return null;
			if (start.Value < 1 || start.Value > str.Length)                         return null;
			if (length.Value < 0)                                                    return null;

			var index = start.Value - 1;
			var maxAllowedLength = Math.Min(str.Length - index, length.Value);

			return str.Remove(index, maxAllowedLength).Insert(index, newString);
		}

		[Function(ServerSideOnly = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.ClickHouse, "concat(substringUTF8({0}, 1, {1} - 1), {3}, substringUTF8({0}, {1} + {2}))", PreferServerSide = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static string Stuff(IEnumerable<string> characterExpression, int? start, int? length, string replaceWithExpression)
		{
			throw new NotImplementedException();
		}

		[Function(                                                        IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.SapHana,    "Lpad('',{0},' ')",                    IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.ClickHouse, "leftPadUTF8('', toUInt32({0}), ' ')", IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static string? Space(int? length)
		{
			return length == null || length.Value < 0 ? null : "".PadRight(length.Value);
		}

		[Function(               Name = "LPad",                            IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.ClickHouse, "leftPadUTF8({0}, toUInt32({1}), {2})", IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static string? PadLeft(string? str, int? length, char? paddingChar)
		{
			if (str == null || length == null || paddingChar == null) return null;
			if (length.Value < 0)                                     return null;
			if (length.Value <= str.Length)                           return str.Substring(0, length.Value);

			return str.PadLeft(length.Value, paddingChar.Value);
		}

		[Function(               Name = "RPad",         IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.ClickHouse, "rightPadUTF8({0}, toUInt32({1}), {2})", IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static string? PadRight(string? str, int? length, char? paddingChar)
		{
			if (str == null || length == null || paddingChar == null) return null;
			if (length.Value < 0) return null;
			if (length.Value <= str.Length) return str.Substring(0, length.Value);

			return str.PadRight(length.Value, paddingChar.Value);
		}

		[Function(PseudoFunctions.REPLACE, IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static string? Replace(string? str, string? oldValue, string? newValue)
		{
			if (str == null || oldValue == null || newValue == null) return null;
			if (str.Length == 0)                                     return str;
			if (oldValue.Length == 0)                                return str; // Replace raises exception here.

			return str.Replace(oldValue, newValue);
		}

		[Function(                              IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.Sybase,     "Str_Replace", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.ClickHouse, "replaceAll",  IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static string? Replace(string? str, char? oldValue, char? newValue)
		{
			if (str == null || oldValue == null || newValue == null) return null;
			if (str.Length == 0)                                     return str;

			return str.Replace(oldValue.Value, newValue.Value);
		}

		#region IsNullOrWhiteSpace
		// set of all White_Space characters per Unicode v13
		const string WHITESPACES       = "\x09\x0A\x0B\x0C\x0D\x20\x85\xA0\x1680\x2000\x2001\x2002\x2003\x2004\x2005\x2006\x2007\x2008\x2009\x200A\x2028\x2029\x205F\x3000";
		const string ASCII_WHITESPACES = "\x09\x0A\x0B\x0C\x0D\x20\x85\xA0";
		const string WHITESPACES_REGEX = "\x09|\x0A|\x0B|\x0C|\x0D|\x20|\x85|\xA0|\x1680|\x2000|\x2001|\x2002|\x2003|\x2004|\x2005|\x2006|\x2007|\x2008|\x2009|\x200A|\x2028|\x2029|\x205F|\x3000";

		/*
		 * marked internal as we don't have plans now to expose it directly (used by string.IsNullOrWhiteSpace mapping)
		 *
		 * implementation tries to mimic .NET implementation of string.IsNullOrWhiteSpace (except null check part):
		 * return true if string doesn't contain any symbols except White_Space codepoints from Unicode.
		 *
		 * Known limitations:
		 * 1. [Access] we handle only following WS:
		 * - 0x20 (SPACE)
		 * - 0x1680 (OGHAM SPACE MARK)
		 * - 0x205F (MEDIUM MATHEMATICAL SPACE)
		 * - 0x3000 (IDEOGRAPHIC SPACE)
		 * Proper implementation will be same as we use for SqlCe, but Replace function is not exposed to SQL by default
		 * and requires sandbox mode: https://support.microsoft.com/en-us/office/turn-sandbox-mode-on-or-off-to-disable-macros-8cc7bad8-38c2-4a7a-a604-43e9a7bbc4fb
		 * 2. [Informix} implementation use only ASCII whitespaces which probably will not work in some cases for WS outside of
		 * ASCII range (currently works in our tests, but it could be that it depends on used encodings)
		 */
		[Extension(                  typeof(IsNullOrWhiteSpaceDefaultBuilder),                     IsPredicate = true)]
		[Extension(ProviderName.Oracle,        typeof(IsNullOrWhiteSpaceOracleBuilder),                      IsPredicate = true)]
		[Extension(ProviderName.Informix,      typeof(IsNullOrWhiteSpaceInformixBuilder),                    IsPredicate = true)]
		[Extension(ProviderName.SqlServer,     typeof(IsNullOrWhiteSpaceSqlServerBuilder),                   IsPredicate = true)]
		[Extension(ProviderName.SqlServer2017, typeof(IsNullOrWhiteSpaceSqlServer2017Builder),               IsPredicate = true)]
		[Extension(ProviderName.SqlServer2019, typeof(IsNullOrWhiteSpaceSqlServer2017Builder),               IsPredicate = true)]
		[Extension(ProviderName.SqlServer2022, typeof(IsNullOrWhiteSpaceSqlServer2017Builder),               IsPredicate = true)]
		[Extension(ProviderName.Access,        typeof(IsNullOrWhiteSpaceAccessBuilder),                      IsPredicate = true)]
		[Extension(ProviderName.Sybase,        typeof(IsNullOrWhiteSpaceSybaseBuilder),                      IsPredicate = true)]
		[Extension(ProviderName.MySql,         typeof(IsNullOrWhiteSpaceMySqlBuilder),                       IsPredicate = true)]
		[Extension(ProviderName.Firebird,      typeof(IsNullOrWhiteSpaceFirebirdBuilder),                    IsPredicate = true)]
		[Extension(ProviderName.SqlCe,         typeof(IsNullOrWhiteSpaceSqlCeBuilder),                       IsPredicate = true)]
		[Expression(ProviderName.ClickHouse, $"empty(replaceRegexpAll(coalesce({{0}}, ''), '{WHITESPACES_REGEX}', ''))", IsPredicate = true)]
		internal static bool IsNullOrWhiteSpace(string? str) => string.IsNullOrWhiteSpace(str);

		// str IS NULL OR REPLACE...(str, WHITEPACES, '') == ''
		internal sealed class IsNullOrWhiteSpaceSqlCeBuilder : IExtensionCallBuilder
		{
			void IExtensionCallBuilder.Build(ISqExtensionBuilder builder)
			{
				var str = builder.GetExpression("str")!;

				var predicate = new mooSQL.data.model.affirms.ExprExpr(
						new ExpressionWord(
							typeof(string),
							"REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE({0}, '\x09', ''), '\x0a', ''), '\x0b', ''), '\x0c', ''), '\x0d', ''), '\x20', ''), '\x85', ''), '\xa0', ''), '\x1680', ''), '\x2000', ''), '\x2001', ''), '\x2002', ''), '\x2003', ''), '\x2004', ''), '\x2005', ''), '\x2006', ''), '\x2007', ''), '\x2008', ''), '\x2009', ''), '\x200a', ''), '\x2028', ''), '\x2029', ''), '\x205f', ''), '\x3000', '')",
							str),
                        AffirmWord.Operator.Equal,
						new ValueWord(typeof(string), string.Empty), false);

				var nullability = new NullabilityContext(builder.Query);
				if (str.CanBeNullable(nullability))
					builder.ResultExpression = new SearchConditionWord(true, 
						new IsNull(str, false),
						predicate);
				else
					builder.ResultExpression = new SearchConditionWord(false, predicate);
			}
		}

		// str IS NULL OR NOT(str SIMILAR TO _utf8 x'%[^WHITESPACES_UTF8]%')
		internal sealed class IsNullOrWhiteSpaceFirebirdBuilder : IExtensionCallBuilder
		{
			void IExtensionCallBuilder.Build(ISqExtensionBuilder builder)
			{
				var str = builder.GetExpression("str")!;

				const string whiteSpaces = $"%[^{WHITESPACES}]%";
				var predicate = new mooSQL.data.model.affirms.Expr(
					new ExpressionWord(
						typeof(bool),
						"{0} SIMILAR TO {1}",
                        PrecedenceLv.Comparison,
                        SqlFlags.IsPredicate,
                        ParametersNullabilityType.NotNullable,
						null,
						str,
						new ValueWord(typeof(string), whiteSpaces)))
					.MakeNot();

				var nullability = new NullabilityContext(builder.Query);
				if (str.CanBeNullable(nullability))
					builder.ResultExpression = new SearchConditionWord(true,
						new IsNull(str, false), predicate);
				else
					builder.ResultExpression = new SearchConditionWord(false, predicate);
			}
		}

		// str IS NULL OR NOT(str RLIKE '%[^WHITESPACES]%')
		internal sealed class IsNullOrWhiteSpaceMySqlBuilder : IExtensionCallBuilder
		{
			void IExtensionCallBuilder.Build(ISqExtensionBuilder builder)
			{
				var str = builder.GetExpression("str")!;

				var whiteSpaces = $"[^{WHITESPACES}]";
				var condition = new mooSQL.data.model.affirms.Expr(
					new ExpressionWord(
						typeof(bool),
						"{0} RLIKE {1}",
                        PrecedenceLv.Comparison,
                        SqlFlags.IsPredicate,
                        ParametersNullabilityType.NotNullable,
						null,
						str,
						new ValueWord(typeof(string), whiteSpaces)))
					.MakeNot();

				var nullability = new NullabilityContext(builder.Query);
				if (str.CanBeNullable(nullability))
					builder.ResultExpression = new SearchConditionWord(true,
						new IsNull(str, false), condition);
				else
					builder.ResultExpression = new SearchConditionWord(false, condition);
			}
		}

		// str IS NULL OR str NOT LIKE '%[^WHITESPACES]%'
		internal sealed class IsNullOrWhiteSpaceSybaseBuilder : IExtensionCallBuilder
		{
			void IExtensionCallBuilder.Build(ISqExtensionBuilder builder)
			{
				var str = builder.GetExpression("str")!;

				var whiteSpaces = $"%[^{WHITESPACES}]%";
				var predicate = new mooSQL.data.model.affirms.Like(
					str,
					true,
					new ValueWord(typeof(string), whiteSpaces),
					null);

				var nullability = new NullabilityContext(builder.Query);
				if (str.CanBeNullable(nullability))
					builder.ResultExpression = new SearchConditionWord(true,
						new IsNull(str, false), predicate);
				else
					builder.ResultExpression = new SearchConditionWord(false, predicate);
			}
		}

		// str IS NULL OR str NOT LIKE N'%[^WHITESPACES]%'
		internal sealed class IsNullOrWhiteSpaceSqlServerBuilder : IExtensionCallBuilder
		{
			void IExtensionCallBuilder.Build(ISqExtensionBuilder builder)
			{
				var str = builder.GetExpression("str")!;

				var whiteSpaces = $"%[^{WHITESPACES}]%";
				var predicate = new mooSQL.data.model.affirms.Like(
					str,
					true,
					new ValueWord(new DbDataType(typeof(string), DataFam.NVarChar), whiteSpaces),
					null);

				var nullability = new NullabilityContext(builder.Query);
				if (str.CanBeNullable(nullability))
					builder.ResultExpression = new SearchConditionWord(true,
						new IsNull(str, false),
						predicate);
				else
					builder.ResultExpression = new SearchConditionWord(false, predicate);
			}
		}

		// str IS NULL OR LTRIM(str, '') = ''
		internal sealed class IsNullOrWhiteSpaceAccessBuilder : IExtensionCallBuilder
		{
			void IExtensionCallBuilder.Build(ISqExtensionBuilder builder)
			{
				var str = builder.GetExpression("str")!;

				var predicate = new mooSQL.data.model.affirms.ExprExpr(
						new FunctionWord(typeof(string), "LTRIM", str),
                        AffirmWord.Operator.Equal,
						new ValueWord(typeof(string), string.Empty), false);

				var nullability = new NullabilityContext(builder.Query);
				if (str.CanBeNullable(nullability))
					builder.ResultExpression = new SearchConditionWord(true,
						new IsNull(str, false),
						predicate);
				else
					builder.ResultExpression = new SearchConditionWord(false, predicate);
			}
		}

		// str IS NULL OR TRIM(N'WHITESPACES FROM str) = ''
		internal sealed class IsNullOrWhiteSpaceSqlServer2017Builder : IExtensionCallBuilder
		{
			void IExtensionCallBuilder.Build(ISqExtensionBuilder builder)
			{
				var str = builder.GetExpression("str")!;

				var predicate = new mooSQL.data.model.affirms.ExprExpr(
						new ExpressionWord(typeof(string), "TRIM({1} FROM {0})", str, new ValueWord(new DbDataType(typeof(string), DataFam.NVarChar), WHITESPACES)),
                        AffirmWord.Operator.Equal,
						new ValueWord(typeof(string), string.Empty), false);

				var nullability = new NullabilityContext(builder.Query);
				if (str.CanBeNullable(nullability))
					builder.ResultExpression = new SearchConditionWord(true,
						new IsNull(str, false),
						predicate);
				else
					builder.ResultExpression = new SearchConditionWord(false, predicate);
			}
		}

		// str IS NULL OR LTRIM(str, WHITESPACES) IS NULL
		internal sealed class IsNullOrWhiteSpaceOracleBuilder : IExtensionCallBuilder
		{
			void IExtensionCallBuilder.Build(ISqExtensionBuilder builder)
			{
				var str = builder.GetExpression("str")!;

				var predicate = new IsNull(new FunctionWord(typeof(string), "LTRIM", str, new ValueWord(typeof(string), WHITESPACES)), false);

				var nullability = new NullabilityContext(builder.Query);
				if (str.CanBeNullable(nullability))
					builder.ResultExpression = new SearchConditionWord(true,
						new IsNull(str, false),
						predicate);
				else
					builder.ResultExpression = new SearchConditionWord(false, predicate);
			}
		}

		// str IS NULL OR LTRIM(str, ASCII_WHITESPACES) = ''
		internal sealed class IsNullOrWhiteSpaceInformixBuilder : IExtensionCallBuilder
		{
			void IExtensionCallBuilder.Build(ISqExtensionBuilder builder)
			{
				var str = builder.GetExpression("str")!;

				var predicate = new mooSQL.data.model.affirms.ExprExpr(
						new FunctionWord(typeof(string), "LTRIM", str, new ValueWord(typeof(string), ASCII_WHITESPACES)),
                        AffirmWord.Operator.Equal,
						new ValueWord(typeof(string), string.Empty), false);

				var nullability = new NullabilityContext(builder.Query);
				if (str.CanBeNullable(nullability))
					builder.ResultExpression = new SearchConditionWord(true,
						new IsNull(str, false),
						predicate);
				else
					builder.ResultExpression = new SearchConditionWord(false, predicate);
			}
		}

		// str IS NULL OR LTRIM(str, WHITESPACES) = ''
		internal sealed class IsNullOrWhiteSpaceDefaultBuilder : IExtensionCallBuilder
		{
			void IExtensionCallBuilder.Build(ISqExtensionBuilder builder)
			{
				var str = builder.GetExpression("str")!;

				var predicate = new mooSQL.data.model.affirms.ExprExpr(
						new FunctionWord(typeof(string), "LTRIM", str, new ValueWord(typeof(string), WHITESPACES)),
                        AffirmWord.Operator.Equal,
						new ValueWord(typeof(string), string.Empty), false);

				var nullability = new NullabilityContext(builder.Query);
				if (str.CanBeNullable(nullability))
					builder.ResultExpression = new SearchConditionWord(true,
						new IsNull(str, false),
						predicate);
				else
					builder.ResultExpression = new SearchConditionWord(false, predicate);
			}
		}
		#endregion

		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static string? Trim(string? str)
		{
			return str?.Trim();
		}

		[Expression(ProviderName.Firebird, "TRIM(LEADING FROM {0})", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function("LTrim"                                , IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.ClickHouse, "trimLeft"              , IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static string? TrimLeft(string? str)
		{
			return str?.TrimStart();
		}

		[Expression(ProviderName.Firebird, "TRIM(TRAILING FROM {0})", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function("RTrim"                                 , IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.ClickHouse, "trimRight"              , IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static string? TrimRight(string? str)
		{
			return str?.TrimEnd();
		}

		[Function(                                            IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.DB2,        "Strip({0}, B, {1})",      IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.ClickHouse, "trim(BOTH {1} FROM {0})", IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static string? Trim(string? str, char? ch)
		{
			return str == null || ch == null ? null : str.Trim(ch.Value);
		}

		[Expression(ProviderName.ClickHouse, "trim(LEADING {1} FROM {0})", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.Firebird,   "TRIM(LEADING {1} FROM {0})", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.DB2,        "Strip({0}, L, {1})",         IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function  (               "LTrim",                      IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static string? TrimLeft(string? str, char? ch)
		{
			return str == null || ch == null ? null : str.TrimStart(ch.Value);
		}

		[Expression(ProviderName.ClickHouse, "trim(TRAILING {1} FROM {0})", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.Firebird,   "TRIM(TRAILING {1} FROM {0})", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.DB2,        "Strip({0}, T, {1})",          IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function  (               "RTrim",                       IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static string? TrimRight(string? str, char? ch)
		{
			return str == null || ch == null ? null : str.TrimEnd(ch.Value);
		}

		[Function(PseudoFunctions.TO_LOWER, ServerSideOnly = true, IsPure = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static string? Lower(string? str)
		{
			return str?.ToLower(CultureInfo.CurrentCulture);
		}

		[Function(PseudoFunctions.TO_UPPER, ServerSideOnly = true, IsPure = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static string? Upper(string? str)
		{
			return str?.ToUpper(CultureInfo.CurrentCulture);
		}

		[Expression("Lpad({0},{1},'0')",                                                                            IsNullable = IsNullableType.SameAsFirstParameter)]
		[Expression(ProviderName.Access, "Format({0}, String('0', {1}))",                                                     IsNullable = IsNullableType.SameAsFirstParameter)]
		[Expression(ProviderName.Sybase, "right(replicate('0',{1}) + cast({0} as varchar(255)),{1})",                         IsNullable = IsNullableType.SameAsFirstParameter)]
		[Expression(ProviderName.PostgreSQL, "Lpad({0}::text,{1},'0')",                                                       IsNullable = IsNullableType.SameAsFirstParameter)]
		[Expression(ProviderName.SQLite, "printf('%0{1}d', {0})",                                                             IsNullable = IsNullableType.SameAsFirstParameter)]
		[Expression(ProviderName.ClickHouse, "leftPadUTF8(toString({0}), toUInt32({1}), '0')",                                IsNullable = IsNullableType.SameAsFirstParameter)]
		[Expression(ProviderName.SqlCe, "REPLICATE('0', {1} - LEN(CAST({0} as NVARCHAR({1})))) + CAST({0} as NVARCHAR({1}))", IsNullable = IsNullableType.SameAsFirstParameter)]
		[Expression(ProviderName.SqlServer, "format({0}, 'd{1}')",                                                            IsNullable = IsNullableType.SameAsFirstParameter)]
		[Expression(ProviderName.SqlServer2005, "REPLICATE('0', CASE WHEN LEN(CAST({0} as NVARCHAR)) > {1} THEN 0 ELSE ({1} - LEN(CAST({0} as NVARCHAR))) END) + CAST({0} as NVARCHAR)", IsNullable = IsNullableType.SameAsFirstParameter)]
		[Expression(ProviderName.SqlServer2008, "REPLICATE('0', CASE WHEN LEN(CAST({0} as NVARCHAR)) > {1} THEN 0 ELSE ({1} - LEN(CAST({0} as NVARCHAR))) END) + CAST({0} as NVARCHAR)", IsNullable = IsNullableType.SameAsFirstParameter)]
		public static string? ZeroPad(int? val, int length)
		{
			return val?.ToString($"d{length}", NumberFormatInfo.InvariantInfo);
		}

		sealed class ConcatAttribute : ExpressionAttribute
		{
			public ConcatAttribute() : base("")
			{
			}


		}

		[Concat]
		public static string Concat(params object[] args)
		{
			return string.Concat(args);
		}

		[Concat]
		public static string Concat(params string[] args)
		{
			return string.Concat(args);
		}

		#endregion

		#region Binary Functions

		[Function(                              PreferServerSide = true, IsNullable = IsNullableType.SameAsFirstParameter)]
		[Function(ProviderName.Access,    "Len",          PreferServerSide = true, IsNullable = IsNullableType.SameAsFirstParameter)]
		[Function(ProviderName.Firebird,  "Octet_Length", PreferServerSide = true, IsNullable = IsNullableType.SameAsFirstParameter)]
		[Function(ProviderName.SqlServer, "DataLength",   PreferServerSide = true, IsNullable = IsNullableType.SameAsFirstParameter)]
		[Function(ProviderName.SqlCe,     "DataLength",   PreferServerSide = true, IsNullable = IsNullableType.SameAsFirstParameter)]
		[Function(ProviderName.Sybase,    "DataLength",   PreferServerSide = true, IsNullable = IsNullableType.SameAsFirstParameter)]
		public static int? Length(Binary? value)
		{
			return value == null ? null : value.Length;
		}

		#endregion

		#region Byte[] Functions

		[Function(                              PreferServerSide = true, IsNullable = IsNullableType.SameAsFirstParameter)]
		[Function(ProviderName.Access,    "Len",          PreferServerSide = true, IsNullable = IsNullableType.SameAsFirstParameter)]
		[Function(ProviderName.Firebird,  "Octet_Length", PreferServerSide = true, IsNullable = IsNullableType.SameAsFirstParameter)]
		[Function(ProviderName.SqlServer, "DataLength",   PreferServerSide = true, IsNullable = IsNullableType.SameAsFirstParameter)]
		[Function(ProviderName.SqlCe,     "DataLength",   PreferServerSide = true, IsNullable = IsNullableType.SameAsFirstParameter)]
		[Function(ProviderName.Sybase,    "DataLength",   PreferServerSide = true, IsNullable = IsNullableType.SameAsFirstParameter)]
		public static int? Length(byte[]? value)
		{
			return value == null ? null : value.Length;
		}

		#endregion

		#region DateTime Functions

		[Property(               "CURRENT_TIMESTAMP", CanBeNull = false)]
		[Property(ProviderName.Informix,   "CURRENT",           CanBeNull = false)]
		[Property(ProviderName.Access,     "Now",               CanBeNull = false)]
		[Function(ProviderName.ClickHouse, "now",               CanBeNull = false)]
		public static DateTime GetDate()
		{
			return DateTime.Now;
		}

		[Property(               "CURRENT_TIMESTAMP", ServerSideOnly = true, CanBeNull = false)]
		[Property(ProviderName.Firebird,   "LOCALTIMESTAMP",    ServerSideOnly = true, CanBeNull = false)]
		[Property(ProviderName.Informix,   "CURRENT",           ServerSideOnly = true, CanBeNull = false)]
		[Property(ProviderName.Access,     "Now",               ServerSideOnly = true, CanBeNull = false)]
		[Function(ProviderName.SqlCe,      "GetDate",           ServerSideOnly = true, CanBeNull = false)]
		[Function(ProviderName.Sybase,     "GetDate",           ServerSideOnly = true, CanBeNull = false)]
		[Function(ProviderName.ClickHouse, "now",               ServerSideOnly = true, CanBeNull = false)]
		public static DateTime CurrentTimestamp => throw new LinqException("'CurrentTimestamp' is server side only property.");

		[Function  (ProviderName.SqlServer , "SYSUTCDATETIME"                      , ServerSideOnly = true, CanBeNull = false)]
		[Function  (ProviderName.Sybase    , "GETUTCDATE"                          , ServerSideOnly = true, CanBeNull = false)]
		[Expression(ProviderName.SQLite    , "DATETIME('now')"                     , ServerSideOnly = true, CanBeNull = false)]
		[Function  (ProviderName.MySql     , "UTC_TIMESTAMP"                       , ServerSideOnly = true, CanBeNull = false)]
		[Expression(ProviderName.PostgreSQL, "timezone('UTC', now())"              , ServerSideOnly = true, CanBeNull = false)]
		[Expression(ProviderName.DB2       , "CURRENT TIMESTAMP - CURRENT TIMEZONE", ServerSideOnly = true, CanBeNull = false, Precedence = PrecedenceLv.Subtraction)]
		[Expression(ProviderName.Oracle    , "SYS_EXTRACT_UTC(SYSTIMESTAMP)"       , ServerSideOnly = true, CanBeNull = false, Precedence = PrecedenceLv.Additive)]
		[Property  (ProviderName.SapHana   , "CURRENT_UTCTIMESTAMP"                , ServerSideOnly = true, CanBeNull = false, Precedence = PrecedenceLv.Additive)]
		[Expression(ProviderName.Informix  , "datetime(1970-01-01 00:00:00) year to second + (dbinfo('utc_current')/86400)::int::char(9)::interval day(9) to day + (mod(dbinfo('utc_current'), 86400))::char(5)::interval second(5) to second", ServerSideOnly = true, CanBeNull = false, Precedence = PrecedenceLv.Additive)]
		[Expression(ProviderName.ClickHouse, "now('UTC')"                          , ServerSideOnly = true, CanBeNull = false)]
		public static DateTime CurrentTimestampUtc => DateTime.UtcNow;

		[Property(               "CURRENT_TIMESTAMP", CanBeNull = false)]
		[Property(ProviderName.Informix,   "CURRENT",           CanBeNull = false)]
		[Property(ProviderName.Access,     "Now",               CanBeNull = false)]
		[Function(ProviderName.SqlCe,      "GetDate",           CanBeNull = false)]
		[Function(ProviderName.Sybase,     "GetDate",           CanBeNull = false)]
		[Function(ProviderName.ClickHouse, "now",               CanBeNull = false)]
		public static DateTime CurrentTimestamp2 => DateTime.Now;

		[Function(ProviderName.SqlServer , "SYSDATETIMEOFFSET", ServerSideOnly = true, CanBeNull = false)]
		[Function(ProviderName.PostgreSQL, "now"              , ServerSideOnly = true, CanBeNull = false)]
		[Property(ProviderName.Oracle    , "SYSTIMESTAMP"     , ServerSideOnly = true, CanBeNull = false, Precedence = PrecedenceLv.Additive)]
		[Function(ProviderName.ClickHouse, "now"              , ServerSideOnly = true, CanBeNull = false)]
		public static DateTimeOffset CurrentTzTimestamp => DateTimeOffset.Now;

		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static DateTime? ToDate(int? year, int? month, int? day, int? hour, int? minute, int? second, int? millisecond)
		{
			return year == null || month == null || day == null || hour == null || minute == null || second == null || millisecond == null ?
				null :
				new DateTime(year.Value, month.Value, day.Value, hour.Value, minute.Value, second.Value, millisecond.Value);
		}

		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static DateTime? ToDate(int? year, int? month, int? day, int? hour, int? minute, int? second)
		{
			return year == null || month == null || day == null || hour == null || minute == null || second == null ?
				null :
				new DateTime(year.Value, month.Value, day.Value, hour.Value, minute.Value, second.Value);
		}

		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static DateTime? ToDate(int? year, int? month, int? day)
		{
			return year == null || month == null || day == null ?
				null :
				new DateTime(year.Value, month.Value, day.Value);
		}

		[Property("@@DATEFIRST", CanBeNull = false)]
		[Property(ProviderName.ClickHouse, "1", CanBeNull = false)]
		public static int DateFirst => 7;

#if NET6_0_OR_GREATER
		public static DateOnly? MakeDateOnly(int? year, int? month, int? day)
		{
			return year == null || month == null || day == null ?
				null :
				new DateOnly(year.Value, month.Value, day.Value);
		}
#endif

		public static DateTime? MakeDateTime(int? year, int? month, int? day)
		{
			return year == null || month == null || day == null ?
				null :
				new DateTime(year.Value, month.Value, day.Value);
		}

		public static DateTime? MakeDateTime(int? year, int? month, int? day, int? hour, int? minute, int? second)
		{
			return year == null || month == null || day == null || hour == null || minute == null || second == null ?
				null :
				new DateTime(year.Value, month.Value, day.Value, hour.Value, minute.Value, second.Value);
		}

		#endregion

		#region Math Functions

		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static decimal? Abs    (decimal? value) => value == null ? null : Math.Abs (value.Value);
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static double?  Abs    (double?  value) => value == null ? null : Math.Abs (value.Value);
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static short?   Abs    (short?   value) => value == null ? null : Math.Abs (value.Value);
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static int?     Abs    (int?     value) => value == null ? null : Math.Abs (value.Value);
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static long?    Abs    (long?    value) => value == null ? null : Math.Abs (value.Value);
		[CLSCompliant(false)]
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static sbyte?   Abs    (sbyte?   value) => value == null ? null : Math.Abs (value.Value);
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static float?   Abs    (float?   value) => value == null ? null : Math.Abs (value.Value);

		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static double?  Acos   (double?  value) => value == null ? null : Math.Acos(value.Value);
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static double?  Asin   (double?  value) => value == null ? null : Math.Asin(value.Value);

		[Function(ProviderName.Access, "Atn", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static double?  Atan   (double?  value) => value == null ? null : Math.Atan(value.Value);

		[CLSCompliant(false)]
		[Function(ProviderName.SqlServer, "Atn2",        IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.DB2,       "Atan2", 1, 0, IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.SqlCe,     "Atn2",        IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.Sybase,    "Atn2",        IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(                             IsNullable = IsNullableType.IfAnyParameterNullable)] public static double?  Atan2  (double? x, double? y) { return x == null || y == null? null : Math.Atan2(x.Value, y.Value); }

		[Function(ProviderName.Informix, "Ceil", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.Oracle,   "Ceil", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.SapHana,  "Ceil", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static decimal? Ceiling(decimal? value) => value == null ? null : decimal.Ceiling(value.Value);

		[Function(ProviderName.Informix, "Ceil", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.Oracle,   "Ceil", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.SapHana,  "Ceil", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static double?  Ceiling(double?  value) => value == null ? null : Math.Ceiling(value.Value);

		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static double?  Cos    (double?  value) => value == null ? null : Math.Cos    (value.Value);

		[Function(ProviderName.ClickHouse, "cosh", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static double?  Cosh   (double?  value) => value == null ? null : Math.Cosh   (value.Value);

		[Expression(ProviderName.ClickHouse, "1/tan({0})", IsNullable = IsNullableType.IfAnyParameterNullable, Precedence = PrecedenceLv.Multiplicative)]
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static double?  Cot    (double?  value) { return value == null ? null : (double?)Math.Cos(value.Value) / Math.Sin(value.Value); }

		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static decimal? Degrees(decimal? value) => value == null ? null : (value.Value * 180m / (decimal)Math.PI);
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static double?  Degrees(double?  value) => value == null ? null : (value.Value * 180 / Math.PI);
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static short?   Degrees(short?   value) { return value == null ? null : (short?)  (value.Value * 180 / Math.PI); }
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static int?     Degrees(int?     value) { return value == null ? null : (int?)    (value.Value * 180 / Math.PI); }
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static long?    Degrees(long?    value) { return value == null ? null : (long?)   (value.Value * 180 / Math.PI); }
		[CLSCompliant(false)]
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static sbyte?   Degrees(sbyte?   value) { return value == null ? null : (sbyte?)  (value.Value * 180 / Math.PI); }
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static float?   Degrees(float?   value) { return value == null ? null : (float?)  (value.Value * 180 / Math.PI); }

		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static double?  Exp    (double?  value) => value == null ? null : Math.Exp(value.Value);

		[Function(ProviderName.Access, "Int", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static decimal? Floor  (decimal? value) => value == null ? null : decimal.Floor(value.Value);

		[Function(ProviderName.Access, "Int", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static double?  Floor  (double?  value) => value == null ? null : Math.Floor(value.Value);

		[Function(ProviderName.Informix,   "LogN", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.Oracle,     "Ln",   IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.Firebird,   "Ln",   IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.PostgreSQL, "Ln",   IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.SapHana,    "Ln",   IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(                       IsNullable = IsNullableType.IfAnyParameterNullable)] public static decimal? Log    (decimal? value) { return value == null ? null : (decimal?)Math.Log     ((double)value.Value); }

		[Function(ProviderName.Informix,   "LogN", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.Oracle,     "Ln",   IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.Firebird,   "Ln",   IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.PostgreSQL, "Ln",   IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.SapHana,    "Ln",   IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(                       IsNullable = IsNullableType.IfAnyParameterNullable)] public static double?  Log    (double?  value) => value == null ? null : Math.Log(value.Value);

		[Function(ProviderName.PostgreSQL, "Log", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.SapHana,  "Log(10,{0})", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static double?  Log10  (double?  value) => value == null ? null : Math.Log10(value.Value);

		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.ClickHouse, "Log({1}) / Log({0})", IsNullable = IsNullableType.IfAnyParameterNullable, Precedence = PrecedenceLv.Multiplicative)]
		public static double?  Log(double? newBase, double? value)
		{
			return value == null || newBase == null ? null : Math.Log(value.Value, newBase.Value);
		}

		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.ClickHouse, "Log({1}) / Log({0})", IsNullable = IsNullableType.IfAnyParameterNullable, Precedence = PrecedenceLv.Multiplicative)]
		public static decimal? Log(decimal? newBase, decimal? value)
		{
			return value == null || newBase == null ? null : (decimal?)Math.Log((double)value.Value, (double)newBase.Value);
		}

		[Expression(ProviderName.Access, "{0} ^ {1}", Precedence = PrecedenceLv.Multiplicative, IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static double?  Power(double? x, double? y)
		{
			return x == null || y == null ? null : Math.Pow(x.Value, y.Value);
		}

		[Function(IsNullable = IsNullableType.SameAsFirstParameter)]
		[Function(ProviderName.ClickHouse, "roundBankers", IsNullable = IsNullableType.SameAsFirstParameter)]
		public static decimal? RoundToEven(decimal? value)
		{
			return value == null ? null : Math.Round(value.Value, MidpointRounding.ToEven);
		}

		[Function(IsNullable = IsNullableType.SameAsFirstParameter)]
		[Function(ProviderName.ClickHouse, "roundBankers", IsNullable = IsNullableType.SameAsFirstParameter)]
		public static double? RoundToEven(double? value)
		{
			return value == null ? null : Math.Round(value.Value, MidpointRounding.ToEven);
		}

		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static decimal? Round(decimal? value) { return Round(value, 0); }
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static double?  Round(double?  value) { return Round(value, 0); }

		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static decimal? Round(decimal? value, int? precision)
		{
			return value == null || precision == null ? null : Math.Round(value.Value, precision.Value, MidpointRounding.AwayFromZero);
		}

		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static double? Round(double? value, int? precision)
		{
			return value == null || precision == null ? null : Math.Round(value.Value, precision.Value, MidpointRounding.AwayFromZero);
		}

		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.ClickHouse, "roundBankers", IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static decimal? RoundToEven(decimal? value, int? precision)
		{
			return value == null || precision == null ? null : Math.Round(value.Value, precision.Value, MidpointRounding.ToEven);
		}

		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.ClickHouse, "roundBankers", IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static double? RoundToEven(double?  value, int? precision)
		{
			return value == null || precision == null ? null : Math.Round(value.Value, precision.Value, MidpointRounding.ToEven);
		}

		[Function(ProviderName.Access, "Sgn", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static int? Sign(decimal? value) => value == null ? null : Math.Sign(value.Value);
		[Function(ProviderName.Access, "Sgn", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static int? Sign(double?  value) => value == null ? null : Math.Sign(value.Value);
		[Function(ProviderName.Access, "Sgn", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static int? Sign(short?   value) => value == null ? null : Math.Sign(value.Value);
		[Function(ProviderName.Access, "Sgn", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static int? Sign(int?     value) => value == null ? null : Math.Sign(value.Value);
		[Function(ProviderName.Access, "Sgn", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static int? Sign(long?    value) => value == null ? null : Math.Sign(value.Value);
		[CLSCompliant(false)]
		[Function(ProviderName.Access, "Sgn", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static int? Sign(sbyte?   value) => value == null ? null : Math.Sign(value.Value);
		[Function(ProviderName.Access, "Sgn", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static int? Sign(float?   value) => value == null ? null : Math.Sign(value.Value);

		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static double?  Sin     (double?  value) => value == null ? null : Math.Sin (value.Value);
		[Function(ProviderName.ClickHouse, "sinh", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static double?  Sinh    (double?  value) => value == null ? null : Math.Sinh(value.Value);
		[Function(ProviderName.Access, "Sqr", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static double?  Sqrt    (double?  value) => value == null ? null : Math.Sqrt(value.Value);
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static double?  Tan     (double?  value) => value == null ? null : Math.Tan (value.Value);
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static double?  Tanh    (double?  value) => value == null ? null : Math.Tanh(value.Value);

		[Expression(ProviderName.SqlServer,  "Round({0}, 0, 1)",          IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.DB2,        "Truncate({0}, 0)",          IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.Informix,   "Trunc({0}, 0)",             IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.Oracle,     "Trunc({0}, 0)",             IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.Firebird,   "Trunc({0}, 0)",             IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.PostgreSQL, "Trunc({0}, 0)",             IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.MySql,      "Truncate({0}, 0)",          IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.SqlCe,      "Round({0}, 0, 1)",          IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.SapHana,    "Round({0}, 0, ROUND_DOWN)", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(                                              IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static decimal? Truncate(decimal? value)
		{
			return value == null ? null : decimal.Truncate(value.Value);
		}

		[Expression(ProviderName.SqlServer,  "Round({0}, 0, 1)",          IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.DB2,        "Truncate({0}, 0)",          IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.Informix,   "Trunc({0}, 0)",             IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.Oracle,     "Trunc({0}, 0)",             IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.Firebird,   "Trunc({0}, 0)",             IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.PostgreSQL, "Trunc({0}, 0)",             IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.MySql,      "Truncate({0}, 0)",          IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.SqlCe,      "Round({0}, 0, 1)",          IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.SapHana,    "Round({0}, 0, ROUND_DOWN)", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(                                              IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static double? Truncate(double? value)
		{
			return value == null ? null : Math.Truncate(value.Value);
		}

		#endregion

		#region Identity Functions
		// identity APIs are internal as:
		// - there is no plans to make them public for now
		// - support for more providers required

		/// <summary>
		/// Returns last identity value (current value) for specific table.
		/// </summary>
		[Function  (ProviderName.SqlServer    , "IDENT_CURRENT", ServerSideOnly = true, CanBeNull = true)]
		[Expression(                  "NULL"         , ServerSideOnly = true, CanBeNull = true)]
		internal static object? CurrentIdentity(string tableName) => throw new LinqException($"'{nameof(CurrentIdentity)}' is server side only property.");

		/// <summary>
		/// Returns identity step for specific table.
		/// </summary>
		[Function  (ProviderName.SqlServer    , "IDENT_INCR", ServerSideOnly = true, CanBeNull = true)]
		[Expression(                  "NULL"      , ServerSideOnly = true, CanBeNull = true)]
		internal static object? IdentityStep(string tableName) => throw new LinqException($"'{nameof(IdentityStep)}' is server side only property.");
		#endregion
	}
}
