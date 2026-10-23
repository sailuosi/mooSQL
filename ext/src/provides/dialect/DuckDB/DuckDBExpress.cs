#if NET6_0_OR_GREATER
using mooSQL.data.builder;
using System;
using System.Text;

namespace mooSQL.data
{
    /// <summary>
    /// DuckDB SQL 方言：LIMIT/OFFSET、双引号标识符、$ 命名参数、IDENTITY / SEQUENCE。
    /// </summary>
    public class DuckDBExpress : SQLExpression
    {
        /// <summary>
        /// DuckDBExpress type.
        /// </summary>
        /// <summary>
        /// DuckDBExpress type.
        /// </summary>
        public DuckDBExpress(Dialect dia) : base(dia)
        {
            _paraPrefix = "$";
            // 无 last_insert_rowid；IDENTITY / SEQUENCE + RETURNING 由插入路径处理
            _selectAutoIncrement = "";
            _provideType = "DuckDB.NET.Data.DuckDBClientFactory,DuckDB.NET.Data";
        }

        /// <inheritdoc/>
        protected override System.Collections.Generic.HashSet<string> GetReservedWords()
            => SqlReservedWords.PostgreSQL;
/// <summary>
/// Quotes an identifier for this dialect.
/// </summary>
 

        public override string wrapKeyword(string value)
        {
            if (value.StartsWith("\"") && value.EndsWith("\""))
                return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
/// <summary>
/// Builds a character-index expression.
/// </summary>
 

        public override string charIndex(string substring, string str) => $"STRPOS({str}, {substring})";
/// <summary>
/// Builds a character-index expression.
/// </summary>
 

        public override string charIndex(string substring, string str, string start)
            => $"(CASE WHEN {start} <= 0 THEN 0 ELSE STRPOS(SUBSTRING({str} FROM {start}::BIGINT), {substring}) + {start}::BIGINT - 1 END)";
/// <summary>
/// Builds an is-null-or-whitespace expression.
/// </summary>
 

        public override string isNullOrWhiteSpace(string expr)
            => $"({expr} IS NULL OR TRIM({expr}) = '')";
/// <summary>
/// Builds a substring expression.
/// </summary>
 

        public override string substring(string expr, string start, string? length = null)
            => length == null ? $"SUBSTRING({expr} FROM {start})" : $"SUBSTRING({expr} FROM {start} FOR {length})";
/// <summary>
/// Builds a date-difference expression in Day.
/// </summary>
 

        public override string dateDiffDay(string start, string end)
            => $"date_diff('day', {start}::TIMESTAMP, {end}::TIMESTAMP)";
/// <summary>
/// Builds a date-difference expression in Hour.
/// </summary>
 

        public override string dateDiffHour(string start, string end)
            => $"date_diff('hour', {start}::TIMESTAMP, {end}::TIMESTAMP)";
/// <summary>
/// Builds a date-difference expression in Minute.
/// </summary>
 

        public override string dateDiffMinute(string start, string end)
            => $"date_diff('minute', {start}::TIMESTAMP, {end}::TIMESTAMP)";
/// <summary>
/// Builds a date-difference expression in Second.
/// </summary>
 

        public override string dateDiffSecond(string start, string end)
            => $"date_diff('second', {start}::TIMESTAMP, {end}::TIMESTAMP)";
/// <summary>
/// Builds a date-difference expression in Millisecond.
/// </summary>
 

        public override string dateDiffMillisecond(string start, string end)
            => $"date_diff('millisecond', {start}::TIMESTAMP, {end}::TIMESTAMP)";
/// <summary>
/// Builds a date-difference expression in Year.
/// </summary>
 

        public override string dateDiffYear(string start, string end)
            => $"date_diff('year', {start}::TIMESTAMP, {end}::TIMESTAMP)";
/// <summary>
/// Builds a date-difference expression in Month.
/// </summary>
 

        public override string dateDiffMonth(string start, string end)
            => $"date_diff('month', {start}::TIMESTAMP, {end}::TIMESTAMP)";
/// <summary>
/// Builds a date-difference expression in Week.
/// </summary>
 

        public override string dateDiffWeek(string start, string end)
            => $"date_diff('week', {start}::TIMESTAMP, {end}::TIMESTAMP)";

        static string DuckDatePart(string part, string date)
            => $"CAST(date_part('{part}', {date}::TIMESTAMP) AS INTEGER)";
/// <summary>
/// Builds a date-part expression for Year.
/// </summary>
 

        public override string? datePartYear(string date) => DuckDatePart("year", date);
 /// <summary>
 /// Builds a date-part expression for Quarter.
 /// </summary>
 
        public override string? datePartQuarter(string date) => DuckDatePart("quarter", date);
 /// <summary>
 /// Builds a date-part expression for Month.
 /// </summary>
 
        public override string? datePartMonth(string date) => DuckDatePart("month", date);
 /// <summary>
 /// Builds a date-part expression for Day.
 /// </summary>
 
        public override string? datePartDay(string date) => DuckDatePart("day", date);
 /// <summary>
 /// Builds a date-part expression for DayOfYear.
 /// </summary>
 
        public override string? datePartDayOfYear(string date) => DuckDatePart("doy", date);
 /// <summary>
 /// Builds a date-part expression for Week.
 /// </summary>
 
        public override string? datePartWeek(string date) => DuckDatePart("week", date);
 /// <summary>
 /// Builds a date-part expression for WeekDay.
 /// </summary>
 
        public override string? datePartWeekDay(string date) => $"({DuckDatePart("dow", date)} + 1)";
 /// <summary>
 /// Builds a date-part expression for Hour.
 /// </summary>
 
        public override string? datePartHour(string date) => DuckDatePart("hour", date);
 /// <summary>
 /// Builds a date-part expression for Minute.
 /// </summary>
 
        public override string? datePartMinute(string date) => DuckDatePart("minute", date);
 /// <summary>
 /// Builds a date-part expression for Second.
 /// </summary>
 
        public override string? datePartSecond(string date) => DuckDatePart("second", date);
 /// <summary>
 /// Builds a date-part expression for Millisecond.
 /// </summary>
 
        public override string? datePartMillisecond(string date)
            => $"CAST((date_part('millisecond', {date}::TIMESTAMP)) AS INTEGER) % 1000";

        static string DuckDateAdd(string unit, string amount, string date)
            => $"({date}::TIMESTAMP + ({amount} || ' {unit}')::INTERVAL)";
/// <summary>
/// Builds a date-add expression for Day.
/// </summary>
 

        public override string? dateAddDay(string amount, string date) => DuckDateAdd("day", amount, date);
 /// <summary>
 /// Builds a date-add expression for Month.
 /// </summary>
 
        public override string? dateAddMonth(string amount, string date) => DuckDateAdd("month", amount, date);
 /// <summary>
 /// Builds a date-add expression for Year.
 /// </summary>
 
        public override string? dateAddYear(string amount, string date) => DuckDateAdd("year", amount, date);
 /// <summary>
 /// Builds a date-add expression for Hour.
 /// </summary>
 
        public override string? dateAddHour(string amount, string date) => DuckDateAdd("hour", amount, date);
 /// <summary>
 /// Builds a date-add expression for Minute.
 /// </summary>
 
        public override string? dateAddMinute(string amount, string date) => DuckDateAdd("minute", amount, date);
 /// <summary>
 /// Builds a date-add expression for Second.
 /// </summary>
 
        public override string? dateAddSecond(string amount, string date) => DuckDateAdd("second", amount, date);
 /// <summary>
 /// Builds a date-add expression for Week.
 /// </summary>
 
        public override string? dateAddWeek(string amount, string date) => DuckDateAdd("week", amount, date);
 /// <summary>
 /// Builds a date-add expression for Quarter.
 /// </summary>
 
        public override string? dateAddQuarter(string amount, string date)
            => $"({date}::TIMESTAMP + (({amount}) || ' month')::INTERVAL * 3)";
 /// <summary>
 /// Builds a date-add expression for Millisecond.
 /// </summary>
 
        public override string? dateAddMillisecond(string amount, string date) => DuckDateAdd("millisecond", amount, date);
/// <summary>
/// Builds a SELECT statement.
/// </summary>
 

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
/// <summary>
/// Wraps a scalar expression for EXISTS checks.
/// </summary>
 

        protected override string WrapExistScalar(string existsSubquery)
            => $"SELECT EXISTS({existsSubquery})";
/// <summary>
/// Appends the EXISTS subquery tail.
/// </summary>
 

        protected override string AppendExistSubqueryTail(string innerSql, FragSQL frag)
            => innerSql + " LIMIT 1";
/// <summary>
/// Builds a paged SELECT statement.
/// </summary>
 

        public override string buildPagedSelect(FragSQL frag) => buildSelect(frag);
/// <summary>
/// Builds an INSERT statement.
/// </summary>
 

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
/// <summary>
/// Builds a MERGE INTO statement.
/// </summary>
 

        public override string buildMergeInto(FragMergeInto frag)
            => buildMergeIntoGeneral(frag);

        #region DDL
/// <summary>
/// Returns SQL for retrieving an auto-generated identity value.
/// </summary>
 

        public override string getTableAutoIdSQL()
            => "DEFAULT nextval('moo_auto_seq')";
/// <summary>
/// Builds a CREATE DATABASE statement.
/// </summary>
 

        public override string CreateDataBaseBy(string database) => string.Empty;
/// <summary>
/// Builds an ADD PRIMARY KEY statement.
/// </summary>
 

        public override string AddPrimaryKeyBy(string tableName, string columnName, string indexName)
            => string.Format("ALTER TABLE {0} ADD PRIMARY KEY ({1})", tableName, columnName);
/// <summary>
/// Builds an ADD COLUMN statement.
/// </summary>
 

        public override string AddColumnToTableBy(string tableName, string columnName, string dataType, string defval, string nullable, string p2, string p3)
            => string.Format("ALTER TABLE {0} ADD COLUMN {1} {2}{3} {4} {5} {6}",
                tableName, columnName, dataType, defval, nullable, p2, p3);
/// <summary>
/// Builds an ALTER COLUMN statement.
/// </summary>
 

        public override string AlterColumnToTableby(string tableName, string columnName, string dataType, string defval, string nullable, string p2, string p3)
            => string.Format("ALTER TABLE {0} ALTER COLUMN {1} TYPE {2}", tableName, columnName, dataType);
/// <summary>
/// Builds a CREATE TABLE statement.
/// </summary>
 

        public override string CreateTableBy(string tableName, string detail)
        {
            // DuckDB 无可靠 IDENTITY+PK 组合；自增走 SEQUENCE + nextval
            if (!string.IsNullOrEmpty(detail) &&
                detail.IndexOf("nextval", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return string.Format(
                    "CREATE SEQUENCE IF NOT EXISTS moo_auto_seq;{0}CREATE TABLE {1}(\r\n{2})",
                    Environment.NewLine, tableName, detail);
            }
            return string.Format("CREATE TABLE {0}(\r\n{1})", tableName, detail);
        }
/// <summary>
/// Builds a CREATE TABLE column definition.
/// </summary>
 

        public override string CreateTableColumnBy(string columnName, string dataType, string defval, string nullable, string p2, string p3)
        {
            if (!string.IsNullOrWhiteSpace(p3) &&
                p3.IndexOf("nextval", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (string.IsNullOrWhiteSpace(dataType))
                    dataType = "BIGINT";
                // p3 已含 DEFAULT nextval(...)；勿再拼 defval
                return string.Format("{0} {1} {2} {3} {4}",
                    columnName,
                    dataType,
                    p3.Trim(),
                    nullable ?? string.Empty,
                    p2 ?? string.Empty
                ).Trim();
            }

            return string.Format("{0} {1}{2} {3} {4} {5}",
                columnName,
                dataType ?? string.Empty,
                defval ?? string.Empty,
                nullable ?? string.Empty,
                p2 ?? string.Empty,
                p3 ?? string.Empty
            ).Trim();
        }
/// <summary>
/// Builds a DROP COLUMN statement.
/// </summary>
 

        public override string DropColumnToTableBy(string tableName, string columnName)
            => string.Format("ALTER TABLE {0} DROP COLUMN {1}", tableName, columnName);
/// <summary>
/// Builds a DROP CONSTRAINT statement.
/// </summary>
 

        public override string DropConstraintBy(string tableName, string constraintName)
            => string.Format("ALTER TABLE {0} DROP CONSTRAINT {1}", tableName, constraintName);
/// <summary>
/// Builds a RENAME COLUMN statement.
/// </summary>
 

        public override string RenameColumnBy(string tableName, string oldName, string newName)
            => string.Format("ALTER TABLE {0} RENAME COLUMN {1} TO {2}", tableName, oldName, newName);
/// <summary>
/// Returns the NULL column qualifier for CREATE TABLE.
/// </summary>
 

        public override string CreateTableNullBy() => "NULL";
 /// <summary>
 /// Returns the NOT NULL column qualifier for CREATE TABLE.
 /// </summary>
 
        public override string CreateTableNotNullBy() => "NOT NULL";
 /// <summary>
 /// Returns the PRIMARY KEY qualifier for CREATE TABLE.
 /// </summary>
 
        public override string CreateTablePirmaryKeyBy() => "PRIMARY KEY";
/// <summary>
/// Builds SQL to add a table caption/comment.
/// </summary>
 

        public override string AddTableCaptionBy(string tableName, string caption)
            => string.Format("COMMENT ON TABLE {0} IS '{1}'", tableName, caption);
/// <summary>
/// Builds SQL to update a table caption/comment.
/// </summary>
 

        public override string UpdateTableCaptionBy(string tableName, string caption)
            => AddTableCaptionBy(tableName, caption);
/// <summary>
/// Builds SQL to delete a table caption/comment.
/// </summary>
 

        public override string DeleteTableCaptionBy(string tableName)
            => string.Format("COMMENT ON TABLE {0} IS ''", tableName);
/// <summary>
/// Builds SQL to add a column caption/comment.
/// </summary>
 

        public override string AddColumnCaptionBy(string tableName, string columnName, string caption)
            => string.Format("COMMENT ON COLUMN {0}.{1} IS '{2}'", tableName, columnName, caption);
/// <summary>
/// Builds SQL to update a column caption/comment.
/// </summary>
 

        public override string UpdateColumnCaptionBy(string tableName, string columnName, string caption)
            => AddColumnCaptionBy(tableName, columnName, caption);
/// <summary>
/// Builds SQL to delete a column caption/comment.
/// </summary>
 

        public override string DeleteColumnCaptionBy(string tableName, string columnName)
            => string.Format("COMMENT ON COLUMN {0}.{1} IS ''", tableName, columnName);
/// <summary>
/// Builds a RENAME TABLE statement.
/// </summary>
 

        public override string RenameTableBy(string oldTableName, string newTableName)
            => string.Format("ALTER TABLE {0} RENAME TO {1}", oldTableName, newTableName);
/// <summary>
/// Builds a CREATE INDEX statement.
/// </summary>
 

        public override string CreateIndexBy(string indexName, string tableName, string columnName, string unique)
            => string.Format("CREATE {3} INDEX {2} ON {0} ({1})", tableName, columnName, indexName, unique);
/// <summary>
/// Builds SQL that checks whether an index exists.
/// </summary>
 

        public override string IsAnyIndexBy(string indexName)
            => string.Format(
                "SELECT COUNT(*) FROM duckdb_indexes() WHERE index_name = '{0}'",
                indexName);
/// <summary>
/// Builds a DROP INDEX statement.
/// </summary>
 

        public override string buildDropIndex(string indexName, string tableName = null)
            => string.Format("DROP INDEX {0}", indexName);

        #endregion
    }
}
#endif
