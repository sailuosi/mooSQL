using System;

namespace mooSQL.linq
{
	partial class DbFunc
	{
		[Extension("", "", PreferServerSide = true, IsPredicate = true)]
		[Obsolete("Prefer DbFuncRegistry / dialect.expression.between; Extension Builder removed in R12.")]
		public static bool Between<T>(this T value, T low, T high)
			where T : IComparable
		{
			return value != null && value.CompareTo(low) >= 0 && value.CompareTo(high) <= 0;
		}

		[Extension("", "", PreferServerSide = true, IsPredicate = true)]
		public static bool Between<T>(this T? value, T? low, T? high)
			where T : struct, IComparable
		{
			return value != null && value.Value.CompareTo(low) >= 0 && value.Value.CompareTo(high) <= 0;
		}

		[Extension("", "", PreferServerSide = true, IsPredicate = true)]
		public static bool NotBetween<T>(this T value, T low, T high)
			where T : IComparable
		{
			return value != null && (value.CompareTo(low) < 0 || value.CompareTo(high) > 0);
		}

		[Extension("", "", PreferServerSide = true, IsPredicate = true)]
		public static bool NotBetween<T>(this T? value, T? low, T? high)
			where T : struct, IComparable
		{
			return value != null && (value.Value.CompareTo(low) < 0 || value.Value.CompareTo(high) > 0);
		}
	}
}
