using mooSQL.data;

namespace mooSQL.linq.translator;

/// <summary>DatePart / DateAdd 方言 SQL 片段解析（Pure <see cref="SQLExpression"/>）。</summary>
internal static class DateSqlTemplateResolver
{
    public static string? ResolveDatePartFormat(SQLExpression expression, DbFunc.DateParts part)
    {
        const string date = "{0}";
        return part switch
        {
            DbFunc.DateParts.Year         => expression.datePartYear(date),
            DbFunc.DateParts.Quarter      => expression.datePartQuarter(date),
            DbFunc.DateParts.Month        => expression.datePartMonth(date),
            DbFunc.DateParts.DayOfYear    => expression.datePartDayOfYear(date),
            DbFunc.DateParts.Day          => expression.datePartDay(date),
            DbFunc.DateParts.Week         => expression.datePartWeek(date),
            DbFunc.DateParts.WeekDay      => expression.datePartWeekDay(date),
            DbFunc.DateParts.Hour         => expression.datePartHour(date),
            DbFunc.DateParts.Minute       => expression.datePartMinute(date),
            DbFunc.DateParts.Second       => expression.datePartSecond(date),
            DbFunc.DateParts.Millisecond  => expression.datePartMillisecond(date),
            _                             => null
        };
    }

    public static string? ResolveDateAddFormat(SQLExpression expression, DbFunc.DateParts part)
    {
        const string amount = "{0}";
        const string date = "{1}";
        return part switch
        {
            DbFunc.DateParts.Year         => expression.dateAddYear(amount, date),
            DbFunc.DateParts.Quarter      => expression.dateAddQuarter(amount, date),
            DbFunc.DateParts.Month        => expression.dateAddMonth(amount, date),
            DbFunc.DateParts.Week         => expression.dateAddWeek(amount, date),
            DbFunc.DateParts.Day          => expression.dateAddDay(amount, date),
            DbFunc.DateParts.DayOfYear    => expression.dateAddDayOfYear(amount, date),
            DbFunc.DateParts.WeekDay      => expression.dateAddWeekDay(amount, date),
            DbFunc.DateParts.Hour         => expression.dateAddHour(amount, date),
            DbFunc.DateParts.Minute       => expression.dateAddMinute(amount, date),
            DbFunc.DateParts.Second       => expression.dateAddSecond(amount, date),
            DbFunc.DateParts.Millisecond  => expression.dateAddMillisecond(amount, date),
            _                             => null
        };
    }
}
