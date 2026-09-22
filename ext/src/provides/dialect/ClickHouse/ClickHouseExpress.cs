#if NET6_0_OR_GREATER
using mooSQL.data.builder;
using System;
using System.Text;

namespace mooSQL.data
{
    /// <summary>
    /// ClickHouse SQL：LIMIT/OFFSET、反引号、@ 参数、date_diff；无 ANSI MERGE / SERIAL。
    /// </summary>
    public class ClickHouseExpress : SQLExpression
    {
        public ClickHouseExpress(Dialect dia) : base(dia)
        {
            _paraPrefix = "@";
            _selectAutoIncrement = "";
            _provideType = "ClickHouse.Driver.ADO.ClickHouseConnectionFactory,ClickHouse.Driver";
        }

        public override string wrapKeyword(string value)
        {
            if (value.StartsWith("`") && value.EndsWith("`"))
                return value;
            return "`" + value.Replace("`", "``") + "`";
        }

        public override string charIndex(string substring, string str)
            => $"position({str}, {substring})";

        public override string charIndex(string substring, string str, string start)
            => $"(CASE WHEN {start} <= 0 THEN 0 ELSE position(substring({str}, toInt32({start})), {substring}) + toInt32({start}) - 1 END)";

        public override string isNullOrWhiteSpace(string expr)
            => $"({expr} IS NULL OR trimBoth({expr}) = '')";

        public override string substring(string expr, string start, string? length = null)
            => length == null
                ? $"substring({expr}, toInt32({start}))"
                : $"substring({expr}, toInt32({start}), toInt32({length}))";

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

        public override string? datePartYear(string date) => $"toYear(toDateTime64({date}, 3))";
        public override string? datePartQuarter(string date) => $"toQuarter(toDateTime64({date}, 3))";
        public override string? datePartMonth(string date) => $"toMonth(toDateTime64({date}, 3))";
        public override string? datePartDay(string date) => $"toDayOfMonth(toDateTime64({date}, 3))";
        public override string? datePartDayOfYear(string date) => $"toDayOfYear(toDateTime64({date}, 3))";
        public override string? datePartWeek(string date) => $"toISOWeek(toDateTime64({date}, 3))";
        public override string? datePartWeekDay(string date) => $"(toDayOfWeek(toDateTime64({date}, 3)) % 7) + 1";
        public override string? datePartHour(string date) => $"toHour(toDateTime64({date}, 3))";
        public override string? datePartMinute(string date) => $"toMinute(toDateTime64({date}, 3))";
        public override string? datePartSecond(string date) => $"toSecond(toDateTime64({date}, 3))";
        public override string? datePartMillisecond(string date)
            => $"toUnixTimestamp64Milli(toDateTime64({date}, 3)) % 1000";

        static string DateAddFn(string fn, string amount, string date)
            => $"{fn}(toDateTime64({date}, 3), {amount})";

        public override string? dateAddDay(string amount, string date) => DateAddFn("addDays", amount, date);
        public override string? dateAddMonth(string amount, string date) => DateAddFn("addMonths", amount, date);
        public override string? dateAddYear(string amount, string date) => DateAddFn("addYears", amount, date);
        public override string? dateAddHour(string amount, string date) => DateAddFn("addHours", amount, date);
        public override string? dateAddMinute(string amount, string date) => DateAddFn("addMinutes", amount, date);
        public override string? dateAddSecond(string amount, string date) => DateAddFn("addSeconds", amount, date);
        public override string? dateAddWeek(string amount, string date) => DateAddFn("addWeeks", amount, date);
        public override string? dateAddQuarter(string amount, string date)
            => $"addMonths(toDateTime64({date}, 3), ({amount}) * 3)";
        public override string? dateAddMillisecond(string amount, string date)
            => $"fromUnixTimestamp64Milli(toUnixTimestamp64Milli(toDateTime64({date}, 3)) + toInt64({amount}))";

        public override string buildSelect(FragSQL frag)
        {
            var sb = new StringBuilder();
            sb.Append("SELECT ");
            if (frag.distincted)
                sb.Append("DISTINCT ");
            sb.Append(frag.selectInner);
            buildSelectFromToOrderPart(frag, sb);
            AppendLimitOffset(sb, frag);
            return sb.ToString();
        }

        protected override string WrapExistScalar(string existsSubquery)
            => $"SELECT if(exists({existsSubquery}), 1, 0)";

        protected override string AppendExistSubqueryTail(string innerSql, FragSQL frag)
            => innerSql + " LIMIT 1";

        public override string buildPagedSelect(FragSQL frag) => buildSelect(frag);

        public override string buildInsert(FragSQL frag)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("INSERT INTO {0} ", frag.insertInto);
            if (!string.IsNullOrWhiteSpace(frag.insertCols))
                sb.AppendFormat(" ({0}) ", frag.insertCols);

            if (frag.insertValues != null && frag.insertValues.Count > 0)
            {
                sb.AppendFormat(" VALUES ({0})", string.Join("),(", frag.insertValues));
                return sb.ToString();
            }

            if (!string.IsNullOrWhiteSpace(frag.fromInner) || !string.IsNullOrWhiteSpace(frag.selectInner))
            {
                sb.Append(" SELECT ");
                if (frag.distincted)
                    sb.Append("DISTINCT ");
                if (!string.IsNullOrWhiteSpace(frag.selectInner))
                    sb.AppendFormat(" {0} ", frag.selectInner);
                else
                    sb.AppendFormat(" {0} ", frag.insertValue);

                if (!string.IsNullOrWhiteSpace(frag.fromInner))
                {
                    sb.AppendFormat(" FROM {0} ", frag.fromInner);
                    if (!string.IsNullOrWhiteSpace(frag.whereInner))
                        sb.AppendFormat(" WHERE {0} ", frag.whereInner);
                    if (!string.IsNullOrWhiteSpace(frag.groupByInner))
                    {
                        sb.Append("GROUP BY ");
                        sb.Append(frag.groupByInner);
                        sb.Append(' ');
                    }
                    if (!string.IsNullOrWhiteSpace(frag.havingInner))
                    {
                        sb.Append("HAVING ");
                        sb.Append(frag.havingInner);
                        sb.Append(' ');
                    }
                }
                return sb.ToString();
            }

            if (!string.IsNullOrWhiteSpace(frag.insertValue))
            {
                sb.AppendFormat(" VALUES ({0}) ", frag.insertValue);
                return sb.ToString();
            }
            throw new Exception("SQL语句不完整！无法构造！");
        }

        public override string buildMergeInto(FragMergeInto frag)
            => throw new NotSupportedException(
                "ClickHouse 不支持 ANSI MERGE；请用 INSERT 或 ReplacingMergeTree。");

        #region DDL

        public override string getTableAutoIdSQL() => "";

        public override string CreateDataBaseBy(string database)
            => string.Format("CREATE DATABASE IF NOT EXISTS {0}", database);

        public override string AddPrimaryKeyBy(string tableName, string columnName, string indexName)
            => throw new NotSupportedException("ClickHouse 建表后通常不能 ALTER ADD PRIMARY KEY；请在 CREATE TABLE 时声明。");

        public override string AddColumnToTableBy(string tableName, string columnName, string dataType, string defval, string nullable, string p2, string p3)
            => string.Format("ALTER TABLE {0} ADD COLUMN {1} {2}{3}",
                tableName, columnName, dataType, string.IsNullOrWhiteSpace(defval) ? "" : " " + defval);

        public override string AlterColumnToTableby(string tableName, string columnName, string dataType, string defval, string nullable, string p2, string p3)
            => string.Format("ALTER TABLE {0} MODIFY COLUMN {1} {2}", tableName, columnName, dataType);

        public override string CreateTableBy(string tableName, string detail)
            => string.Format(
                "CREATE TABLE {0}(\r\n{1}\r\n) ENGINE = MergeTree ORDER BY tuple()",
                tableName, detail);

        public override string CreateTableColumnBy(string columnName, string dataType, string defval, string nullable, string p2, string p3)
            => string.Format("{0} {1}{2} {3} {4} {5}",
                columnName,
                dataType ?? string.Empty,
                defval ?? string.Empty,
                nullable ?? string.Empty,
                p2 ?? string.Empty,
                p3 ?? string.Empty
            ).Trim();

        public override string DropColumnToTableBy(string tableName, string columnName)
            => string.Format("ALTER TABLE {0} DROP COLUMN {1}", tableName, columnName);

        public override string DropConstraintBy(string tableName, string constraintName)
            => throw new NotSupportedException("ClickHouse 不支持 DROP CONSTRAINT。");

        public override string RenameColumnBy(string tableName, string oldName, string newName)
            => string.Format("ALTER TABLE {0} RENAME COLUMN {1} TO {2}", tableName, oldName, newName);

        public override string CreateTableNullBy() => "NULL";
        public override string CreateTableNotNullBy() => "NOT NULL";
        public override string CreateTablePirmaryKeyBy() => "";

        public override string buildSoloFieldCaption(DDLFragSQL frag, DDLField fie) => "";
        public override string buildSoloTableCaption(DDLFragSQL frag) => "";
        public override string AddTableCaptionBy(string tableName, string caption)
            => string.Format("ALTER TABLE {0} MODIFY COMMENT '{1}'", tableName, caption?.Replace("'", "''"));
        public override string UpdateTableCaptionBy(string tableName, string caption)
            => AddTableCaptionBy(tableName, caption);
        public override string DeleteTableCaptionBy(string tableName) => "";
        public override string AddColumnCaptionBy(string tableName, string columnName, string caption)
            => string.Format("ALTER TABLE {0} COMMENT COLUMN {1} '{2}'", tableName, columnName, caption?.Replace("'", "''"));
        public override string UpdateColumnCaptionBy(string tableName, string columnName, string caption)
            => AddColumnCaptionBy(tableName, columnName, caption);
        public override string DeleteColumnCaptionBy(string tableName, string columnName) => "";

        public override string RenameTableBy(string oldTableName, string newTableName)
            => string.Format("RENAME TABLE {0} TO {1}", oldTableName, newTableName);

        public override string CreateIndexBy(string indexName, string tableName, string columnName, string unique)
            => string.Format("ALTER TABLE {0} ADD INDEX {2} {1} TYPE minmax GRANULARITY 4", tableName, columnName, indexName);

        public override string IsAnyIndexBy(string indexName)
            => string.Format(
                "SELECT count() FROM system.data_skipping_indices WHERE name = '{0}'",
                indexName?.Replace("'", "''"));

        public override string buildDropIndex(string indexName, string tableName = null)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new NotSupportedException("ClickHouse DROP INDEX 需要表名。");
            return string.Format("ALTER TABLE {0} DROP INDEX {1}", tableName, indexName);
        }

        public override string getDateTimeColumnType(int length) => "DateTime64(3)";
        public override string getBoolColumnType() => "UInt8";
        public override string getGuidColumnType() => "UUID";

        #endregion
    }
}
#endif
