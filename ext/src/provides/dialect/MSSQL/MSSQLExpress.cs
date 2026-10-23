
using mooSQL.data.builder;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// MSSQLExpress type.
    /// </summary>
    /// <summary>
    /// MSSQLExpress type.
    /// </summary>
    public class MSSQLExpress:SQLExpression
    {
        /// <summary>
        /// MSSQLExpress type.
        /// </summary>
        /// <summary>
        /// MSSQLExpress type.
        /// </summary>
        public MSSQLExpress(Dialect dia) : base(dia)
        {
            _paraPrefix = "@";
            _selectAutoIncrement = "SELECT Scope_Identity()";
            _provideType = "System.Data.SqlClient.SqlClientFactory,System.Data.SqlClient";
        }

        /// <inheritdoc/>
        protected override System.Collections.Generic.HashSet<string> GetReservedWords()
            => SqlReservedWords.SqlServer;
/// <summary>
/// Quotes an identifier for this dialect.
/// </summary>
 

        public override string wrapKeyword(string value)
        {
            if (value.StartsWith("[") && value.EndsWith("]")) { 
                return value;
            }
            return "[" + value + "]";
        }

        /// <inheritdoc/>
        protected override string buildSetAssignment(FragSetPart item)
        {
            if (string.IsNullOrEmpty(item.jsonPath))
                return base.buildSetAssignment(item);
            var pathLit = "'" + item.jsonPath.Replace("'", "''") + "'";
            return item.field + "=JSON_MODIFY(" + item.field + ", " + pathLit + ", " + item.value + ")";
        }
/// <summary>
/// Builds a date-difference expression in Day.
/// </summary>
 

        public override string dateDiffDay(string start, string end)
            => $"DATEDIFF(day, {start}, {end})";
/// <summary>
/// Builds an is-null-or-whitespace expression.
/// </summary>
 

        public override string isNullOrWhiteSpace(string expr)
            => $"({expr} IS NULL OR TRIM({expr}) = N'')";
/// <summary>
/// Builds a date-difference expression in Hour.
/// </summary>
 

        public override string dateDiffHour(string start, string end)
            => $"DATEDIFF(hour, {start}, {end})";
/// <summary>
/// Builds a date-difference expression in Minute.
/// </summary>
 

        public override string dateDiffMinute(string start, string end)
            => $"DATEDIFF(minute, {start}, {end})";
/// <summary>
/// Builds a date-difference expression in Second.
/// </summary>
 

        public override string dateDiffSecond(string start, string end)
            => $"DATEDIFF(second, {start}, {end})";
/// <summary>
/// Builds a date-difference expression in Millisecond.
/// </summary>
 

        public override string dateDiffMillisecond(string start, string end)
            => $"DATEDIFF(millisecond, {start}, {end})";
/// <summary>
/// Builds a date-difference expression in Year.
/// </summary>
 

        public override string dateDiffYear(string start, string end)
            => $"DATEDIFF(year, {start}, {end})";
/// <summary>
/// Builds a date-difference expression in Month.
/// </summary>
 

        public override string dateDiffMonth(string start, string end)
            => $"DATEDIFF(month, {start}, {end})";
/// <summary>
/// Builds a date-difference expression in Week.
/// </summary>
 

        public override string dateDiffWeek(string start, string end)
            => $"DATEDIFF(week, {start}, {end})";
/// <summary>
/// Builds a date-difference expression in Quarter.
/// </summary>
 

        public override string dateDiffQuarter(string start, string end)
            => $"DATEDIFF(quarter, {start}, {end})";

        static string SqlDatePart(string part, string date) => $"DATEPART({part}, {date})";
/// <summary>
/// Builds a date-part expression for Year.
/// </summary>
 

        public override string? datePartYear(string date) => SqlDatePart("year", date);
/// <summary>
/// Builds a date-part expression for Quarter.
/// </summary>
 

        public override string? datePartQuarter(string date) => SqlDatePart("quarter", date);
/// <summary>
/// Builds a date-part expression for Month.
/// </summary>
 

        public override string? datePartMonth(string date) => SqlDatePart("month", date);
/// <summary>
/// Builds a date-part expression for Day.
/// </summary>
 

        public override string? datePartDay(string date) => SqlDatePart("day", date);
/// <summary>
/// Builds a date-part expression for DayOfYear.
/// </summary>
 

        public override string? datePartDayOfYear(string date) => SqlDatePart("dayofyear", date);
/// <summary>
/// Builds a date-part expression for Week.
/// </summary>
 

        public override string? datePartWeek(string date) => SqlDatePart("week", date);
/// <summary>
/// Builds a date-part expression for WeekDay.
/// </summary>
 

        public override string? datePartWeekDay(string date) => SqlDatePart("weekday", date);
/// <summary>
/// Builds a date-part expression for Hour.
/// </summary>
 

        public override string? datePartHour(string date) => SqlDatePart("hour", date);
/// <summary>
/// Builds a date-part expression for Minute.
/// </summary>
 

        public override string? datePartMinute(string date) => SqlDatePart("minute", date);
/// <summary>
/// Builds a date-part expression for Second.
/// </summary>
 

        public override string? datePartSecond(string date) => SqlDatePart("second", date);
/// <summary>
/// Builds a date-part expression for Millisecond.
/// </summary>
 

        public override string? datePartMillisecond(string date) => SqlDatePart("millisecond", date);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int? getWhereInLimit()
        {
            return 2500;
        }

        #region DML 修改数据语句
        private string dealValsPivot(List<string> strings) { 
            var res= new StringBuilder();
            foreach (string s in strings)
            {

                if (s.Length > 0) { 
                    res.Append(s);
                }
                if (RegxUntils.IsNumeric(s)|| RegxUntils.IsInt(s)) { 
                    res.Append(s);
                }
                else
                {
                    res.Append("'"+s+"'");
                }
            }
            return res.ToString();
        }
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
            if (frag.toped > -1)
            {
                sb.Append("TOP ");
                sb.Append(frag.toped);
                sb.Append(" ");
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


            sb.Append(" ");
            sb.Append("FROM ");
            sb.Append(frag.fromInner);

            if (frag.pivots != null) {
                foreach (var pivot in frag.pivots) { 
                    sb.AppendFormat(" PIVOT ({0} FOR {1} IN ({2})) {3} "
                        , pivot.aggregation
                        , pivot.headField
                        , dealValsPivot(pivot.headValues)
                        ,pivot.asName);                
                }

            }
            if (frag.unpivots != null)
            {
                foreach (var unpivot in frag.unpivots) {
                    sb.AppendFormat(" UNPIVOT ({0} FOR {1} IN ({2})) {3} "
                        , unpivot.valueName
                        , unpivot.fieldName
                        , string.Join(",",unpivot.fields)
                        , unpivot.asName);                
                }

            }

            sb.Append(" ");
            if (!string.IsNullOrWhiteSpace(frag.whereInner))
            {
                sb.Append("WHERE ");
                sb.Append(frag.whereInner);
                sb.Append(" ");
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

            if (!string.IsNullOrWhiteSpace(frag.orderbyInner))
            {
                sb.Append("ORDER BY ");
                sb.Append(frag.orderbyInner);
                sb.Append(" ");
            }



            return sb.ToString();
        }

        /// <inheritdoc/>
        protected override string WrapExistScalar(string existsSubquery)
            => $"SELECT CASE WHEN EXISTS ({existsSubquery}) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END";

        /// <inheritdoc/>
        protected override string BuildExistInnerSelect(FragSQL frag)
        {
            string inner;
            if (frag.distincted)
            {
                var sb = new StringBuilder();
                sb.Append("SELECT DISTINCT ");
                sb.Append(frag.selectInner);
                sb.Append(" FROM ");
                sb.Append(frag.fromInner);
                AppendExistWhereGroupHaving(frag, sb);
                inner = "SELECT TOP 1 1 FROM (" + sb + ") existwrap";
            }
            else
            {
                var sb = new StringBuilder();
                sb.Append("SELECT TOP 1 1 FROM ");
                sb.Append(frag.fromInner);
                AppendExistWhereGroupHaving(frag, sb);
                inner = sb.ToString();
            }
            return inner;
        }

        /*
         * 
         */
 /// <summary>
 /// Builds a paged SELECT statement.
 /// </summary>
 
        public override string buildPagedSelect(FragSQL frag)
        {
            if (!HasSkipTakePaging(frag))
                return base.buildPagedSelect(frag);

            var ver = dialect.CurVersion;

            if (ver != null && ver.VersionNumber >= 11) {
                //由于SQLServer 的翻页语句必须有order by 
                if (!frag.orderbyInner.HasText()) {
                    frag.orderbyInner = "1";
                }

                return buildPagedSelectTail(frag, sb => AppendOffsetFetch(sb, frag));
            }
                

            return buildPagedByRowNumber(frag);
        }
/// <summary>
/// Builds an INSERT statement.
/// </summary>
 

        public override string buildInsert(FragSQL frag)
        {
            StringBuilder sb = new StringBuilder();
            // sql server 支持直接插入多行数据、单行数据
            sb.AppendFormat("INSERT INTO {0} ", frag.insertInto);
            if(string.IsNullOrWhiteSpace( frag.insertCols)==false )
            {
                sb.AppendFormat(" ({0}) ", frag.insertCols);
            }

            if(frag.insertValues != null && frag.insertValues.Count>0)
            {
                //多行插入
                sb.AppendFormat(" VALUES ({0})",string.Join("),(",frag.insertValues));
                return sb.ToString();
            }
            //如果 from 不为空，则是 insert into  select...
            if (!string.IsNullOrWhiteSpace(frag.fromInner)|| !string.IsNullOrWhiteSpace(frag.selectInner)) {
                //此时的单行插入值，实际上是select 部分。但是，如果明确给了 select内容，则使用 select内容
                sb.Append(" SELECT ");
                if (frag.distincted)
                {
                    sb.Append("DISTINCT ");
                }
                if (!string.IsNullOrWhiteSpace(frag.selectInner)) {
                    sb.AppendFormat(" {0} ", frag.selectInner);
                }
                else
                {
                    sb.AppendFormat(" {0} ", frag.insertValue);
                }
                //追加from 部分。
                if (!string.IsNullOrWhiteSpace(frag.fromInner)) {
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
/// Builds an UPDATE FROM statement.
/// </summary>
 

        public override string buildUpdateFrom(FragSQL frag)
        {
            var sb = new StringBuilder();
            // update a set a=b from ... where ...
            sb.AppendFormat("UPDATE {0} SET {1} ", frag.updateTo, this.buildSetPart(frag));
            sb.AppendFormat(" FROM {0} ", frag.fromInner);
            if (!string.IsNullOrWhiteSpace(frag.whereInner)) {
                sb.AppendFormat(" WHERE {0}", frag.whereInner);
            }
            return sb.ToString() ;
        }
/// <summary>
/// Builds a MERGE INTO statement.
/// </summary>
 

        public override string buildMergeInto(FragMergeInto frag)
        {
            //merge into 目标表 a
            //using 源表 b
            //on a.条件字段1 = b.条件字段1 and a.条件字段2 = b.条件字段2...
            //when matched update set a.字段1 = b.字段1,
            //      a.字段2 = b.字段2
            //when not matched insert values(b.字段1, b.字段2)
            //when not matched by source
            //then delete

            return this.buildMergeIntoGeneral(frag);
        }
        #endregion

        #region DDL
        private string getSchema(DDLFragSQL frag) {
            if (!string.IsNullOrWhiteSpace(frag.Schema)) { 
                return frag.Schema ;
            }
            return "dbo";
        }
 /// <summary>
 /// Builds a primary-key constraint fragment.
 /// </summary>
 
        protected override string buildConstrainPK(string pkname, string fields)
        {
            return string.Format("CONSTRAINT {0} PRIMARY KEY ({1})",pkname,fields);
        }
        /// <summary>
        /// 整体注释的处理（sp_addextendedproperty 已由 buildDDLSoloCaptions 输出）
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        protected override string buildDDLFieldsCaption(DDLFragSQL frag)
        {
            return string.Empty;
        }

        /// <inheritdoc/>
        protected override bool SoloCaptionsAsSeparateStatements => true;

        /// <summary>
        /// 独立的字段注释
        /// </summary>
        /// <param name="frag"></param>
        /// <param name="fie"></param>
        /// <returns></returns>
        public override string buildSoloFieldCaption(DDLFragSQL frag, DDLField fie)
        {
            return string.Format(@"EXEC sp_addextendedproperty @name = N'MS_Description',@value = '{2}',@level0type = N'SCHEMA', @level0name = '{3}',  @level1type = N'TABLE', @level1name = '{0}', @level2type = N'COLUMN', @level2name = '{1}';", frag.Table, fie.FieldName, fie.Caption,getSchema(frag));
        }
        /// <summary>
        /// 独立的表注释
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        public override string buildSoloTableCaption(DDLFragSQL frag)
        {
            return string.Format(@"EXEC sp_addextendedproperty @name = N'MS_Description', @value = N'{1}', @level0type = N'SCHEMA', @level0name = '{2}',  @level1type = N'TABLE',  @level1name = '{0}';", frag.Table, frag.TableCaption,getSchema(frag));
        }
        /// <summary>
        /// 复制表结构
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        public override string buildCopyTableSchema(DDLFragSQL frag)
        {
            return string.Format("SELECT * INTO {0} FROM {1} WHERE 1=2", frag.Table, frag.SrcTable);
        }
/// <summary>
/// Builds a copy-table statement.
/// </summary>
 

        public override string buildCopyTable(DDLFragSQL frag)
        {
            return string.Format("SELECT * INTO {0} FROM {1}", frag.Table, frag.SrcTable);
        }
 /// <summary>
 /// Builds a DROP INDEX statement.
 /// </summary>
 
        public override string buildDropIndex(string indexName, string tableName = null)
        {
            return string.Format("DROP INDEX {0}.{1}", tableName, indexName);
        }
/// <summary>
/// Returns SQL for retrieving an auto-generated identity value.
/// </summary>
 

        public override string getTableAutoIdSQL()
        {
            return "IDENTITY(1,1)";
        }
 /// <summary>
 /// Builds a CREATE DATABASE statement.
 /// </summary>
 
        public override string CreateDataBaseBy(string database){ 
            return string.Format("CREATE DATABASE {0}  ",database);
        }
 /// <summary>
 /// Builds an ADD PRIMARY KEY statement.
 /// </summary>
 
        public override string AddPrimaryKeyBy(string tableName, string columnName,string indexName) {
            return string.Format("ALTER TABLE {0} ADD CONSTRAINT {1} PRIMARY KEY({2})"
                ,tableName,indexName,columnName);
        }
 /// <summary>
 /// Builds an ADD COLUMN statement.
 /// </summary>
 
        public override string AddColumnToTableBy(string tableName, string columnName,string dataType,string defval,string nullable,string p2,string p3){
            return string.Format("ALTER TABLE {0} ADD {1} {2}{3} {4} {5} {6}",
                tableName,columnName,dataType,
                defval,nullable,p2,p3
                );
        }
 /// <summary>
 /// Builds an ALTER COLUMN statement.
 /// </summary>
 
        public override string AlterColumnToTableby(string tableName, string columnName, string dataType, string defval, string nullable, string p2, string p3)
        {
            return string.Format("ALTER TABLE {0} ALTER COLUMN {1} {2}{3} {4} {5} {6}",
                tableName, columnName, dataType,
                defval, nullable, p2, p3
                );
        }
 /// <summary>
 /// Builds a CREATE TABLE statement.
 /// </summary>
 
        public override string CreateTableBy(string tableName,string detail){
            return string.Format("CREATE TABLE {0}({1})",tableName,detail);
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
 

        public override string DropColumnToTableBy(string tableName, string columnName){
            return string.Format("ALTER TABLE {0} DROP COLUMN {1}",tableName,columnName);
        }
 /// <summary>
 /// Builds a DROP CONSTRAINT statement.
 /// </summary>
 
        public override string DropConstraintBy(string tableName, string constraintName){
            return string.Format("ALTER TABLE {0} DROP CONSTRAINT  {1}",tableName,constraintName);
        }
 /// <summary>
 /// Builds a RENAME COLUMN statement.
 /// </summary>
 
        public override string RenameColumnBy(string tableName,string oldName,string newName){
            return string.Format("exec sp_rename '{0}.{1}','{2}','column'",tableName,oldName,newName);
        }
 /// <summary>
 /// Builds SQL to add a column caption/comment.
 /// </summary>
 
        public override string AddColumnCaptionBy(string tableName, string columnName,string caption){
            return string.Format("EXECUTE sp_addextendedproperty N'MS_Description', '{2}', N'user', N'dbo', N'table', N'{1}', N'column', N'{0}'",
                columnName,tableName,caption
                );
        }
 /// <summary>
 /// Builds SQL to update a column caption/comment.
 /// </summary>
 
        public override string UpdateColumnCaptionBy(string tableName, string columnName, string caption)
        {
            return string.Format("EXECUTE sp_updateextendedproperty N'MS_Description', '{2}', N'user', N'dbo', N'table', N'{1}', N'column', N'{0}'",
                columnName, tableName, caption);
        }
 /// <summary>
 /// Builds SQL to delete a column caption/comment.
 /// </summary>
 
        public override string DeleteColumnCaptionBy(string tableName, string columnName){
            return string.Format("EXEC sp_dropextendedproperty 'MS_Description','user',dbo,'table','{1}','column','{0}'",
                columnName,tableName
                );
        }
 /// <summary>
 /// Builds SQL that checks whether a column caption exists.
 /// </summary>
 
        public override string IsAnyColumnCaptionBy(string columnName, string tableName){
            return string.Format("SELECT A.name AS table_name, B.name AS column_name, C.value AS column_description FROM sys.tables A LEFT JOIN sys.extended_properties C ON C.major_id = A.object_id LEFT JOIN sys.columns B ON B.object_id = A.object_id AND C.minor_id = B.column_id INNER JOIN sys.schemas SC ON SC.schema_id = A.schema_id AND SC.name = 'dbo' WHERE A.name = '{1}' AND b.name = '{0}'",
                columnName,tableName
                );
        }
 /// <summary>
 /// Builds SQL to add a table caption/comment.
 /// </summary>
 
        public override string AddTableCaptionBy(string tableName, string caption){
            return string.Format("EXECUTE sp_addextendedproperty N'MS_Description', '{1}', N'user', N'dbo', N'table', N'{0}', NULL, NULL",tableName,caption);
        }
 /// <summary>
 /// Builds SQL to update a table caption/comment.
 /// </summary>
 
        public override string UpdateTableCaptionBy(string tableName, string caption)
        {
            return string.Format("EXECUTE sp_updateextendedproperty N'MS_Description', '{1}', N'user', N'dbo', N'table', N'{0}', NULL, NULL", tableName, caption);
        }
 /// <summary>
 /// Builds SQL to delete a table caption/comment.
 /// </summary>
 
        public override string DeleteTableCaptionBy(string tableName){
            return string.Format("EXEC sp_dropextendedproperty 'MS_Description','user',dbo,'table','{0}' ",tableName);
        }
 /// <summary>
 /// Builds SQL that checks whether a table caption exists.
 /// </summary>
 
        public override string IsAnyTableCaptionBy(string tableName){
            return string.Format("SELECT C.class_desc" +
                " FROM sys.tables A " +
                "LEFT JOIN sys.extended_properties C ON C.major_id = A.object_id " +
                "INNER JOIN sys.schemas SC ON  SC.schema_id=A.schema_id AND SC.name='dbo'" +
                " WHERE A.name = '{0}'  AND minor_id=0",tableName);
        }
 /// <summary>
 /// Builds a RENAME TABLE statement.
 /// </summary>
 
        public override string RenameTableBy(string oldTableName, string newTableName){
            return string.Format("EXEC sp_rename '{0}','{1}'",oldTableName,newTableName);
        }
 /// <summary>
 /// Builds a CREATE INDEX statement.
 /// </summary>
 
        public override string CreateIndexBy(string indexName,string tableName,string columnName, string unique){
            return string.Format("CREATE {3} NONCLUSTERED INDEX Index_{0}_{2} ON {0}({1})",tableName,columnName,indexName,unique);
        }
 /// <summary>
 /// Builds SQL that checks whether an index exists.
 /// </summary>
 
        public override string IsAnyIndexBy(string indexName){
            return string.Format("SELECT COUNT(*) FROM sys.indexes WHERE name='{0}'",indexName);
        }
 /// <summary>
 /// Builds SQL to check system-table permissions.
 /// </summary>
 
        public override string CheckSystemTablePermissionsBy(){
            return "SELECT TOP 1 id FROM sysobjects";
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
            return "DATETIME";
        }
 /// <summary>
 /// Returns the dialect-specific Boolean column type.
 /// </summary>
 
        public override string getBoolColumnType()
        {
            return "BIT";
        }
 /// <summary>
 /// Returns the dialect-specific GUID column type.
 /// </summary>
 
        public override string getGuidColumnType()
        {
            return "UNIQUEIDENTIFIER";
        }
        #endregion


    }
}
