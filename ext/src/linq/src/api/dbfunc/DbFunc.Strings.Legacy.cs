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
		#region String Functions (Legacy)

		[CLSCompliant(false)]
		[Function(                                     IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.DB2,        "Locate",             IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.MySql,      "Locate",             IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.Firebird,   "Position",           IsNullable = IsNullableType.IfAnyParameterNullable)]
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
		public static int? CharIndex(string? substring, string? str, int? start)
		{
			if (str == null || substring == null || start == null) return null;

			var index = start.Value < 1 ? 0 : start.Value > str.Length ? str.Length - 1 : start.Value - 1;
			return substring.Length == 0 ? 0 : str.IndexOf(substring, index) + 1;
		}

		[Function(                                     IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.DB2,        "Locate",             IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.MySql,      "Locate",             IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.Firebird,   "Position",           IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static int? CharIndex(char? value, string? str)
		{
			if (value == null || str == null) return null;

			return str.IndexOf(value.Value) + 1;
		}

		[Function(                                                          IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.DB2,        "Locate",                                  IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.MySql,      "Locate",                                  IsNullable = IsNullableType.IfAnyParameterNullable)]
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

		#endregion
	}
}
