// LINQ 可翻译函数 — 逐步从 Ext DbFunc 迁入 Pure 方言

using System;

namespace mooSQL.data
{
    public abstract partial class SQLExpression
    {
        /// <summary>SQL BETWEEN 谓词片段（方言可 override）。</summary>
        public virtual string between(string column, string low, string high)
            => $"{column} BETWEEN {low} AND {high}";

        /// <summary>SQL NOT BETWEEN 谓词片段。</summary>
        public virtual string notBetween(string column, string low, string high)
            => $"{column} NOT BETWEEN {low} AND {high}";

        /// <summary>IS NULL 谓词。</summary>
        public virtual string isNull(string expr) => $"{expr} IS NULL";

        /// <summary>IS NOT NULL 谓词。</summary>
        public virtual string isNotNull(string expr) => $"{expr} IS NOT NULL";

        /// <summary>LIKE 谓词（方言可 override ESCAPE）。</summary>
        public virtual string like(string expr, string pattern)
            => $"{expr} LIKE {pattern}";

        /// <summary>LIKE … ESCAPE 谓词片段。</summary>
        public virtual string like(string expr, string pattern, string escape)
            => $"{expr} LIKE {pattern} ESCAPE {escape}";

        /// <summary>SUBSTRING / 子串（默认 SUBSTRING，方言可 override）。</summary>
        public virtual string substring(string expr, string start, string? length = null)
            => length == null ? $"SUBSTRING({expr}, {start})" : $"SUBSTRING({expr}, {start}, {length})";

        /// <summary>DATEADD 片段（默认 SQL Server；MySQL/SQLite 等方言 override dateAdd*）。</summary>
        public virtual string dateAdd(string part, string amount, string date)
            => $"DATEADD({part}, {amount}, {date})";

        /// <summary>DateAdd 天；{0}=amount，{1}=date。</summary>
        public virtual string? dateAddDay(string amount, string date) => $"DATEADD(Day, Cast({amount} As Int), {date})";

        /// <summary>DateAdd 年。</summary>
        public virtual string? dateAddYear(string amount, string date) => $"DATEADD(Year, Cast({amount} As Int), {date})";

        /// <summary>DateAdd 季度。</summary>
        public virtual string? dateAddQuarter(string amount, string date) => $"DATEADD(Quarter, Cast({amount} As Int), {date})";

        /// <summary>DateAdd 月。</summary>
        public virtual string? dateAddMonth(string amount, string date) => $"DATEADD(Month, Cast({amount} As Int), {date})";

        /// <summary>DateAdd 周。</summary>
        public virtual string? dateAddWeek(string amount, string date) => $"DATEADD(Week, Cast({amount} As Int), {date})";

        /// <summary>DateAdd 小时。</summary>
        public virtual string? dateAddHour(string amount, string date) => $"DATEADD(Hour, Cast({amount} As Int), {date})";

        /// <summary>DateAdd 分钟。</summary>
        public virtual string? dateAddMinute(string amount, string date) => $"DATEADD(Minute, Cast({amount} As Int), {date})";

        /// <summary>DateAdd 秒。</summary>
        public virtual string? dateAddSecond(string amount, string date) => $"DATEADD(Second, Cast({amount} As Int), {date})";

        /// <summary>DateAdd 毫秒。</summary>
        public virtual string? dateAddMillisecond(string amount, string date) => $"DATEADD(Millisecond, Cast({amount} As Int), {date})";

        /// <summary>DateAdd 年中日序 / 星期（按天计）。</summary>
        public virtual string? dateAddDayOfYear(string amount, string date) => dateAddDay(amount, date);

        /// <summary>DateAdd 星期几偏移（按天计）。</summary>
        public virtual string? dateAddWeekDay(string amount, string date) => dateAddDay(amount, date);

        /// <summary>ROW_NUMBER 窗口片段（方言可 override）。</summary>
        public virtual string rowNumber(string orderBy) => $"ROW_NUMBER() OVER(ORDER BY {orderBy})";

        /// <summary>IN 列表谓词片段（字面量列表由 Ext 编译层展开）。</summary>
        public virtual string inList(string expr, string values) => $"{expr} IN ({values})";

        /// <summary>字符串拼接（复用 stringConcat 链）。</summary>
        public virtual string concat(string left, string right)
            => stringConcat(left, right);

        public virtual string lower(string expr) => $"LOWER({expr})";
        public virtual string upper(string expr) => $"UPPER({expr})";
        public virtual string trim(string expr) => $"TRIM({expr})";
        public virtual string length(string expr) => $"LENGTH({expr})";

        /// <summary>COALESCE 片段。</summary>
        public virtual string coalesce(string left, string right) => $"COALESCE({left}, {right})";

        /// <summary>NULLIF 片段。</summary>
        public virtual string nullIf(string left, string right) => $"NULLIF({left}, {right})";

        /// <summary>DateDiff 天；{0}=start，{1}=end。方言 override，默认 null 表示走 Ext Extension Builder。</summary>
        public virtual string? dateDiffDay(string start, string end) => null;

        /// <summary>DateDiff 小时。</summary>
        public virtual string? dateDiffHour(string start, string end) => null;

        /// <summary>DateDiff 分钟。</summary>
        public virtual string? dateDiffMinute(string start, string end) => null;

        /// <summary>DateDiff 秒。</summary>
        public virtual string? dateDiffSecond(string start, string end) => null;

        /// <summary>DateDiff 毫秒。</summary>
        public virtual string? dateDiffMillisecond(string start, string end) => null;

        /// <summary>DateDiff 年。</summary>
        public virtual string? dateDiffYear(string start, string end) => null;

        /// <summary>DateDiff 月。</summary>
        public virtual string? dateDiffMonth(string start, string end) => null;

        /// <summary>DateDiff 周。</summary>
        public virtual string? dateDiffWeek(string start, string end) => null;

        /// <summary>DateDiff 季度。</summary>
        public virtual string? dateDiffQuarter(string start, string end) => null;

        /// <summary>DatePart 年；{0}=date。</summary>
        public virtual string? datePartYear(string date) => null;

        /// <summary>DatePart 月。</summary>
        public virtual string? datePartMonth(string date) => null;

        /// <summary>DatePart 日。</summary>
        public virtual string? datePartDay(string date) => null;

        /// <summary>DatePart 小时。</summary>
        public virtual string? datePartHour(string date) => null;

        /// <summary>DatePart 分钟。</summary>
        public virtual string? datePartMinute(string date) => null;

        /// <summary>DatePart 秒。</summary>
        public virtual string? datePartSecond(string date) => null;

        /// <summary>DatePart 年中日序。</summary>
        public virtual string? datePartDayOfYear(string date) => null;

        /// <summary>DatePart 季度。</summary>
        public virtual string? datePartQuarter(string date) => null;

        /// <summary>DatePart 周。</summary>
        public virtual string? datePartWeek(string date) => null;

        /// <summary>DatePart 星期（0=Sunday …）。</summary>
        public virtual string? datePartWeekDay(string date) => null;

        /// <summary>DatePart 毫秒。</summary>
        public virtual string? datePartMillisecond(string date) => null;

        /// <summary>COLLATE 片段（{0}=expr；collation 名由调用方校验后嵌入）。</summary>
        public virtual string collate(string exprPlaceholder, string collationLiteral)
            => $"{exprPlaceholder} COLLATE {collationLiteral}";

        /// <summary>DB2 LUW：COLLATION_KEY_BIT({0}, 'collation')。</summary>
        public virtual string collateDb2(string exprPlaceholder, string collationLiteral)
            => $"COLLATION_KEY_BIT({exprPlaceholder}, '{collationLiteral.Replace("'", "''")}')";

        /// <summary>窗口函数 OVER 子句包装（Phase F P1 IR）。</summary>
        public virtual string windowOver(string functionSql, string overBody)
            => string.IsNullOrEmpty(overBody)
                ? $"{functionSql} OVER ()"
                : $"{functionSql} OVER ({overBody})";

        /// <summary>单参数数学函数（registry-first 默认 SQL Server 风格）。</summary>
        public virtual string abs(string expr) => $"ABS({expr})";
        public virtual string acos(string expr) => $"ACOS({expr})";
        public virtual string asin(string expr) => $"ASIN({expr})";
        public virtual string atan(string expr) => $"ATAN({expr})";
        public virtual string ceiling(string expr) => $"CEILING({expr})";
        public virtual string cos(string expr) => $"COS({expr})";
        public virtual string cosh(string expr) => $"COSH({expr})";
        public virtual string exp(string expr) => $"EXP({expr})";
        public virtual string floor(string expr) => $"FLOOR({expr})";
        public virtual string log(string expr) => $"LOG({expr})";
        public virtual string log10(string expr) => $"LOG10({expr})";
        public virtual string sign(string expr) => $"SIGN({expr})";
        public virtual string sin(string expr) => $"SIN({expr})";
        public virtual string sinh(string expr) => $"SINH({expr})";
        public virtual string sqrt(string expr) => $"SQRT({expr})";
        public virtual string tan(string expr) => $"TAN({expr})";
        public virtual string tanh(string expr) => $"TANH({expr})";

        /// <summary>CharIndex(substring, str) — 1-based；方言 override 于 Express。</summary>
        public virtual string charIndex(string substring, string str) => $"CHARINDEX({substring}, {str})";

        /// <summary>CharIndex(substring, str, start) — 1-based start。</summary>
        public virtual string charIndex(string substring, string str, string start)
            => $"CHARINDEX({substring}, {str}, {start})";
    }
}
