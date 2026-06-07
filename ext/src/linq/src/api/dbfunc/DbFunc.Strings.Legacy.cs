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
		/// <summary>registry-first（Bootstrap）；方言 <see cref="mooSQL.data.dialect.SQLExpression.charIndex"/> override。</summary>
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

		public static int? CharIndex(string? substring, string? str, int? start)
		{
			if (str == null || substring == null || start == null) return null;

			var index = start.Value < 1 ? 0 : start.Value > str.Length ? str.Length - 1 : start.Value - 1;
			return substring.Length == 0 ? 0 : str.IndexOf(substring, index) + 1;
		}

		public static int? CharIndex(char? value, string? str)
		{
			if (value == null || str == null) return null;

			return str.IndexOf(value.Value) + 1;
		}

		public static int? CharIndex(char? value, string? str, int? start)
		{
			if (str == null || value == null || start == null) return null;

			var index = start.Value < 1 ? 0 : start.Value > str.Length ? str.Length - 1 : start.Value - 1;
			return str.IndexOf(value.Value, index) + 1;
		}

		[Function(                              IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static string? Reverse(string? str)
		{
			if (string.IsNullOrEmpty(str)) return str;

			var chars = str!.ToCharArray();
			Array.Reverse(chars);
			return new string(chars);
		}

		[Function(                           PreferServerSide = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.SQLite,     "LeftStr",  PreferServerSide = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
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
		public static string Stuff(IEnumerable<string> characterExpression, int? start, int? length, string replaceWithExpression)
		{
			throw new NotImplementedException();
		}

		[Function(                                                        IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static string? Space(int? length)
		{
			return length == null || length.Value < 0 ? null : "".PadRight(length.Value);
		}

		[Function(               Name = "LPad",                            IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static string? PadLeft(string? str, int? length, char? paddingChar)
		{
			if (str == null || length == null || paddingChar == null) return null;
			if (length.Value < 0)                                     return null;
			if (length.Value <= str.Length)                           return str.Substring(0, length.Value);

			return str.PadLeft(length.Value, paddingChar.Value);
		}

		[Function(               Name = "RPad",         IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static string? PadRight(string? str, int? length, char? paddingChar)
		{
			if (str == null || length == null || paddingChar == null) return null;
			if (length.Value < 0) return null;
			if (length.Value <= str.Length) return str.Substring(0, length.Value);

			return str.PadRight(length.Value, paddingChar.Value);
		}

		public static string? Replace(string? str, string? oldValue, string? newValue)
		{
			if (str == null || oldValue == null || newValue == null) return null;
			if (str.Length == 0)                                     return str;
			if (oldValue.Length == 0)                                return str; // Replace raises exception here.

			return str.Replace(oldValue, newValue);
		}

		public static string? Replace(string? str, char? oldValue, char? newValue)
		{
			if (str == null || oldValue == null || newValue == null) return null;
			if (str.Length == 0)                                     return str;

			return str.Replace(oldValue.Value, newValue.Value);
		}

		#region IsNullOrWhiteSpace

		/// <summary>registry-first（Bootstrap <c>IsNullOrWhiteSpacePredicate</c>）；<c>string.IsNullOrWhiteSpace</c> 映射目标。</summary>
		public static bool IsNullOrWhiteSpace(string? str)
			=> throw new LinqException($"'{nameof(IsNullOrWhiteSpace)}' is only server-side method.");

		#endregion

		#endregion
	}
}
