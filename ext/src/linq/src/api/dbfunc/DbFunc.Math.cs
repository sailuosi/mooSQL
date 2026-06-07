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
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static decimal? Ceiling(decimal? value) => value == null ? null : decimal.Ceiling(value.Value);

		[Function(ProviderName.Informix, "Ceil", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.Oracle,   "Ceil", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static double?  Ceiling(double?  value) => value == null ? null : Math.Ceiling(value.Value);

		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static double?  Cos    (double?  value) => value == null ? null : Math.Cos    (value.Value);

		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static double?  Cosh   (double?  value) => value == null ? null : Math.Cosh   (value.Value);

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
		[Function(                       IsNullable = IsNullableType.IfAnyParameterNullable)] public static decimal? Log    (decimal? value) { return value == null ? null : (decimal?)Math.Log     ((double)value.Value); }

		[Function(ProviderName.Informix,   "LogN", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.Oracle,     "Ln",   IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.Firebird,   "Ln",   IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(ProviderName.PostgreSQL, "Ln",   IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(                       IsNullable = IsNullableType.IfAnyParameterNullable)] public static double?  Log    (double?  value) => value == null ? null : Math.Log(value.Value);

		[Function(ProviderName.PostgreSQL, "Log", IsNullable = IsNullableType.IfAnyParameterNullable)]
		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)] public static double?  Log10  (double?  value) => value == null ? null : Math.Log10(value.Value);

		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static double?  Log(double? newBase, double? value)
		{
			return value == null || newBase == null ? null : Math.Log(value.Value, newBase.Value);
		}

		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)]
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
		public static decimal? RoundToEven(decimal? value)
		{
			return value == null ? null : Math.Round(value.Value, MidpointRounding.ToEven);
		}

		[Function(IsNullable = IsNullableType.SameAsFirstParameter)]
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
		public static decimal? RoundToEven(decimal? value, int? precision)
		{
			return value == null || precision == null ? null : Math.Round(value.Value, precision.Value, MidpointRounding.ToEven);
		}

		[Function(IsNullable = IsNullableType.IfAnyParameterNullable)]
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
		[Function(                                              IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static double? Truncate(double? value)
		{
			return value == null ? null : Math.Truncate(value.Value);
		}

		#endregion
	}
}
