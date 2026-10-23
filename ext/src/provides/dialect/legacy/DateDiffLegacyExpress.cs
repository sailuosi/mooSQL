using System;
using System.Text;
using mooSQL.data.builder;

namespace mooSQL.data;

/// <summary>仅提供 DateDiff Pure 片段的 Express 基类（无完整方言 DML）。</summary>
public abstract class DateDiffFragmentExpressBase : SQLExpression
{
    /// <summary>
    /// 初始化 <see cref="DateDiffFragmentExpressBase"/> 实例。
    /// </summary>
    protected DateDiffFragmentExpressBase(Dialect dia, string paraPrefix) : base(dia)
    {
        _paraPrefix = paraPrefix;
    }

    /// <summary>为本方言包裹标识符。</summary>
    public override string wrapKeyword(string value) => value;

    /// <summary>构建 SELECT 语句。</summary>
    public override string buildSelect(FragSQL frag)
    {
        var sb = new StringBuilder();
        sb.Append("SELECT ");
        if (frag.distincted)
            sb.Append("DISTINCT ");
        sb.Append(frag.selectInner);
        buildSelectFromToOrderPart(frag, sb);
        return sb.ToString();
    }

    /// <summary>构建 INSERT 语句。</summary>
    public override string buildInsert(FragSQL frag)
        => throw new NotSupportedException($"{GetType().Name} does not support insert SQL.");
}

/// <summary>ClickHouse DateDiff-only legacy（原 DateDiffBuilderClickHouse；完整方言见 ClickHouseExpress）。</summary>
public sealed class ClickHouseDateDiffExpress : DateDiffFragmentExpressBase
{
    /// <summary>
    /// 初始化 <see cref="ClickHouseDateDiffExpress"/> 实例。
    /// </summary>
    public ClickHouseDateDiffExpress(Dialect dia) : base(dia, "@") { }

    static string DateDiffUnit(string unit, string start, string end)
        => $"date_diff('{unit}', {start}, {end})";

    /// <summary>构建按年的日期差表达式。</summary>
    public override string dateDiffYear(string start, string end) => DateDiffUnit("year", start, end);

    /// <summary>构建按季度的日期差表达式。</summary>
    public override string dateDiffQuarter(string start, string end) => DateDiffUnit("quarter", start, end);

    /// <summary>构建按月的日期差表达式。</summary>
    public override string dateDiffMonth(string start, string end) => DateDiffUnit("month", start, end);

    /// <summary>构建按周的日期差表达式。</summary>
    public override string dateDiffWeek(string start, string end) => DateDiffUnit("week", start, end);

    /// <summary>构建按日的日期差表达式。</summary>
    public override string dateDiffDay(string start, string end) => DateDiffUnit("day", start, end);

    /// <summary>构建按小时的日期差表达式。</summary>
    public override string dateDiffHour(string start, string end) => DateDiffUnit("hour", start, end);

    /// <summary>构建按分钟的日期差表达式。</summary>
    public override string dateDiffMinute(string start, string end) => DateDiffUnit("minute", start, end);

    /// <summary>构建按秒的日期差表达式。</summary>
    public override string dateDiffSecond(string start, string end) => DateDiffUnit("second", start, end);

    /// <summary>构建按毫秒的日期差表达式。</summary>
    public override string dateDiffMillisecond(string start, string end)
        => $"toUnixTimestamp64Milli(toDateTime64({end}, 3)) - toUnixTimestamp64Milli(toDateTime64({start}, 3))";
}

/// <summary>SAP HANA DateDiff（原 DateDiffBuilderSapHana）。</summary>
public sealed class SapHanaExpress : DateDiffFragmentExpressBase
{
    /// <summary>
    /// 初始化 <see cref="SapHanaExpress"/> 实例。
    /// </summary>
    public SapHanaExpress(Dialect dia) : base(dia, ":") { }

    /// <summary>构建按日的日期差表达式。</summary>
    public override string dateDiffDay(string start, string end)
        => $"Days_Between({start}, {end})";

    /// <summary>构建按小时的日期差表达式。</summary>
    public override string dateDiffHour(string start, string end)
        => $"Seconds_Between({start}, {end}) / 3600";

    /// <summary>构建按分钟的日期差表达式。</summary>
    public override string dateDiffMinute(string start, string end)
        => $"Seconds_Between({start}, {end}) / 60";

    /// <summary>构建按秒的日期差表达式。</summary>
    public override string dateDiffSecond(string start, string end)
        => $"Seconds_Between({start}, {end})";

    /// <summary>构建按毫秒的日期差表达式。</summary>
    public override string dateDiffMillisecond(string start, string end)
        => $"Nano100_Between({start}, {end}) / 10000";
}

/// <summary>DB2 DateDiff（原 DateDiffBuilderDB2）。</summary>
public sealed class DB2Express : DateDiffFragmentExpressBase
{
    /// <summary>
    /// 初始化 <see cref="DB2Express"/> 实例。
    /// </summary>
    public DB2Express(Dialect dia) : base(dia, "@") { }

    static string SecondsBetween(string start, string end)
        => $"((Days({end}) - Days({start})) * 86400 + (MIDNIGHT_SECONDS({end}) - MIDNIGHT_SECONDS({start})))";

    /// <summary>构建按日的日期差表达式。</summary>
    public override string dateDiffDay(string start, string end)
        => $"({SecondsBetween(start, end)}) / 86400";

    /// <summary>构建按小时的日期差表达式。</summary>
    public override string dateDiffHour(string start, string end)
        => $"({SecondsBetween(start, end)}) / 3600";

    /// <summary>构建按分钟的日期差表达式。</summary>
    public override string dateDiffMinute(string start, string end)
        => $"({SecondsBetween(start, end)}) / 60";

    /// <summary>构建按秒的日期差表达式。</summary>
    public override string dateDiffSecond(string start, string end)
        => SecondsBetween(start, end);

    /// <summary>构建按毫秒的日期差表达式。</summary>
    public override string dateDiffMillisecond(string start, string end)
        => $"(({SecondsBetween(start, end)}) * 1000 + (MICROSECOND({end}) - MICROSECOND({start})) / 1000)";
}

/// <summary>SQL CE NullIf 片段（原 NullIf Expression 回退）。</summary>
public sealed class SqlCeExpress : DateDiffFragmentExpressBase
{
    /// <summary>
    /// 初始化 <see cref="SqlCeExpress"/> 实例。
    /// </summary>
    public SqlCeExpress(Dialect dia) : base(dia, "@") { }

    /// <summary>构建 NULLIF 表达式。</summary>
    public override string nullIf(string left, string right)
        => $"CASE WHEN {left} = {right} THEN NULL ELSE {left} END";
}
