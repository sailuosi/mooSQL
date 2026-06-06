
using mooSQL.data.builder;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public class NpgsqlExpress:SQLExpression
    {
        public NpgsqlExpress(Dialect dia) : base(dia) {
            _paraPrefix = ":";
            _selectAutoIncrement = "";
            _provideType = "Npgsql.NpgsqlFactory,Npgsql";
        }

        public override string wrapKeyword(string value)
        {
            if(value.StartsWith("\"") && value.EndsWith("\""))
            {
                return value;
            }
            return "\""+value+"\"";
        }

        public override string dateDiffDay(string start, string end)
            => $"EXTRACT(EPOCH FROM ({end}::timestamp - {start}::timestamp)) / 86400";

        public override string charIndex(string substring, string str) => $"STRPOS({str}, {substring})";

        public override string charIndex(string substring, string str, string start)
            => $"(CASE WHEN {start} <= 0 THEN 0 ELSE STRPOS(SUBSTRING({str} FROM {start}::int), {substring}) + {start}::int - 1 END)";

        public override string isNullOrWhiteSpace(string expr)
            => $"({expr} IS NULL OR BTRIM({expr}, E' \\t\\n\\r\\f\\u0085\\u00a0') = '')";

        public override string dateDiffHour(string start, string end)
            => $"EXTRACT(EPOCH FROM ({end}::timestamp - {start}::timestamp)) / 3600";

        public override string dateDiffMinute(string start, string end)
            => $"EXTRACT(EPOCH FROM ({end}::timestamp - {start}::timestamp)) / 60";

        public override string dateDiffSecond(string start, string end)
            => $"EXTRACT(EPOCH FROM ({end}::timestamp - {start}::timestamp))";

        public override string dateDiffMillisecond(string start, string end)
            => $"ROUND(EXTRACT(EPOCH FROM ({end}::timestamp - {start}::timestamp)) * 1000)";

        public override string dateDiffYear(string start, string end)
            => $"(DATE_PART('year', {end}::date) - DATE_PART('year', {start}::date))";

        public override string dateDiffMonth(string start, string end)
            => $"((DATE_PART('year', {end}::date) - DATE_PART('year', {start}::date)) * 12 + (DATE_PART('month', {end}::date) - DATE_PART('month', {start}::date)))";

        public override string dateDiffWeek(string start, string end)
            => $"TRUNC(DATE_PART('day', {end}::timestamp - {start}::timestamp) / 7)";

        static string PgDatePart(string field, string date) => $"DATE_PART('{field}', {date}::timestamp)::int";

        public override string? datePartYear(string date) => PgDatePart("year", date);

        public override string? datePartQuarter(string date) => PgDatePart("quarter", date);

        public override string? datePartMonth(string date) => PgDatePart("month", date);

        public override string? datePartDay(string date) => PgDatePart("day", date);

        public override string? datePartDayOfYear(string date) => PgDatePart("doy", date);

        public override string? datePartWeek(string date) => PgDatePart("week", date);

        public override string? datePartWeekDay(string date) => $"({PgDatePart("dow", date)} + 1)";

        public override string? datePartHour(string date) => PgDatePart("hour", date);

        public override string? datePartMinute(string date) => PgDatePart("minute", date);

        public override string? datePartSecond(string date) => PgDatePart("second", date);

        public override string? datePartMillisecond(string date)
            => $"((EXTRACT(MILLISECONDS FROM {date}::timestamp))::int % 1000)";

        public override string collate(string exprPlaceholder, string collationLiteral)
        {
            var escaped = collationLiteral.Replace("\"", "\"\"");
            return $"{exprPlaceholder} COLLATE \"{escaped}\"";
        }

        static string PgDateAdd(string unit, string amount, string date)
            => $"({date}::timestamp + ({amount}::text || ' {unit}')::interval)";

        public override string? dateAddDay(string amount, string date) => PgDateAdd("day", amount, date);

        public override string? dateAddMonth(string amount, string date) => PgDateAdd("month", amount, date);

        public override string? dateAddYear(string amount, string date) => PgDateAdd("year", amount, date);

        public override string? dateAddHour(string amount, string date) => PgDateAdd("hour", amount, date);

        public override string? dateAddMinute(string amount, string date) => PgDateAdd("minute", amount, date);

        public override string? dateAddSecond(string amount, string date) => PgDateAdd("second", amount, date);

        public override string? dateAddWeek(string amount, string date) => PgDateAdd("week", amount, date);

        public override string? dateAddQuarter(string amount, string date)
            => $"({date}::timestamp + (({amount})::text || ' month')::interval * 3)";

        public override string? dateAddDayOfYear(string amount, string date) => dateAddDay(amount, date);

        public override string? dateAddWeekDay(string amount, string date) => dateAddDay(amount, date);

        public override string? dateAddMillisecond(string amount, string date) => PgDateAdd("millisecond", amount, date);

        #region DML语句
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

        public override string buildMergeInto(FragMergeInto frag)
        {
            return this.buildMergeIntoGeneral(frag);
        }

        #endregion

        #region DDL语句
        protected override string buildConstrainPK(string pkname, string fields)
        {
            return string.Format("CONSTRAINT {0} PRIMARY KEY ({1})", pkname, fields);
        }
        /// <summary>
        /// 整体注释的处理
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        protected override string buildDDLFieldsCaption(DDLFragSQL frag)
        {
            return buildDDLSoloCaptions(frag);
        }

        public override string buildSoloFieldCaption(DDLFragSQL frag, DDLField fie)
        {
            return string.Format("COMMENT ON COLUMN {0}.{1} IS '{2}';", frag.Table, fie.FieldName, fie.Caption);
        }
        public override string buildSoloTableCaption(DDLFragSQL frag)
        {
            return string.Format("COMMENT ON TABLE {0} IS '{1}';", frag.Table, frag.TableCaption);
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

        public override string buildCopyTableSchema(DDLFragSQL frag)
        {
            return string.Format("CREATE TABLE {0} AS TABLE {1} WITH NO DATA", frag.Table, frag.SrcTable);
        }
        public override string buildCopyTable(DDLFragSQL frag)
        {
            return string.Format("CREATE TABLE {0} AS TABLE {1}", frag.Table, frag.SrcTable);
        }
        public override string buildDropIndex(string indexName, string tableName = null)
        {
            return string.Format("DROP INDEX {0}", indexName);
        }

        public override string getTableAutoIdSQL()
        {
            return "serial";
        }
        public override string CreateDataBaseBy(string database)
        {
            return string.Format("CREATE DATABASE {0}", database);
        }
        public override string AddPrimaryKeyBy(string tableName, string columnName, string indexName) { 
            return string.Format("ALTER TABLE {0} ADD PRIMARY KEY({2}) /*{1}*/", tableName, indexName, columnName);
        }
        public override string AddColumnToTableBy(string tableName, string columnName, string dataType, string defval, string nullable, string p2, string p3)
        {
            return string.Format("ALTER TABLE {0} ADD COLUMN {1} {2}{3} {4} {5} {6}",
                tableName, columnName, dataType,
                defval, nullable, p2, p3
                );
        }
        public override string AlterColumnToTableby(string tableName, string columnName, string dataType, string defval, string nullable, string p2, string p3)
        { 
            return string.Format("ALTER TABLE {0} ALTER COLUMN {1} {2}{3} {4} {5} {6}",
                tableName, columnName, dataType,
                defval, nullable, p2, p3
                );
        }
        public override string CreateTableBy(string tableName, string detail)
        { 
            return string.Format("CREATE TABLE {0}(\r\n{1} $PrimaryKey)", tableName, detail);
        }
        public override string CreateTableColumnBy(string columnName, string dataType, string defval, string nullable, string p2, string p3)
        { 
            return string.Format("{0} {1}{2} {3} {4} {5}",
                columnName, dataType,
                defval, nullable, p2, p3
                );
        }
        //protected override string TruncateTableSql(){ "TRUNCATE TABLE {0}";

        //protected override string DropTableSql(){ "DROP TABLE {0}";

        public override string DropColumnToTableBy(string tableName, string columnName)
        { 
            return string.Format("ALTER TABLE {0} DROP COLUMN {1}", tableName, columnName);
        }
        public override string DropConstraintBy(string tableName, string constraintName)
        { 
            return string.Format("ALTER TABLE {0} DROP CONSTRAINT {1}", tableName, constraintName);
        }
        public override string RenameColumnBy(string tableName, string oldName, string newName)
        { 
            return string.Format("ALTER TABLE {0} RENAME {1} TO {2}", tableName, oldName, newName);
        }
        public override string AddColumnCaptionBy(string tableName, string columnName, string caption)
        { 
            return string.Format("COMMENT ON COLUMN {1}.{0} IS '{2}'", columnName, tableName, caption
                );
        }
        public override string UpdateColumnCaptionBy(string tableName, string columnName, string caption)
        {
            return AddColumnCaptionBy(tableName, columnName, caption);
        }
        public override string DeleteColumnCaptionBy(string tableName, string columnName)
        { 
            return string.Format("COMMENT ON COLUMN {1}.{0} IS ''",
                columnName, tableName
                );
        }


        public override string AddTableCaptionBy(string tableName, string caption)
        { 
            return string.Format("COMMENT ON TABLE {0} IS '{1}'", tableName, caption);
        }
        public override string UpdateTableCaptionBy(string tableName, string caption)
        {
            return AddTableCaptionBy(tableName, caption);
        }
        public override string DeleteTableCaptionBy(string tableName)
        { 
            return string.Format("COMMENT ON TABLE {0} IS ''", tableName);
        }


        public override string RenameTableBy(string oldTableName, string newTableName)
        { 
            return string.Format("ALTER TABLE 表名 {0} to {1}", oldTableName, newTableName);
        }
        public override string CreateIndexBy(string indexName, string tableName, string columnName, string unique)
        { 
            return string.Format("CREATE {3} INDEX Index_{0}_{2} ON {0} ({1})", tableName, columnName, indexName, unique);
        }
        public override string IsAnyIndexBy(string indexName)
        { 
            return string.Format(" SELECT COUNT(1) FROM (SELECT to_regclass('{0}') AS c ) t WHERE t.c IS NOT NULL", indexName);
        }
        public override string CheckSystemTablePermissionsBy(){ 
            return "SELECT 1 FROM information_schema.columns LIMIT 1 OFFSET 0";
        }
        public override string CreateTableNullBy(){
            return "NULL";
        }
        public override string CreateTableNotNullBy(){
            return "NOT NULL";
        }
        public override string CreateTablePirmaryKeyBy(){
            return "PRIMARY KEY";
        }
        #endregion

        #region 字段类型
        public override string getDateTimeColumnType(int length)
        {
            return "TIMESTAMP";
        }
        public override string getBoolColumnType()
        {
            return "BOOLEAN";
        }
        public override string getGuidColumnType()
        {
            return "UUID";
        }
        #endregion
    }
}
