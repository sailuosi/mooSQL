using System;
using System.Globalization;

namespace mooSQL.linq
{
	using mooSQL.linq.clause;
	using mooSQL.linq.expressions;
	using PN = ProviderName;
    using mooSQL.data.model;

    public partial class DbFunc
	{
		/// <summary>
		/// Builds a date-part expression for s.
		/// </summary>
		[Enum]
		public enum DateParts
		{
			/// <summary>
			/// Builds a year-part expression.
			/// </summary>
			/// <summary>
			/// Builds a year-part expression.
			/// </summary>
			Year        =  0,
			/// <summary>
			/// Quarter
			/// </summary>
			/// <summary>
			/// Quarter
			/// </summary>
			Quarter     =  1,
			/// <summary>
			/// Builds a month-part expression.
			/// </summary>
			/// <summary>
			/// Builds a month-part expression.
			/// </summary>
			Month       =  2,
			/// <summary>
			/// DayOfYear
			/// </summary>
			/// <summary>
			/// DayOfYear
			/// </summary>
			DayOfYear   =  3,
			/// <summary>
			/// Builds a day-part expression.
			/// </summary>
			/// <summary>
			/// Builds a day-part expression.
			/// </summary>
			Day         =  4,
			/// <summary>
			/// This date part behavior depends on used database and also depends on where if calculated - in C# code or in database.
			/// Eeach database could have own week numbering logic, see notes below.
			///
			/// Current implementation uses following schemas per-provider:
			/// C# evaluation:
			/// <para>
			/// <c>CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(date.Value, CalendarWeekRule.FirstDay, DayOfWeek.Sunday)</c>
			/// </para>
			/// Databases:
			/// <list type="bullet">
			/// <item>US numbering schema used by:
			/// <list type="bullet">
			/// <item>MS Access</item>
			/// <item>SQL CE</item>
			/// <item>SQL Server</item>
			/// <item>SAP/Sybase ASE</item>
			/// <item>Informix</item>
			/// </list>
			/// </item>
			/// <item>US 0-based numbering schema used by MySQL database</item>
			/// <item>ISO numbering schema with incorrect numbering of first week used by SAP HANA database</item>
			/// <item>ISO numbering schema with proper numbering of first week used by:
			/// <list type="bullet">
			/// <item>Firebird</item>
			/// <item>PostgreSQL</item>
			/// <item>ClickHouse</item>
			/// </list>
			/// </item>
			/// <item>Primitive (each 7 days counted as week) numbering schema:
			/// <list type="bullet">
			/// <item>DB2</item>
			/// <item>Oracle</item>
			/// </list>
			/// </item>
			/// <item>SQLite numbering logic cannot be classified by human being</item>
			/// </list>
			/// </summary>
			Week        =  5,
			/// <summary>
			/// WeekDay
			/// </summary>
			/// <summary>
			/// WeekDay
			/// </summary>
			WeekDay     =  6,
			/// <summary>
			/// Hour
			/// </summary>
			/// <summary>
			/// Hour
			/// </summary>
			Hour        =  7,
			/// <summary>
			/// Minute
			/// </summary>
			/// <summary>
			/// Minute
			/// </summary>
			Minute      =  8,
			/// <summary>
			/// Second
			/// </summary>
			/// <summary>
			/// Second
			/// </summary>
			Second      =  9,
			/// <summary>
			/// Millisecond
			/// </summary>
			/// <summary>
			/// Millisecond
			/// </summary>
			Millisecond = 10,
		}

		#region DatePart
		/// <summary>
		/// Builds a date-part expression for .
		/// </summary>

		public static int? DatePart([SqlQueryDependent] DateParts part, [ExprParameter] DateTime? date)
		{
			if (date == null)
				return null;

			return part switch
			{
				DateParts.Year          => date.Value.Year,
				DateParts.Quarter       => (date.Value.Month - 1) / 3 + 1,
				DateParts.Month         => date.Value.Month,
				DateParts.DayOfYear     => date.Value.DayOfYear,
				DateParts.Day           => date.Value.Day,
				DateParts.Week          => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(date.Value, CalendarWeekRule.FirstDay, DayOfWeek.Sunday),
				DateParts.WeekDay       => ((int)date.Value.DayOfWeek + 1 + DateFirst + 6) % 7 + 1,
				DateParts.Hour          => date.Value.Hour,
				DateParts.Minute        => date.Value.Minute,
				DateParts.Second        => date.Value.Second,
				DateParts.Millisecond   => date.Value.Millisecond,
				_                           => throw new InvalidOperationException(),
			};
		}

		#endregion DatePart

		#region DateAdd
		/// <summary>
		/// Builds a date-add expression for .
		/// </summary>

		public static DateTime? DateAdd([SqlQueryDependent] DateParts part, double? number, DateTime? date)
		{
			if (number == null || date == null)
				return null;

			return part switch
			{
				DateParts.Year          => date.Value.AddYears((int)number),
				DateParts.Quarter       => date.Value.AddMonths((int)number * 3),
				DateParts.Month         => date.Value.AddMonths((int)number),
				DateParts.DayOfYear     => date.Value.AddDays(number.Value),
				DateParts.Day           => date.Value.AddDays(number.Value),
				DateParts.Week          => date.Value.AddDays(number.Value * 7),
				DateParts.WeekDay       => date.Value.AddDays(number.Value),
				DateParts.Hour          => date.Value.AddHours(number.Value),
				DateParts.Minute        => date.Value.AddMinutes(number.Value),
				DateParts.Second        => date.Value.AddSeconds(number.Value),
				DateParts.Millisecond   => date.Value.AddMilliseconds(number.Value),
				_                       => throw new InvalidOperationException(),
			};
		}

		#endregion

		#region DateDiff

		/// <summary>registry-first（Bootstrap <see cref="mooSQL.data.translation.DbFuncExpressionEntry.IsDateDiffPredicate"/>）；无 <see cref="ExtensionAttribute"/>。</summary>
		[CLSCompliant(false)]
		public static int? DateDiff(DateParts part, DateTime? startDate, DateTime? endDate)
		{
			if (startDate == null || endDate == null)
				return null;

			return part switch
			{
				DateParts.Day         => (int)(endDate - startDate).Value.TotalDays,
				DateParts.Hour        => (int)(endDate - startDate).Value.TotalHours,
				DateParts.Minute      => (int)(endDate - startDate).Value.TotalMinutes,
				DateParts.Second      => (int)(endDate - startDate).Value.TotalSeconds,
				DateParts.Millisecond => (int)(endDate - startDate).Value.TotalMilliseconds,
				_                     => throw new InvalidOperationException(),
			};
		}

		#endregion
	}
}
