
using mooSQL.data.builder;
using System;
using System.Text;

namespace mooSQL.data
{
    /// <summary>
    /// SQLite的特性语法
    /// </summary>
    public class SQLiteExpress:SQLExpression
    {
        /// <summary>
        /// SQLiteExpress type.
        /// </summary>
        /// <summary>
        /// SQLiteExpress type.
        /// </summary>
        public SQLiteExpress(Dialect dia) : base(dia) {
            // Microsoft.Data.Sqlite 使用 @name 命名参数；?paramName 会导致语法错误
            _paraPrefix = "@";
            _selectAutoIncrement = "SELECT last_insert_rowid()";
            _provideType = "SQLite.Data.SQLiteClient.SQLiteClientFactory,SQLite.Data";
        }

        /// <inheritdoc/>
        protected override System.Collections.Generic.HashSet<string> GetReservedWords()
            => SqlReservedWords.SQLite;
/// <summary>
/// Quotes an identifier for this dialect.
/// </summary>
 

        public override string wrapKeyword(string value)
        {
            if (value.StartsWith("[") && value.EndsWith("]"))
            {
                return value;
            }
            return "[" + value + "]";
        }
/// <summary>
/// Builds a date-difference expression in Day.
/// </summary>
 

        public override string dateDiffDay(string start, string end)
            => $"round((julianday({end}) - julianday({start})))";
/// <summary>
/// Builds a character-index expression.
/// </summary>
 

        public override string charIndex(string substring, string str) => $"INSTR({str}, {substring})";
/// <summary>
/// Builds a character-index expression.
/// </summary>
 

        public override string charIndex(string substring, string str, string start)
            => $"(CASE WHEN {start} < 1 THEN 0 ELSE INSTR(SUBSTR({str}, {start}), {substring}) + {start} - 1 END)";
/// <summary>
/// Builds an is-null-or-whitespace expression.
/// </summary>
 

        public override string isNullOrWhiteSpace(string expr)
            => $"({expr} IS NULL OR TRIM({expr}) = '')";
/// <summary>
/// Builds a date-difference expression in Hour.
/// </summary>
 

        public override string dateDiffHour(string start, string end)
            => $"round((julianday({end}) - julianday({start})) * 24)";
/// <summary>
/// Builds a date-difference expression in Minute.
/// </summary>
 

        public override string dateDiffMinute(string start, string end)
            => $"round((julianday({end}) - julianday({start})) * 1440)";
/// <summary>
/// Builds a date-difference expression in Second.
/// </summary>
 

        public override string dateDiffSecond(string start, string end)
            => $"round((julianday({end}) - julianday({start})) * 86400)";
/// <summary>
/// Builds a date-difference expression in Millisecond.
/// </summary>
 

        public override string dateDiffMillisecond(string start, string end)
            => $"round((julianday({end}) - julianday({start})) * 86400000)";
/// <summary>
/// Builds a substring expression.
/// </summary>
 

        public override string substring(string expr, string start, string? length = null)
            => length == null ? $"Substr({expr}, {start})" : $"Substr({expr}, {start}, {length})";

        static string StrftimeInt(string format, string date)
            => $"Cast(Strftime('{format}', {date}) As Integer)";
/// <summary>
/// Builds a date-part expression for Year.
/// </summary>
 

        public override string datePartYear(string date) => StrftimeInt("%Y", date);
/// <summary>
/// Builds a date-part expression for Month.
/// </summary>
 

        public override string datePartMonth(string date) => StrftimeInt("%m", date);
/// <summary>
/// Builds a date-part expression for Day.
/// </summary>
 

        public override string datePartDay(string date) => StrftimeInt("%d", date);
/// <summary>
/// Builds a date-part expression for Hour.
/// </summary>
 

        public override string datePartHour(string date) => StrftimeInt("%H", date);
/// <summary>
/// Builds a date-part expression for Minute.
/// </summary>
 

        public override string datePartMinute(string date) => StrftimeInt("%M", date);
/// <summary>
/// Builds a date-part expression for Second.
/// </summary>
 

        public override string datePartSecond(string date) => StrftimeInt("%S", date);
/// <summary>
/// Builds a date-part expression for DayOfYear.
/// </summary>
 

        public override string datePartDayOfYear(string date) => StrftimeInt("%j", date);
/// <summary>
/// Builds a date-part expression for Quarter.
/// </summary>
 

        public override string datePartQuarter(string date)
            => $"((Cast(Strftime('%m', {date}) As Integer)-1)/3+1)";
/// <summary>
/// Builds a date-part expression for Week.
/// </summary>
 

        public override string datePartWeek(string date) => StrftimeInt("%W", date);
/// <summary>
/// Builds a date-part expression for WeekDay.
/// </summary>
 

        public override string datePartWeekDay(string date) => StrftimeInt("%w", date);
/// <summary>
/// Builds a date-part expression for Millisecond.
/// </summary>
 

        public override string datePartMillisecond(string date)
            => $"Cast((Cast(Strftime('%f', {date}) As Real)*1000) As Integer) % 1000";

        static string SqliteDateAddModifier(string unit, string amount, string date)
            => $"Datetime({date}, '+' || Cast({amount} As Text) || ' {unit}')";
/// <summary>
/// Builds a date-add expression for Day.
/// </summary>
 

        public override string? dateAddDay(string amount, string date) => SqliteDateAddModifier("Days", amount, date);
/// <summary>
/// Builds a date-add expression for Month.
/// </summary>
 

        public override string? dateAddMonth(string amount, string date) => SqliteDateAddModifier("Months", amount, date);
/// <summary>
/// Builds a date-add expression for Year.
/// </summary>
 

        public override string? dateAddYear(string amount, string date) => SqliteDateAddModifier("Years", amount, date);
/// <summary>
/// Builds a date-add expression for Hour.
/// </summary>
 

        public override string? dateAddHour(string amount, string date) => SqliteDateAddModifier("Hours", amount, date);
/// <summary>
/// Builds a date-add expression for Minute.
/// </summary>
 

        public override string? dateAddMinute(string amount, string date) => SqliteDateAddModifier("Minutes", amount, date);
/// <summary>
/// Builds a date-add expression for Second.
/// </summary>
 

        public override string? dateAddSecond(string amount, string date) => SqliteDateAddModifier("Seconds", amount, date);
/// <summary>
/// Builds a date-add expression for Week.
/// </summary>
 

        public override string? dateAddWeek(string amount, string date)
            => $"Datetime({date}, '+' || Cast(({amount}) * 7 As Text) || ' Days')";
/// <summary>
/// Builds a date-add expression for Quarter.
/// </summary>
 

        public override string? dateAddQuarter(string amount, string date) => SqliteDateAddModifier("Months", $"({amount})*3", date);
/// <summary>
/// Builds a date-add expression for Millisecond.
/// </summary>
 

        public override string? dateAddMillisecond(string amount, string date)
            => $"Datetime({date}, '+' || Cast({amount} As Text) || ' Milliseconds')";

        /// <summary>
        /// 创建普通的select语句
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        public override string buildSelect(FragSQL frag)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT ");
            if (frag.distincted)
            {
                sb.Append("DISTINCT ");
            }
            sb.Append(frag.selectInner);
            this.buildSelectFromToOrderPart(frag, sb);
            AppendLimitOffset(sb, frag);

            return sb.ToString();
        }

        /// <inheritdoc/>
        protected override string WrapExistScalar(string existsSubquery)
            => $"SELECT EXISTS({existsSubquery})";

        /// <inheritdoc/>
        protected override string AppendExistSubqueryTail(string innerSql, FragSQL frag)
            => innerSql + " LIMIT 1";
/// <summary>
/// Builds a paged SELECT statement.
/// </summary>
 

        public override string buildPagedSelect(FragSQL frag) => buildSelect(frag);
        /// <summary>
        /// 创建普通的插值语句
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public override string buildInsert(FragSQL frag)
        {
            StringBuilder sb = new StringBuilder();
            // sql server 支持直接插入多行数据、单行数据
            sb.AppendFormat("INSERT INTO {0} ", frag.insertInto);
            if (string.IsNullOrWhiteSpace(frag.insertCols) == false)
            {
                sb.AppendFormat(" ({0}) ", frag.insertCols);
            }

            if (frag.insertValues != null && frag.insertValues.Count > 0)
            {
                //多行插入
                sb.AppendFormat(" VALUES ({0})", string.Join("),(", frag.insertValues));
                return sb.ToString();
            }
            //如果 from 不为空，则是 insert into  select...
            if (!string.IsNullOrWhiteSpace(frag.fromInner) || !string.IsNullOrWhiteSpace(frag.selectInner))
            {
                //此时的单行插入值，实际上是select 部分。但是，如果明确给了 select内容，则使用 select内容
                sb.Append(" SELECT ");
                if (frag.distincted)
                {
                    sb.Append("DISTINCT ");
                }
                if (!string.IsNullOrWhiteSpace(frag.selectInner))
                {
                    sb.AppendFormat(" {0} ", frag.selectInner);
                }
                else
                {
                    sb.AppendFormat(" {0} ", frag.insertValue);
                }
                //追加from 部分。
                if (!string.IsNullOrWhiteSpace(frag.fromInner))
                {
                    sb.AppendFormat(" FROM {0} ", frag.fromInner);

                    //带from 时，才允许追加 where条件
                    if (!string.IsNullOrWhiteSpace(frag.whereInner))
                    {
                        sb.AppendFormat(" WHERE {0} ", frag.whereInner);
                    }
                    if (!string.IsNullOrWhiteSpace(frag.groupByInner))
                    {
                        sb.Append("GROUP BY ");
                        sb.Append(frag.groupByInner);
                        sb.Append(" ");
                    }
                    if (!string.IsNullOrWhiteSpace(frag.havingInner))
                    {
                        sb.Append("HAVING ");
                        sb.Append(frag.havingInner);
                        sb.Append(" ");
                    }
                }

                return sb.ToString();
            }
            //如果是单行插入
            if (!string.IsNullOrWhiteSpace(frag.insertValue))
            {
                sb.AppendFormat(" VALUES ({0}) ", frag.insertValue);
                return sb.ToString();
            }
            throw new Exception("SQL语句不完整！无法构造！");
        }
        /// <summary>
        /// SQLite 3.33+：UPDATE target SET … FROM …（对齐 SQL Server 形态，非 MySQL JOIN 写法）。
        /// </summary>
        public override string buildUpdateFrom(FragSQL frag)
        {
            EnsureSqliteVersionAtLeast(3.33, "UPDATE … FROM");
            var sb = new StringBuilder();
            sb.AppendFormat("UPDATE {0} SET {1} ", frag.updateTo, this.buildSetPart(frag));
            sb.AppendFormat(" FROM {0} ", frag.fromInner);
            if (!string.IsNullOrWhiteSpace(frag.whereInner))
            {
                sb.AppendFormat(" WHERE {0}", frag.whereInner);
            }
            return sb.ToString();
        }

        /// <summary>
        /// SQLite：WITH batch_data(cols) AS (VALUES …) UPDATE SET col=(SELECT …) WHERE EXISTS。
        /// </summary>
        public override string buildUpdateBatch(FragUpdateBatch frag)
        {
            if (frag == null
                || string.IsNullOrWhiteSpace(frag.tableName)
                || frag.setColumns == null || frag.setColumns.Count == 0
                || frag.keyColumns == null || frag.keyColumns.Count == 0
                || frag.selectLists == null || frag.selectLists.Count == 0
                || frag.sourceAliases == null
                || frag.sourceAliases.Count != frag.setColumns.Count + frag.keyColumns.Count)
            {
                return null;
            }

            var table = normalizeName(frag.tableName);
            var sb = new StringBuilder();
            sb.Append("WITH batch_data(");
            for (var i = 0; i < frag.sourceAliases.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(normalizeName(frag.sourceAliases[i]));
            }
            sb.Append(") AS (VALUES ");
            for (var i = 0; i < frag.selectLists.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append('(');
                sb.Append(frag.selectLists[i]);
                sb.Append(')');
            }
            sb.Append(") UPDATE ");
            sb.Append(table);
            sb.Append(" SET ");
            for (var i = 0; i < frag.setColumns.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                var col = normalizeName(frag.setColumns[i]);
                var src = normalizeName(frag.sourceAliases[i]);
                sb.Append(col);
                sb.Append(" = (SELECT ");
                sb.Append(src);
                sb.Append(" FROM batch_data WHERE ");
                AppendSqliteBatchKeyMatch(sb, table, frag);
                sb.Append(')');
            }
            sb.Append(" WHERE EXISTS (SELECT 1 FROM batch_data WHERE ");
            AppendSqliteBatchKeyMatch(sb, table, frag);
            sb.Append(')');
            return sb.ToString();
        }

        void AppendSqliteBatchKeyMatch(StringBuilder sb, string quotedTable, FragUpdateBatch frag)
        {
            for (var k = 0; k < frag.keyColumns.Count; k++)
            {
                if (k > 0) sb.Append(" AND ");
                var aliasIdx = frag.setColumns.Count + k;
                var keyAlias = normalizeName(frag.sourceAliases[aliasIdx]);
                sb.Append(quotedTable);
                sb.Append('.');
                sb.Append(keyAlias);
                sb.Append(" = batch_data.");
                sb.Append(keyAlias);
            }
        }

        /// <summary>SQLite 无 TRUNCATE；用 DELETE FROM 清空表。</summary>
        public override string buildTruncateTable(DDLFragSQL frag)
        {
            return "DELETE FROM " + frag.Table;
        }

        #region DDL
/// <summary>
/// Returns SQL for retrieving an auto-generated identity value.
/// </summary>
 

        public override string getTableAutoIdSQL()
        {
            // SQLite 自增必须写在 CREATE TABLE 列定义中：INTEGER PRIMARY KEY AUTOINCREMENT
            return "AUTOINCREMENT";
        }
/// <summary>
/// Builds a CREATE DATABASE statement.
/// </summary>
 

        public override string CreateDataBaseBy(string database)
        {
            // SQLite 无独立 CREATE DATABASE；文件库在连接时创建
            return string.Empty;
        }
/// <summary>
/// Builds an ADD PRIMARY KEY statement.
/// </summary>
 

        public override string AddPrimaryKeyBy(string tableName, string columnName, string indexName)
        {
            // SQLite 不能在建表后可靠地 ALTER ADD PRIMARY KEY；
            // 自增主键已在 CreateTableColumnBy 中内联声明，此处返回空操作。
            return "SELECT 1";
        }
/// <summary>
/// Builds an ADD COLUMN statement.
/// </summary>
 

        public override string AddColumnToTableBy(string tableName, string columnName, string dataType, string defval, string nullable, string p2, string p3)
        {
            return string.Format("ALTER TABLE {0} ADD COLUMN {1} {2}{3} {4} {5} {6}",
                tableName, columnName, dataType,
                defval, nullable, p2, p3
                );
        }
/// <summary>
/// Builds an ALTER COLUMN statement.
/// </summary>
 

        public override string AlterColumnToTableby(string tableName, string columnName, string dataType, string defval, string nullable, string p2, string p3)
        {
            // 改类型/可空/默认值需重建表；不发伪 RENAME 以免误判成功
            return "SELECT 1";
        }
/// <summary>
/// Builds a CREATE TABLE statement.
/// </summary>
 

        public override string CreateTableBy(string tableName, string detail)
        {
            // 不保留 $PrimaryKey 占位符（核心未替换）；主键由列定义或后续空操作处理
            return string.Format("CREATE TABLE {0}(\r\n{1})", tableName, detail);
        }
/// <summary>
/// Builds a CREATE TABLE column definition.
/// </summary>
 

        public override string CreateTableColumnBy(string columnName, string dataType, string defval, string nullable, string p2, string p3)
        {
            // 自增列：AUTOINCREMENT 要求 INTEGER PRIMARY KEY
            if (!string.IsNullOrWhiteSpace(p3) &&
                p3.IndexOf("AUTOINCREMENT", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                dataType = "INTEGER";
                if (string.IsNullOrWhiteSpace(p2))
                    p2 = "PRIMARY KEY";
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
        {
            EnsureSqliteVersionAtLeast(3.35, "ALTER TABLE … DROP COLUMN");
            return string.Format("ALTER TABLE {0} DROP COLUMN {1}", tableName, columnName);
        }
/// <summary>
/// Builds a DROP CONSTRAINT statement.
/// </summary>
 

        public override string DropConstraintBy(string tableName, string constraintName)
        {
            // 无 ALTER DROP PRIMARY KEY / 通用 DROP CONSTRAINT；需重建表
            return "SELECT 1";
        }
/// <summary>
/// Builds a RENAME COLUMN statement.
/// </summary>
 

        public override string RenameColumnBy(string tableName, string oldName, string newName)
        {
            EnsureSqliteVersionAtLeast(3.25, "ALTER TABLE … RENAME COLUMN");
            return string.Format("ALTER TABLE {0} RENAME COLUMN {1} TO {2}", tableName, oldName, newName);
        }
/// <summary>
/// Builds SQL to check system-table permissions.
/// </summary>
 

        public override string CheckSystemTablePermissionsBy()
        {
            return "SELECT 1 FROM sqlite_master LIMIT 1";
        }
/// <summary>
/// Returns the NULL column qualifier for CREATE TABLE.
/// </summary>
 

        public override string CreateTableNullBy()
        {
            return "NULL";
        }
/// <summary>
/// Returns the NOT NULL column qualifier for CREATE TABLE.
/// </summary>
 

        public override string CreateTableNotNullBy()
        {
            return "NOT NULL";
        }
/// <summary>
/// Returns the PRIMARY KEY qualifier for CREATE TABLE.
/// </summary>
 

        public override string CreateTablePirmaryKeyBy()
        {
            return "PRIMARY KEY";
        }

        /// <summary>标准 SQLite 无表/列注释元数据。</summary>
        public override string AddTableCaptionBy(string tableName, string caption) => string.Empty;
/// <summary>
/// Builds SQL to update a table caption/comment.
/// </summary>
 

        public override string UpdateTableCaptionBy(string tableName, string caption) => string.Empty;
/// <summary>
/// Builds SQL to delete a table caption/comment.
/// </summary>
 

        public override string DeleteTableCaptionBy(string tableName) => string.Empty;
/// <summary>
/// Builds SQL to add a column caption/comment.
/// </summary>
 

        public override string AddColumnCaptionBy(string tableName, string columnName, string caption) => string.Empty;
/// <summary>
/// Builds SQL to update a column caption/comment.
/// </summary>
 

        public override string UpdateColumnCaptionBy(string tableName, string columnName, string caption) => string.Empty;
/// <summary>
/// Builds SQL to delete a column caption/comment.
/// </summary>
 

        public override string DeleteColumnCaptionBy(string tableName, string columnName) => string.Empty;
/// <summary>
/// Builds a RENAME TABLE statement.
/// </summary>
 

        public override string RenameTableBy(string oldTableName, string newTableName)
        {
            return string.Format("ALTER TABLE {0} RENAME TO {1}", oldTableName, newTableName);
        }
/// <summary>
/// Builds a CREATE INDEX statement.
/// </summary>
 

        public override string CreateIndexBy(string indexName, string tableName, string columnName, string unique)
        {
            return string.Format("CREATE {3} INDEX {2} ON {0} ({1})", tableName, columnName, indexName, unique);
        }
/// <summary>
/// Builds SQL that checks whether an index exists.
/// </summary>
 

        public override string IsAnyIndexBy(string indexName)
        {
            var safe = (indexName ?? string.Empty).Replace("'", "''");
            return string.Format(
                "SELECT COUNT(1) FROM sqlite_master WHERE type = 'index' AND name = '{0}'",
                safe);
        }
/// <summary>
/// Builds a DROP INDEX statement.
/// </summary>
 

        public override string buildDropIndex(string indexName, string tableName = null)
        {
            return string.Format("DROP INDEX IF EXISTS {0}", indexName);
        }

        /// <summary>
        /// 已知版本低于要求时抛错；版本未知（未探测）则放行，兼容默认较新的 Microsoft.Data.Sqlite。
        /// </summary>
        void EnsureSqliteVersionAtLeast(double minVersion, string feature)
        {
            var ver = dialect?.CurVersion;
            if (ver != null && ver.VersionNumber < minVersion)
            {
                throw new NotSupportedException(
                    string.Format("SQLite {0} 需要 {1}+，当前为 {2}", feature, minVersion, ver.VersionName ?? ver.VersionCode));
            }
        }

        #endregion
    }
}
