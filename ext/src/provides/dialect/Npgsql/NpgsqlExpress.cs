
using mooSQL.data.builder;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// NpgsqlExpress type.
    /// </summary>
    /// <summary>
    /// NpgsqlExpress type.
    /// </summary>
    public class NpgsqlExpress:SQLExpression
    {
        /// <summary>
        /// NpgsqlExpress type.
        /// </summary>
        /// <summary>
        /// NpgsqlExpress type.
        /// </summary>
        public NpgsqlExpress(Dialect dia) : base(dia) {
            _paraPrefix = ":";
            _selectAutoIncrement = "";
            _provideType = "Npgsql.NpgsqlFactory,Npgsql";
        }

        /// <inheritdoc/>
        public override NameCaseMode DefaultNameCase => NameCaseMode.ToLower;
        /// <inheritdoc/>
        public override NameQuoteMode DefaultNameQuote => NameQuoteMode.IfNeeded;
        /// <inheritdoc/>
        public override bool FoldsUnquotedNames => true;

        /// <inheritdoc/>
        public override ResultColumnNameMode DefaultResultColumnName => ResultColumnNameMode.Pre;

        /// <inheritdoc/>
        public override bool StrictParameterTypes => true;

        /// <inheritdoc/>
        protected override System.Collections.Generic.HashSet<string> GetReservedWords()
            => SqlReservedWords.PostgreSQL;
/// <summary>
/// Quotes an identifier for this dialect.
/// </summary>
 

        public override string wrapKeyword(string value)
        {
            if(value.StartsWith("\"") && value.EndsWith("\""))
            {
                return value;
            }
            return "\""+value+"\"";
        }

        /// <inheritdoc/>
        protected override string buildSetAssignment(FragSetPart item)
        {
            if (string.IsNullOrEmpty(item.jsonPath))
                return base.buildSetAssignment(item);
            var pgPath = JsonPathToPgTextArray(item.jsonPath);
            var pathLit = "'" + pgPath.Replace("'", "''") + "'";
            return item.field + "=jsonb_set(" + item.field + "::jsonb, " + pathLit + "::text[], to_jsonb(" + item.value + "), true)";
        }

        /// <summary>
        /// <c>$.a.b</c> / <c>$.basket[0].qty</c> → <c>{a,b}</c> / <c>{basket,0,qty}</c>。
        /// </summary>
        internal static string JsonPathToPgTextArray(string jsonPath)
        {
            if (string.IsNullOrWhiteSpace(jsonPath))
                throw new ArgumentException("JSON path is required.", nameof(jsonPath));
            var p = jsonPath.Trim();
            if (p.StartsWith("$."))
                p = p.Substring(2);
            else if (p.StartsWith("$"))
                p = p.Substring(1).TrimStart('.');
            else
                throw new NotSupportedException("PostgreSQL jsonb_set path must be a JSON Path starting with $.");

            var parts = new List<string>();
            var sb = new StringBuilder();
            for (int i = 0; i < p.Length; i++)
            {
                char c = p[i];
                if (c == '.')
                {
                    if (sb.Length > 0) { parts.Add(sb.ToString()); sb.Clear(); }
                }
                else if (c == '[')
                {
                    if (sb.Length > 0) { parts.Add(sb.ToString()); sb.Clear(); }
                    int end = p.IndexOf(']', i);
                    if (end < 0)
                        throw new NotSupportedException("Invalid JSON Path array index: " + jsonPath);
                    parts.Add(p.Substring(i + 1, end - i - 1));
                    i = end;
                }
                else
                {
                    sb.Append(c);
                }
            }
            if (sb.Length > 0)
                parts.Add(sb.ToString());
            if (parts.Count == 0)
                throw new NotSupportedException("Empty JSON Path after $: " + jsonPath);
            return "{" + string.Join(",", parts) + "}";
        }
/// <summary>
/// Builds a date-difference expression in Day.
/// </summary>
 

        public override string dateDiffDay(string start, string end)
            => $"EXTRACT(EPOCH FROM ({end}::timestamp - {start}::timestamp)) / 86400";
/// <summary>
/// Builds a character-index expression.
/// </summary>
 

        public override string charIndex(string substring, string str) => $"STRPOS({str}, {substring})";
/// <summary>
/// Builds a character-index expression.
/// </summary>
 

        public override string charIndex(string substring, string str, string start)
            => $"(CASE WHEN {start} <= 0 THEN 0 ELSE STRPOS(SUBSTRING({str} FROM {start}::int), {substring}) + {start}::int - 1 END)";
/// <summary>
/// Builds an is-null-or-whitespace expression.
/// </summary>
 

        public override string isNullOrWhiteSpace(string expr)
            => $"({expr} IS NULL OR BTRIM({expr}, E' \\t\\n\\r\\f\\u0085\\u00a0') = '')";
/// <summary>
/// Builds a date-difference expression in Hour.
/// </summary>
 

        public override string dateDiffHour(string start, string end)
            => $"EXTRACT(EPOCH FROM ({end}::timestamp - {start}::timestamp)) / 3600";
/// <summary>
/// Builds a date-difference expression in Minute.
/// </summary>
 

        public override string dateDiffMinute(string start, string end)
            => $"EXTRACT(EPOCH FROM ({end}::timestamp - {start}::timestamp)) / 60";
/// <summary>
/// Builds a date-difference expression in Second.
/// </summary>
 

        public override string dateDiffSecond(string start, string end)
            => $"EXTRACT(EPOCH FROM ({end}::timestamp - {start}::timestamp))";
/// <summary>
/// Builds a date-difference expression in Millisecond.
/// </summary>
 

        public override string dateDiffMillisecond(string start, string end)
            => $"ROUND(EXTRACT(EPOCH FROM ({end}::timestamp - {start}::timestamp)) * 1000)";
/// <summary>
/// Builds a date-difference expression in Year.
/// </summary>
 

        public override string dateDiffYear(string start, string end)
            => $"(DATE_PART('year', {end}::date) - DATE_PART('year', {start}::date))";
/// <summary>
/// Builds a date-difference expression in Month.
/// </summary>
 

        public override string dateDiffMonth(string start, string end)
            => $"((DATE_PART('year', {end}::date) - DATE_PART('year', {start}::date)) * 12 + (DATE_PART('month', {end}::date) - DATE_PART('month', {start}::date)))";
/// <summary>
/// Builds a date-difference expression in Week.
/// </summary>
 

        public override string dateDiffWeek(string start, string end)
            => $"TRUNC(DATE_PART('day', {end}::timestamp - {start}::timestamp) / 7)";

        static string PgDatePart(string field, string date) => $"DATE_PART('{field}', {date}::timestamp)::int";
/// <summary>
/// Builds a date-part expression for Year.
/// </summary>
 

        public override string? datePartYear(string date) => PgDatePart("year", date);
/// <summary>
/// Builds a date-part expression for Quarter.
/// </summary>
 

        public override string? datePartQuarter(string date) => PgDatePart("quarter", date);
/// <summary>
/// Builds a date-part expression for Month.
/// </summary>
 

        public override string? datePartMonth(string date) => PgDatePart("month", date);
/// <summary>
/// Builds a date-part expression for Day.
/// </summary>
 

        public override string? datePartDay(string date) => PgDatePart("day", date);
/// <summary>
/// Builds a date-part expression for DayOfYear.
/// </summary>
 

        public override string? datePartDayOfYear(string date) => PgDatePart("doy", date);
/// <summary>
/// Builds a date-part expression for Week.
/// </summary>
 

        public override string? datePartWeek(string date) => PgDatePart("week", date);
/// <summary>
/// Builds a date-part expression for WeekDay.
/// </summary>
 

        public override string? datePartWeekDay(string date) => $"({PgDatePart("dow", date)} + 1)";
/// <summary>
/// Builds a date-part expression for Hour.
/// </summary>
 

        public override string? datePartHour(string date) => PgDatePart("hour", date);
/// <summary>
/// Builds a date-part expression for Minute.
/// </summary>
 

        public override string? datePartMinute(string date) => PgDatePart("minute", date);
/// <summary>
/// Builds a date-part expression for Second.
/// </summary>
 

        public override string? datePartSecond(string date) => PgDatePart("second", date);
/// <summary>
/// Builds a date-part expression for Millisecond.
/// </summary>
 

        public override string? datePartMillisecond(string date)
            => $"((EXTRACT(MILLISECONDS FROM {date}::timestamp))::int % 1000)";
/// <summary>
/// Builds a COLLATE expression.
/// </summary>
 

        public override string collate(string exprPlaceholder, string collationLiteral)
        {
            var escaped = collationLiteral.Replace("\"", "\"\"");
            return $"{exprPlaceholder} COLLATE \"{escaped}\"";
        }

        static string PgDateAdd(string unit, string amount, string date)
            => $"({date}::timestamp + ({amount}::text || ' {unit}')::interval)";
/// <summary>
/// Builds a date-add expression for Day.
/// </summary>
 

        public override string? dateAddDay(string amount, string date) => PgDateAdd("day", amount, date);
/// <summary>
/// Builds a date-add expression for Month.
/// </summary>
 

        public override string? dateAddMonth(string amount, string date) => PgDateAdd("month", amount, date);
/// <summary>
/// Builds a date-add expression for Year.
/// </summary>
 

        public override string? dateAddYear(string amount, string date) => PgDateAdd("year", amount, date);
/// <summary>
/// Builds a date-add expression for Hour.
/// </summary>
 

        public override string? dateAddHour(string amount, string date) => PgDateAdd("hour", amount, date);
/// <summary>
/// Builds a date-add expression for Minute.
/// </summary>
 

        public override string? dateAddMinute(string amount, string date) => PgDateAdd("minute", amount, date);
/// <summary>
/// Builds a date-add expression for Second.
/// </summary>
 

        public override string? dateAddSecond(string amount, string date) => PgDateAdd("second", amount, date);
/// <summary>
/// Builds a date-add expression for Week.
/// </summary>
 

        public override string? dateAddWeek(string amount, string date) => PgDateAdd("week", amount, date);
/// <summary>
/// Builds a date-add expression for Quarter.
/// </summary>
 

        public override string? dateAddQuarter(string amount, string date)
            => $"({date}::timestamp + (({amount})::text || ' month')::interval * 3)";
/// <summary>
/// Builds a date-add expression for DayOfYear.
/// </summary>
 

        public override string? dateAddDayOfYear(string amount, string date) => dateAddDay(amount, date);
/// <summary>
/// Builds a date-add expression for WeekDay.
/// </summary>
 

        public override string? dateAddWeekDay(string amount, string date) => dateAddDay(amount, date);
/// <summary>
/// Builds a date-add expression for Millisecond.
/// </summary>
 

        public override string? dateAddMillisecond(string amount, string date) => PgDateAdd("millisecond", amount, date);

        #region DML语句
 /// <summary>
 /// Builds a SELECT statement.
 /// </summary>
 
        public override string buildSelect(FragSQL frag)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT ");
            if (frag.distincted)
            {
                sb.Append("DISTINCT ");
            }
            sb.Append(frag.selectInner);
            //如果使用了行号函数
            if (frag.hasRowNumber)
            {
                var t = buildRowNumber(frag);
                if (!string.IsNullOrWhiteSpace(t))
                {
                    if (!string.IsNullOrWhiteSpace(frag.selectInner))
                    {
                        sb.Append(",");
                    }
                    sb.Append(t);
                }

            }
            this.buildSelectFromToOrderPart(frag, sb);
            AppendLimitOffset(sb, frag);

            return sb.ToString();
        }
/// <summary>
/// Builds a paged SELECT statement.
/// </summary>
 

        public override string buildPagedSelect(FragSQL frag)
            => HasSkipTakePaging(frag) ? buildSelect(frag) : base.buildPagedSelect(frag);
        /// <summary>
        /// 生成插入的sql
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
                //多行插入（PostgreSQL: VALUES (row1), (row2), ...）
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
/// Builds a MERGE INTO statement.
/// </summary>
 

        public override string buildMergeInto(FragMergeInto frag)
        {
            return this.buildMergeIntoGeneral(frag);
        }

        /// <summary>
        /// PostgreSQL：UPDATE T SET … FROM (SELECT … UNION ALL …) S WHERE T.key = S.key
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

            var t = string.IsNullOrWhiteSpace(frag.targetAlias) ? "T" : frag.targetAlias;
            var s = string.IsNullOrWhiteSpace(frag.sourceAlias) ? "S" : frag.sourceAlias;
            var sb = new StringBuilder();
            sb.Append("UPDATE ");
            sb.Append(normalizeName(frag.tableName));
            sb.Append(" AS ");
            sb.Append(t);
            sb.Append(" SET ");
            for (var i = 0; i < frag.setColumns.Count; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append(t);
                sb.Append('.');
                sb.Append(normalizeName(frag.setColumns[i]));
                sb.Append(" = ");
                sb.Append(s);
                sb.Append('.');
                sb.Append(normalizeName(frag.sourceAliases[i]));
            }
            sb.Append(" FROM (");
            for (var i = 0; i < frag.selectLists.Count; i++)
            {
                if (i > 0)
                    sb.Append(" UNION ALL ");
                sb.Append("SELECT ");
                sb.Append(frag.selectLists[i]);
            }
            sb.Append(") AS ");
            sb.Append(s);
            sb.Append(" WHERE ");
            for (var k = 0; k < frag.keyColumns.Count; k++)
            {
                if (k > 0)
                    sb.Append(" AND ");
                var aliasIdx = frag.setColumns.Count + k;
                sb.Append(t);
                sb.Append('.');
                sb.Append(normalizeName(frag.keyColumns[k]));
                sb.Append(" = ");
                sb.Append(s);
                sb.Append('.');
                sb.Append(normalizeName(frag.sourceAliases[aliasIdx]));
            }
            return sb.ToString();
        }

        #endregion

        #region DDL语句
 /// <summary>
 /// Builds a primary-key constraint fragment.
 /// </summary>
 
        protected override string buildConstrainPK(string pkname, string fields)
        {
            return string.Format("CONSTRAINT {0} PRIMARY KEY ({1})", pkname, fields);
        }
        /// <summary>
        /// COMMENT ON 已由 buildCreateTable → buildDDLSoloCaptions 输出，此处勿再重复。
        /// </summary>
        protected override string buildDDLFieldsCaption(DDLFragSQL frag)
        {
            return string.Empty;
        }

        /// <inheritdoc/>
        protected override bool SoloCaptionsAsSeparateStatements => true;
/// <summary>
/// Builds a single field caption DDL fragment.
/// </summary>
 

        public override string buildSoloFieldCaption(DDLFragSQL frag, DDLField fie)
        {
            return string.Format("COMMENT ON COLUMN {0}.{1} IS '{2}';",
                frag.Table, fie.FieldName, fie.Caption);
        }
 /// <summary>
 /// Builds a single table caption DDL fragment.
 /// </summary>
 
        public override string buildSoloTableCaption(DDLFragSQL frag)
        {
            return string.Format("COMMENT ON TABLE {0} IS '{1}';",
                frag.Table, frag.TableCaption);
        }

        /// <summary>
        /// 修改视图
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        public override string buildAlterView(DDLFragSQL frag)
        {
            var sb = new StringBuilder();
            sb.Append("CREATE OR REPLACE VIEW ")
                .Append(frag.Table)
                .Append(" AS ");
            sb.Append(frag.SelectSQL);
            return sb.ToString();
        }
/// <summary>
/// Builds a copy-table-schema statement.
/// </summary>
 

        public override string buildCopyTableSchema(DDLFragSQL frag)
        {
            return string.Format("CREATE TABLE {0} AS TABLE {1} WITH NO DATA", frag.Table, frag.SrcTable);
        }
 /// <summary>
 /// Builds a copy-table statement.
 /// </summary>
 
        public override string buildCopyTable(DDLFragSQL frag)
        {
            return string.Format("CREATE TABLE {0} AS TABLE {1}", frag.Table, frag.SrcTable);
        }
 /// <summary>
 /// Builds a DROP INDEX statement.
 /// </summary>
 
        public override string buildDropIndex(string indexName, string tableName = null)
        {
            return string.Format("DROP INDEX {0}", indexName);
        }
/// <summary>
/// Returns SQL for retrieving an auto-generated identity value.
/// </summary>
 

        public override string getTableAutoIdSQL()
        {
            return "serial";
        }
 /// <summary>
 /// Builds a CREATE DATABASE statement.
 /// </summary>
 
        public override string CreateDataBaseBy(string database)
        {
            return string.Format("CREATE DATABASE {0}", database);
        }
 /// <summary>
 /// Builds an ADD PRIMARY KEY statement.
 /// </summary>
 
        public override string AddPrimaryKeyBy(string tableName, string columnName, string indexName) { 
            return string.Format("ALTER TABLE {0} ADD PRIMARY KEY({2}) /*{1}*/", tableName, indexName, columnName);
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
            // PG: ALTER COLUMN ... TYPE ...；可空性用 SET/DROP NOT NULL，不能写成 MSSQL 风格
            var sb = new StringBuilder();
            sb.AppendFormat("ALTER TABLE {0} ALTER COLUMN {1} TYPE {2}", tableName, columnName, dataType);
            var nullSql = BuildPgAlterNullability(tableName, columnName, nullable);
            if (!string.IsNullOrEmpty(nullSql))
                sb.Append(';').Append(nullSql);
            return sb.ToString();
        }

        /// <summary>
        /// PG 列修改：TYPE + 可空性分句（供 doAlterColumn / WidenColumns）。
        /// </summary>
        public override string buildAlterColumn(DDLFragSQL frag, DDLField field)
        {
            if (frag == null || field == null || string.IsNullOrWhiteSpace(field.FieldName))
                return string.Empty;
            var sb = new StringBuilder();
            sb.Append("ALTER TABLE ")
                .Append(frag.Table)
                .Append(" ALTER COLUMN ")
                .Append(field.FieldName)
                .Append(" TYPE ")
                .Append(field.TextType);
            var nullClause = field.Nullable
                ? "DROP NOT NULL"
                : "SET NOT NULL";
            sb.Append(';')
                .Append("ALTER TABLE ")
                .Append(frag.Table)
                .Append(" ALTER COLUMN ")
                .Append(field.FieldName)
                .Append(' ')
                .Append(nullClause);
            return sb.ToString();
        }

        static string BuildPgAlterNullability(string tableName, string columnName, string nullable)
        {
            if (string.IsNullOrWhiteSpace(nullable))
                return null;
            var n = nullable.Trim();
            if (n.Equals("NOT NULL", StringComparison.OrdinalIgnoreCase)
                || n.IndexOf("NOT NULL", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return string.Format("ALTER TABLE {0} ALTER COLUMN {1} SET NOT NULL", tableName, columnName);
            }
            if (n.Equals("NULL", StringComparison.OrdinalIgnoreCase))
            {
                return string.Format("ALTER TABLE {0} ALTER COLUMN {1} DROP NOT NULL", tableName, columnName);
            }
            return null;
        }
 /// <summary>
 /// Builds a CREATE TABLE statement.
 /// </summary>
 
        public override string CreateTableBy(string tableName, string detail)
        { 
            return string.Format("CREATE TABLE {0}(\r\n{1} $PrimaryKey)", tableName, detail);
        }
 /// <summary>
 /// Builds a CREATE TABLE column definition.
 /// </summary>
 
        public override string CreateTableColumnBy(string columnName, string dataType, string defval, string nullable, string p2, string p3)
        { 
            return string.Format("{0} {1}{2} {3} {4} {5}",
                columnName, dataType,
                defval, nullable, p2, p3
                );
        }
        //protected override string TruncateTableSql(){ "TRUNCATE TABLE {0}";

        //protected override string DropTableSql(){ "DROP TABLE {0}";
/// <summary>
/// Builds a DROP COLUMN statement.
/// </summary>
 

        public override string DropColumnToTableBy(string tableName, string columnName)
        { 
            return string.Format("ALTER TABLE {0} DROP COLUMN {1}", tableName, columnName);
        }
 /// <summary>
 /// Builds a DROP CONSTRAINT statement.
 /// </summary>
 
        public override string DropConstraintBy(string tableName, string constraintName)
        { 
            return string.Format("ALTER TABLE {0} DROP CONSTRAINT {1}", tableName, constraintName);
        }
 /// <summary>
 /// Builds a RENAME COLUMN statement.
 /// </summary>
 
        public override string RenameColumnBy(string tableName, string oldName, string newName)
        { 
            return string.Format("ALTER TABLE {0} RENAME {1} TO {2}", tableName, oldName, newName);
        }
 /// <summary>
 /// Builds SQL to add a column caption/comment.
 /// </summary>
 
        public override string AddColumnCaptionBy(string tableName, string columnName, string caption)
        { 
            return string.Format("COMMENT ON COLUMN {1}.{0} IS '{2}'",
                wrapField(columnName), wrapTable(tableName), caption);
        }
 /// <summary>
 /// Builds SQL to update a column caption/comment.
 /// </summary>
 
        public override string UpdateColumnCaptionBy(string tableName, string columnName, string caption)
        {
            return AddColumnCaptionBy(tableName, columnName, caption);
        }
 /// <summary>
 /// Builds SQL to delete a column caption/comment.
 /// </summary>
 
        public override string DeleteColumnCaptionBy(string tableName, string columnName)
        { 
            return string.Format("COMMENT ON COLUMN {1}.{0} IS ''",
                wrapField(columnName), wrapTable(tableName));
        }
/// <summary>
/// Builds SQL to add a table caption/comment.
/// </summary>
 


        public override string AddTableCaptionBy(string tableName, string caption)
        { 
            return string.Format("COMMENT ON TABLE {0} IS '{1}'", wrapTable(tableName), caption);
        }
 /// <summary>
 /// Builds SQL to update a table caption/comment.
 /// </summary>
 
        public override string UpdateTableCaptionBy(string tableName, string caption)
        {
            return AddTableCaptionBy(tableName, caption);
        }
 /// <summary>
 /// Builds SQL to delete a table caption/comment.
 /// </summary>
 
        public override string DeleteTableCaptionBy(string tableName)
        { 
            return string.Format("COMMENT ON TABLE {0} IS ''", wrapTable(tableName));
        }
/// <summary>
/// Builds a RENAME TABLE statement.
/// </summary>
 


        public override string RenameTableBy(string oldTableName, string newTableName)
        { 
            return string.Format("ALTER TABLE 表名 {0} to {1}", oldTableName, newTableName);
        }
 /// <summary>
 /// Builds a CREATE INDEX statement.
 /// </summary>
 
        public override string CreateIndexBy(string indexName, string tableName, string columnName, string unique)
        { 
            return string.Format("CREATE {3} INDEX Index_{0}_{2} ON {0} ({1})", tableName, columnName, indexName, unique);
        }
 /// <summary>
 /// Builds SQL that checks whether an index exists.
 /// </summary>
 
        public override string IsAnyIndexBy(string indexName)
        { 
            return string.Format(" SELECT COUNT(1) FROM (SELECT to_regclass('{0}') AS c ) t WHERE t.c IS NOT NULL", indexName);
        }
 /// <summary>
 /// Builds SQL to check system-table permissions.
 /// </summary>
 
        public override string CheckSystemTablePermissionsBy(){ 
            return "SELECT 1 FROM information_schema.columns LIMIT 1 OFFSET 0";
        }
 /// <summary>
 /// Returns the NULL column qualifier for CREATE TABLE.
 /// </summary>
 
        public override string CreateTableNullBy(){
            return "NULL";
        }
 /// <summary>
 /// Returns the NOT NULL column qualifier for CREATE TABLE.
 /// </summary>
 
        public override string CreateTableNotNullBy(){
            return "NOT NULL";
        }
 /// <summary>
 /// Returns the PRIMARY KEY qualifier for CREATE TABLE.
 /// </summary>
 
        public override string CreateTablePirmaryKeyBy(){
            return "PRIMARY KEY";
        }
        #endregion

        #region 字段类型
 /// <summary>
 /// Returns the dialect-specific DateTime column type.
 /// </summary>
 
        public override string getDateTimeColumnType(int length)
        {
            return "TIMESTAMP";
        }
 /// <summary>
 /// Returns the dialect-specific Boolean column type.
 /// </summary>
 
        public override string getBoolColumnType()
        {
            return "BOOLEAN";
        }
 /// <summary>
 /// Returns the dialect-specific GUID column type.
 /// </summary>
 
        public override string getGuidColumnType()
        {
            return "UUID";
        }
        #endregion
    }
}
