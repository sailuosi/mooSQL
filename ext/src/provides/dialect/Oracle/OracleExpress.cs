

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using mooSQL.data.builder;


namespace mooSQL.data
{
    /// <summary>
    /// Oracle数据库表达式，封装了Oracle特有的语法和特性。
    /// </summary>
    public class OracleExpress:SQLExpression
    {
        /// <summary>
        /// Oracle数据库表达式构造函数。
        /// </summary>
        /// <param name="dia"></param>
        public OracleExpress(OracleDialect dia):base(dia) {
            _paraPrefix = ":";
            _selectAutoIncrement = "";
            _provideType = "Oracle.ManagedDataAccess.Client.OracleClientFactory,Oracle.ManagedDataAccess";
            _oracleDialect = dia;
        }
        /// <summary>
        /// Oracle方言对象。
        /// </summary>
        public OracleDialect _oracleDialect;
        /// <summary>
        /// 封装Oracle关键字，Oracle关键字需要用双引号包裹。
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override string wrapKeyword(string value)
        {
            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                return value;
            }
            return "\"" + value + "\"";
        }
        /// <summary>
        /// Oracle：IN子句最多支持1000个表达式，超过则抛出ORA-01795错误。
        /// </summary>
        /// <returns></returns>
        public override int? getWhereInLimit()
        {
            return 1000;
        }

        public override string charIndex(string substring, string str) => $"INSTR({str}, {substring})";

        public override string charIndex(string substring, string str, string start)
            => $"INSTR({str}, {substring}, {start})";

        public override string isNullOrWhiteSpace(string expr)
            => $"({expr} IS NULL OR LTRIM({expr}, ' \t\n\r\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u205f\u3000') IS NULL)";

        public override string dateDiffWeek(string start, string end)
            => $"(CAST ({end} as DATE) - CAST ({start} as DATE)) / 7";

        public override string dateDiffDay(string start, string end)
            => $"(CAST ({end} as DATE) - CAST ({start} as DATE))";

        public override string dateDiffHour(string start, string end)
            => $"(CAST ({end} as DATE) - CAST ({start} as DATE)) * 24";

        public override string dateDiffMinute(string start, string end)
            => $"(CAST ({end} as DATE) - CAST ({start} as DATE)) * 1440";

        public override string dateDiffSecond(string start, string end)
            => $"(CAST ({end} as DATE) - CAST ({start} as DATE)) * 86400";

        public override string dateDiffMillisecond(string start, string end)
            => "1000 * (EXTRACT(SECOND FROM CAST (" + end + " as TIMESTAMP) - CAST (" + start + " as TIMESTAMP))"
               + " + 60 * (EXTRACT(MINUTE FROM CAST (" + end + " as TIMESTAMP) - CAST (" + start + " as TIMESTAMP))"
               + " + 60 * (EXTRACT(HOUR FROM CAST (" + end + " as TIMESTAMP) - CAST (" + start + " as TIMESTAMP))"
               + " + 24 * EXTRACT(DAY FROM CAST (" + end + " as TIMESTAMP) - CAST (" + start + " as TIMESTAMP)))))";

        #region DML语句
        /// <summary>
        /// 构建Select语句，Oracle特有的分页方式。
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



            var res= sb.ToString();

            //对Oracle来说，top使用rownum伪列实现
            if (frag.toped > -1)
            {
                //这里要做版本兼容处理，Oracle 12c 以上版本支持 fetch first xx rows only
                if (this._oracleDialect.Is12cOrHigher())
                {
                    //FETCH FIRST 200 ROWS ONLY
                    res += " FETCH FIRST " + frag.toped+" ROWS ONLY";
                }
                else
                {
                    /*
                     SELECT * FROM (   ) WHERE ROWNUM <= 200;
                     */
                    res = string.Format("SELECT * FROM ( {0}  ) toptmp WHERE ROWNUM <= {1}",res,frag.toped);
                }
            }
            return res;
        }

        /// <inheritdoc/>
        protected override string WrapExistScalar(string existsSubquery)
            => $"SELECT CASE WHEN EXISTS ({existsSubquery}) THEN 1 ELSE 0 END FROM DUAL";

        /// <inheritdoc/>
        protected override string AppendExistSubqueryTail(string innerSql, FragSQL frag)
        {
            if (_oracleDialect.Is12cOrHigher())
                return innerSql + " FETCH FIRST 1 ROW ONLY";
            if (innerSql.IndexOf(" WHERE ", StringComparison.OrdinalIgnoreCase) >= 0)
                return innerSql + " AND ROWNUM = 1";
            return innerSql + " WHERE ROWNUM = 1";
        }

        /// <summary>
        /// 构建分页Select语句，Oracle特有的分页方式。
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        public override string buildPagedSelect(FragSQL frag)
        {
            if (!HasSkipTakePaging(frag))
                return base.buildPagedSelect(frag);

            if (_oracleDialect.Is12cOrHigher())
                return buildPagedSelectTail(frag, sb => AppendOffsetFetch(sb, frag));

            /* 标准三层嵌套的翻页写法，Oracle 12c以下版本
             SELECT * FROM (
                SELECT tt.*, ROWNUM AS rn FROM (
                    SELECT * FROM emp 
                    WHERE deptno = 10 
                    ORDER BY hiredate DESC  -- 排序条件
                ) tt 
                WHERE ROWNUM <= 30  -- 结束行号
            ) WHERE rn > 20;        -- 起始行号
             */

            var res = this.buildPagedSelectTail(frag, (sb) => {
                var nowSQL = sb.ToString();

            //SELECT * FROM ( SELECT tt.*, ROWNUM AS rn FROM( ) tt WHERE ROWNUM <= 30 ) WHERE rn > 20

                if (frag.pageSize > -1)
                {
                    int start = frag.pageSize * (frag.pageNum - 1);
                    int end = start + frag.pageSize;
                    sb.Clear();
                    sb.AppendFormat("SELECT * FROM ( SELECT p_gtt.*, ROWNUM AS pgtmp_rn FROM({0}) p_gtt WHERE ROWNUM <= {1} ) WHERE pgtmp_rn > {2}"
                        , nowSQL, end, start);
                }
                else if (frag.toped > -1)
                {
                    int end = frag.pageSize * (frag.pageNum );
                    sb.Clear();
                    sb.AppendFormat("SELECT p_gtt.* FROM({0}) p_gtt WHERE ROWNUM <= {1}"
                        , nowSQL, end);
                }
            });
            return res;
            //return buildPagedByRowNumber(frag);
        }
        /// <summary>
        /// 构建insert语句，Oracle特有的语法。
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
                sb.Append(" VALUES ");
                for(var i=0; i<frag.insertValues.Count;i++)
                {
                    sb.AppendFormat(" SELECT {0} FROM DUAL ", frag.insertValues[i]);
                    if(i < frag.insertValues.Count-1) { 
                        sb.Append("UNION"); 
                    }
                }
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
        /// 构建MergeInto语句，Oracle特有的语法。
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
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
            return string.Format("CREATE TABLE {0} AS SELECT * FROM {1} WHERE 1 = 0", frag.Table, frag.SrcTable);
        }
        /// <summary>
        /// 复制表结构但不包含数据
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        public override string buildCopyTable(DDLFragSQL frag)
        {
            return string.Format("CREATE TABLE {0} AS SELECT * FROM {1} ", frag.Table, frag.SrcTable);
        }

        public override string buildDropIndex(string indexName, string tableName = null)
        {
            return string.Format("DROP INDEX {0}", indexName);
        }


        public override string CreateIndexBy(string indexName, string tableName, string columnName, string unique)
        {
            return string.Format("CREATE {3} INDEX Index_{0}_{2} ON {0}({1})", tableName, columnName, indexName, unique);
        }
        /// <summary>
        /// 判断索引是否存在，Oracle中通过查询数据字典视图来确定。
        /// </summary>
        /// <param name="indexName"></param>
        /// <returns></returns>
        public override string IsAnyIndexBy(string indexName)
        {
            return string.Format("SELECT COUNT(1) FROM user_ind_columns WHERE index_name=('{0}')", indexName);
        }
        /// <summary>
        /// 创建数据库，Oracle中没有直接的CREATE DATABASE语句，而是通过DBA权限创建一个新的PL/SQL数据库实例。
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public override string CreateDataBaseBy(string database)
        {
            return string.Format("CREATE DATABASE {0}",database);
        }
        public override string AddPrimaryKeyBy(string tableName, string columnName, string indexName) { 
            return string.Format("ALTER TABLE {0} ADD CONSTRAINT {1} PRIMARY KEY({2})", tableName, indexName, columnName);
        }
        public override string AddColumnToTableBy(string tableName, string columnName, string dataType, string defval, string nullable, string p2, string p3)
        {
            return string.Format("ALTER TABLE {0} ADD ({1} {2}{3} {4} {5} {6})",
                tableName, columnName, dataType,
                defval, nullable, p2, p3
                );
        }
        public override string AlterColumnToTableby(string tableName, string columnName, string dataType, string defval, string nullable, string p2, string p3)
        { 
            return string.Format("ALTER TABLE {0} MODIFY ({1} {2}{3} {4} {5} {6}) ",
                tableName, columnName, dataType,
                defval, nullable, p2, p3
                );
        }
        public override string CreateTableBy(string tableName, string detail)
        { 
            return string.Format("CREATE TABLE {0}(\r\n{1})", tableName, detail);
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
            return string.Format("ALTER TABLE {0} DROP CONSTRAINT  {1}", tableName, constraintName);
        }
        public override string RenameColumnBy(string tableName, string oldName, string newName)
        { 
            return string.Format("ALTER TABLE {0} RENAME COLUMN {1} TO {2}", tableName, oldName, newName);
        }
        public override string AddColumnCaptionBy(string tableName, string columnName, string caption)
        { 
            return string.Format("COMMENT ON COLUMN {1}.{0} IS '{2}'",
                columnName, tableName, caption
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
        public override string IsAnyColumnCaptionBy(string tableName, string columnName)
        { 
            return string.Format("SELECT * FROM user_col_comments WHERE Table_Name='{1}' AND COLUMN_NAME='{0}' ORDER BY column_name",
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
        public override string IsAnyTableCaptionBy(string tablename){ 
            return string.Format("SELECT * FROM user_tab_comments WHERE Table_Name='{0}'ORDER BY Table_Name",tablename);
        }
        public override string RenameTableBy(string oldTableName, string newTableName)
        { 
            return string.Format("ALTER TABLE {0} RENAME TO {1}", oldTableName, newTableName);
        }
        public override string CheckSystemTablePermissionsBy(){ 
            return "SELECT t.table_name FROM user_tables t  WHERE rownum=1";
        }
        /// <summary>
        /// null的声明
        /// </summary>
        /// <returns></returns>
        public override string CreateTableNullBy(){ 
            return "";
        }
        /// <summary>
        /// not null的声明
        /// </summary>
        /// <returns></returns>
        public override string CreateTableNotNullBy(){ 
            return " NOT NULL ";
        }
        /// <summary>
        /// 主键的声明
        /// </summary>
        /// <returns></returns>
        public override string CreateTablePirmaryKeyBy(){
            return "PRIMARY KEY";
        }
        #endregion


        #region 字段类型
        public override string getStringColumnType(int length)
        {
            return string.Format("VARCHAR2({0})",length);
        }
        public override string getDateTimeColumnType(int length)
        {
            return "TIMESTAMP";
        }
        public override string getNumberColumnType(int precision, int scale)
        {
            return string.Format("NUMBER({0}, {1})", precision, scale);
        }
        public override string getBoolColumnType()
        {
            return "NUMBER(1)";
        }
        public override string getGuidColumnType()
        {
            return "CHAR(36)";
        }
        #endregion
    }
}
