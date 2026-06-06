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

        /// <summary>SUBSTRING / 子串（默认 SUBSTRING，方言可 override）。</summary>
        public virtual string substring(string expr, string start, string? length = null)
            => length == null ? $"SUBSTRING({expr}, {start})" : $"SUBSTRING({expr}, {start}, {length})";

        /// <summary>DATEADD 片段（默认 DATEADD，MySQL 等方言 override）。</summary>
        public virtual string dateAdd(string part, string amount, string date)
            => $"DATEADD({part}, {amount}, {date})";

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
    }
}
