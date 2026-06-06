#if NET6_0_OR_GREATER
using System;
using System.Globalization;

namespace mooSQL.linq
{
	using SqlQuery;
	using Expressions;

	using PN = ProviderName;

	public partial class DbFunc
	{
		#region DatePart

		public static int? DatePart([SqlQueryDependent] DateParts part, [ExprParameter] DateOnly? date)
		{
			if (date == null)
				return null;

			return part switch
			{
				DateParts.Year      => date.Value.Year,
				DateParts.Quarter   => (date.Value.Month - 1) / 3 + 1,
				DateParts.Month     => date.Value.Month,
				DateParts.DayOfYear => date.Value.DayOfYear,
				DateParts.Day       => date.Value.Day,
				DateParts.Week      => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(date.Value.ToDateTime(TimeOnly.MinValue), CalendarWeekRule.FirstDay, DayOfWeek.Sunday),
				DateParts.WeekDay   => ((int)date.Value.DayOfWeek + 1 + DateFirst + 6) % 7 + 1,
				_                   => throw new InvalidOperationException(),
			};
		}

		#endregion

		#region DateAdd

		public static DateOnly? DateAdd([SqlQueryDependent] DateParts part, double? number, DateOnly? date)
		{
			if (number == null || date == null)
				return null;

			return part switch
			{
				DateParts.Year      => date.Value.AddYears((int)number),
				DateParts.Quarter   => date.Value.AddMonths((int)number * 3),
				DateParts.Month     => date.Value.AddMonths((int)number),
				DateParts.DayOfYear => date.Value.AddDays((int)number.Value),
				DateParts.Day       => date.Value.AddDays((int)number.Value),
				DateParts.Week      => date.Value.AddDays((int)number.Value * 7),
				DateParts.WeekDay   => date.Value.AddDays((int)number.Value),
				_                   => throw new InvalidOperationException(),
			};
		}

		#endregion

		#region DateDiff
		[CLSCompliant(false)]
		[Extension(               "",              PreferServerSide = true)]
		[Extension(PN.MySql,      "",              PreferServerSide = true)]
		[Extension(PN.DB2,        "",              PreferServerSide = true)]
		[Extension(PN.SapHana,    "",              PreferServerSide = true)]
		[Extension(PN.SQLite,     "",              PreferServerSide = true)]
		[Extension(PN.PostgreSQL, "",              PreferServerSide = true)]
		[Extension(PN.Oracle,     "",              PreferServerSide = true)]
		[Extension(PN.Access,     "",              PreferServerSide = true)]
		[Extension(PN.ClickHouse, "",              PreferServerSide = true)]
		public static int? DateDiff(DateParts part, DateOnly? startDate, DateOnly? endDate)
		{
			if (startDate == null || endDate == null)
				return null;

			return part switch
			{
				DateParts.Day => endDate.Value.DayNumber - startDate.Value.DayNumber,
				_             => throw new InvalidOperationException(),
			};
		}
		#endregion
	}
}
#endif
