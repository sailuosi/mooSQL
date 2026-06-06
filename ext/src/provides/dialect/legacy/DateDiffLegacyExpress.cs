using System;
using System.Text;
using mooSQL.data.builder;

namespace mooSQL.data;

/// <summary>仅提供 DateDiff Pure 片段的 Express 基类（无完整方言 DML）。</summary>
public abstract class DateDiffFragmentExpressBase : SQLExpression
{
    protected DateDiffFragmentExpressBase(Dialect dia, string paraPrefix) : base(dia)
    {
        _paraPrefix = paraPrefix;
    }

    public override string wrapKeyword(string value) => value;

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

    public override string buildInsert(FragSQL frag)
        => throw new NotSupportedException($"{GetType().Name} does not support insert SQL.");
}

/// <summary>ClickHouse DateDiff（原 DateDiffBuilderClickHouse）。</summary>
public sealed class ClickHouseExpress : DateDiffFragmentExpressBase
{
    public ClickHouseExpress(Dialect dia) : base(dia, "@") { }

    static string DateDiffUnit(string unit, string start, string end)
        => $"date_diff('{unit}', {start}, {end})";

    public override string dateDiffYear(string start, string end) => DateDiffUnit("year", start, end);
    public override string dateDiffQuarter(string start, string end) => DateDiffUnit("quarter", start, end);
    public override string dateDiffMonth(string start, string end) => DateDiffUnit("month", start, end);
    public override string dateDiffWeek(string start, string end) => DateDiffUnit("week", start, end);
    public override string dateDiffDay(string start, string end) => DateDiffUnit("day", start, end);
    public override string dateDiffHour(string start, string end) => DateDiffUnit("hour", start, end);
    public override string dateDiffMinute(string start, string end) => DateDiffUnit("minute", start, end);
    public override string dateDiffSecond(string start, string end) => DateDiffUnit("second", start, end);

    public override string dateDiffMillisecond(string start, string end)
        => $"toUnixTimestamp64Milli(toDateTime64({end}, 3)) - toUnixTimestamp64Milli(toDateTime64({start}, 3))";
}

/// <summary>SAP HANA DateDiff（原 DateDiffBuilderSapHana）。</summary>
public sealed class SapHanaExpress : DateDiffFragmentExpressBase
{
    public SapHanaExpress(Dialect dia) : base(dia, ":") { }

    public override string dateDiffDay(string start, string end)
        => $"Days_Between({start}, {end})";

    public override string dateDiffHour(string start, string end)
        => $"Seconds_Between({start}, {end}) / 3600";

    public override string dateDiffMinute(string start, string end)
        => $"Seconds_Between({start}, {end}) / 60";

    public override string dateDiffSecond(string start, string end)
        => $"Seconds_Between({start}, {end})";

    public override string dateDiffMillisecond(string start, string end)
        => $"Nano100_Between({start}, {end}) / 10000";
}

/// <summary>DB2 DateDiff（原 DateDiffBuilderDB2）。</summary>
public sealed class DB2Express : DateDiffFragmentExpressBase
{
    public DB2Express(Dialect dia) : base(dia, "@") { }

    static string SecondsBetween(string start, string end)
        => $"((Days({end}) - Days({start})) * 86400 + (MIDNIGHT_SECONDS({end}) - MIDNIGHT_SECONDS({start})))";

    public override string dateDiffDay(string start, string end)
        => $"({SecondsBetween(start, end)}) / 86400";

    public override string dateDiffHour(string start, string end)
        => $"({SecondsBetween(start, end)}) / 3600";

    public override string dateDiffMinute(string start, string end)
        => $"({SecondsBetween(start, end)}) / 60";

    public override string dateDiffSecond(string start, string end)
        => SecondsBetween(start, end);

    public override string dateDiffMillisecond(string start, string end)
        => $"(({SecondsBetween(start, end)}) * 1000 + (MICROSECOND({end}) - MICROSECOND({start})) / 1000)";
}

/// <summary>SQL CE NullIf 片段（原 NullIf Expression 回退）。</summary>
public sealed class SqlCeExpress : DateDiffFragmentExpressBase
{
    public SqlCeExpress(Dialect dia) : base(dia, "@") { }

    public override string nullIf(string left, string right)
        => $"CASE WHEN {left} = {right} THEN NULL ELSE {left} END";
}
