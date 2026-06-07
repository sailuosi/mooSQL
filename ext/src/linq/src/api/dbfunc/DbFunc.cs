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

		/// <summary>客户端执行（<see cref="ProviderMemberTranslatorDefault"/> 翻译为常量）；非 server-side SQL 函数。</summary>
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

		/// <summary>registry-first（Bootstrap）；无 <see cref="FunctionAttribute"/>。</summary>
		public static int? Length(string? str)
		{
			return str?.Length;
		}

		/// <summary>registry-first（Bootstrap）；无 <see cref="FunctionAttribute"/>。</summary>
		public static string? Substring(string? str, int? start, int? length)
		{
			if (str == null || start == null || length == null) return null;
			if (start.Value < 1 || start.Value > str.Length) return null;
			if (length.Value < 0) return null;

			var index = start.Value - 1;
			var maxAllowedLength = Math.Min(str.Length - index, length.Value);

			return str.Substring(index, maxAllowedLength);
		}

		/// <summary>registry-first（Bootstrap）；无 <see cref="FunctionAttribute"/>。</summary>
		public static bool Like(string? matchExpression, string? pattern)
			=> throw new LinqException($"'{nameof(Like)}' is only server-side method.");

		/// <summary>registry-first（Bootstrap）；无 <see cref="FunctionAttribute"/>。</summary>
		public static bool Like(string? matchExpression, string? pattern, char? escapeCharacter)
			=> throw new LinqException($"'{nameof(Like)}' is only server-side method.");


		/// <summary>registry-first（Bootstrap）；无 <see cref="FunctionAttribute"/>。</summary>
		public static string? Trim(string? str)
		{
			return str?.Trim();
		}

		[Expression(ProviderName.Firebird, "TRIM(LEADING FROM {0})", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function("LTrim"                                , IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static string? TrimLeft(string? str)
		{
			return str?.TrimStart();
		}

		[Expression(ProviderName.Firebird, "TRIM(TRAILING FROM {0})", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function("RTrim"                                 , IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static string? TrimRight(string? str)
		{
			return str?.TrimEnd();
		}

		[Function(                                            IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.DB2,        "Strip({0}, B, {1})",      IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static string? Trim(string? str, char? ch)
		{
			return str == null || ch == null ? null : str.Trim(ch.Value);
		}

		[Expression(ProviderName.Firebird,   "TRIM(LEADING {1} FROM {0})", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.DB2,        "Strip({0}, L, {1})",         IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function  (               "LTrim",                      IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static string? TrimLeft(string? str, char? ch)
		{
			return str == null || ch == null ? null : str.TrimStart(ch.Value);
		}

		[Expression(ProviderName.Firebird,   "TRIM(TRAILING {1} FROM {0})", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Expression(ProviderName.DB2,        "Strip({0}, T, {1})",          IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function  (               "RTrim",                       IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static string? TrimRight(string? str, char? ch)
		{
			return str == null || ch == null ? null : str.TrimEnd(ch.Value);
		}

		/// <summary>registry-first（Bootstrap）；无 <see cref="FunctionAttribute"/>。</summary>
		public static string? Lower(string? str)
		{
			return str?.ToLower(CultureInfo.CurrentCulture);
		}

		/// <summary>registry-first（Bootstrap）；无 <see cref="FunctionAttribute"/>。</summary>
		public static string? Upper(string? str)
		{
			return str?.ToUpper(CultureInfo.CurrentCulture);
		}

		[Expression("Lpad({0},{1},'0')",                                                                            IsNullable = IsNullableType.SameAsFirstParameter)]
		[Expression(ProviderName.Access, "Format({0}, String('0', {1}))",                                                     IsNullable = IsNullableType.SameAsFirstParameter)]
		[Expression(ProviderName.Sybase, "right(replicate('0',{1}) + cast({0} as varchar(255)),{1})",                         IsNullable = IsNullableType.SameAsFirstParameter)]
		[Expression(ProviderName.PostgreSQL, "Lpad({0}::text,{1},'0')",                                                       IsNullable = IsNullableType.SameAsFirstParameter)]
		[Expression(ProviderName.SQLite, "printf('%0{1}d', {0})",                                                             IsNullable = IsNullableType.SameAsFirstParameter)]
		[Expression(ProviderName.SqlCe, "REPLICATE('0', {1} - LEN(CAST({0} as NVARCHAR({1})))) + CAST({0} as NVARCHAR({1}))", IsNullable = IsNullableType.SameAsFirstParameter)]
		[Expression(ProviderName.SqlServer, "format({0}, 'd{1}')",                                                            IsNullable = IsNullableType.SameAsFirstParameter)]
		[Expression(ProviderName.SqlServer2005, "REPLICATE('0', CASE WHEN LEN(CAST({0} as NVARCHAR)) > {1} THEN 0 ELSE ({1} - LEN(CAST({0} as NVARCHAR))) END) + CAST({0} as NVARCHAR)", IsNullable = IsNullableType.SameAsFirstParameter)]
		[Expression(ProviderName.SqlServer2008, "REPLICATE('0', CASE WHEN LEN(CAST({0} as NVARCHAR)) > {1} THEN 0 ELSE ({1} - LEN(CAST({0} as NVARCHAR))) END) + CAST({0} as NVARCHAR)", IsNullable = IsNullableType.SameAsFirstParameter)]
		public static string? ZeroPad(int? val, int length)
		{
			return val?.ToString($"d{length}", NumberFormatInfo.InvariantInfo);
		}

		/// <summary>registry-first（Bootstrap <see cref="mooSQL.data.translation.DbFuncExpressionEntry.IsConcatPredicate"/>）；无 <see cref="ExpressionAttribute"/>。</summary>
		public static string Concat(params object[] args)
		{
			return string.Concat(args);
		}

		/// <summary>registry-first（Bootstrap）；无 <see cref="ExpressionAttribute"/>。</summary>
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
		public static DateTime CurrentTimestamp => throw new LinqException("'CurrentTimestamp' is server side only property.");

		[Function  (ProviderName.SqlServer , "SYSUTCDATETIME"                      , ServerSideOnly = true, CanBeNull = false)]
		[Function  (ProviderName.Sybase    , "GETUTCDATE"                          , ServerSideOnly = true, CanBeNull = false)]
		[Expression(ProviderName.SQLite    , "DATETIME('now')"                     , ServerSideOnly = true, CanBeNull = false)]
		[Function  (ProviderName.MySql     , "UTC_TIMESTAMP"                       , ServerSideOnly = true, CanBeNull = false)]
		[Expression(ProviderName.PostgreSQL, "timezone('UTC', now())"              , ServerSideOnly = true, CanBeNull = false)]
		[Expression(ProviderName.DB2       , "CURRENT TIMESTAMP - CURRENT TIMEZONE", ServerSideOnly = true, CanBeNull = false, Precedence = PrecedenceLv.Subtraction)]
		[Expression(ProviderName.Oracle    , "SYS_EXTRACT_UTC(SYSTIMESTAMP)"       , ServerSideOnly = true, CanBeNull = false, Precedence = PrecedenceLv.Additive)]
		[Expression(ProviderName.Informix  , "datetime(1970-01-01 00:00:00) year to second + (dbinfo('utc_current')/86400)::int::char(9)::interval day(9) to day + (mod(dbinfo('utc_current'), 86400))::char(5)::interval second(5) to second", ServerSideOnly = true, CanBeNull = false, Precedence = PrecedenceLv.Additive)]
		public static DateTime CurrentTimestampUtc => DateTime.UtcNow;

		[Property(               "CURRENT_TIMESTAMP", CanBeNull = false)]
		[Property(ProviderName.Informix,   "CURRENT",           CanBeNull = false)]
		[Property(ProviderName.Access,     "Now",               CanBeNull = false)]
		[Function(ProviderName.SqlCe,      "GetDate",           CanBeNull = false)]
		[Function(ProviderName.Sybase,     "GetDate",           CanBeNull = false)]
		public static DateTime CurrentTimestamp2 => DateTime.Now;

		[Function(ProviderName.SqlServer , "SYSDATETIMEOFFSET", ServerSideOnly = true, CanBeNull = false)]
		[Function(ProviderName.PostgreSQL, "now"              , ServerSideOnly = true, CanBeNull = false)]
		[Property(ProviderName.Oracle    , "SYSTIMESTAMP"     , ServerSideOnly = true, CanBeNull = false, Precedence = PrecedenceLv.Additive)]
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

		#region Collate

		/// <summary>Apply collation — registry + Pure <see cref="SQLExpression.collate"/> 方言片段。</summary>
		[return: NotNullIfNotNull(nameof(expr))]
		public static string? Collate(this string? expr, [SqlQueryDependent] string collation)
			=> throw new InvalidOperationException($"{nameof(DbFunc)}.{nameof(Collate)} is server-side only API.");

		#endregion

	}
}
