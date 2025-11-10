
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
            _provideType = "Oracle.ManagedDataAccess.Client.OracleClientFactory,Oracle.ManagedDataAccess";
        }

        public override string wrapKeyword(string value)
        {
            if(value.StartsWith("\"") && value.EndsWith("\""))
            {
                return value;
            }
            return "\""+value+"\"";
        }

        #region DML语句
        public override string buildSelect(FragSQL frag)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT ");
            if (frag.distincted)
            {
                sb.Append("distinct ");
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

            if (frag.toped > -1)
            {
                sb.Append("LIMIT ");
                sb.Append(frag.toped);
                sb.Append(" ");
            }

            return sb.ToString();
        }

        public override string buildPagedSelect(FragSQL frag)
        {
            return this.buildPagedSelectTail(frag, (sb) => {
                if (frag.pageSize > -1)
                {
                    int end = frag.pageSize * (frag.pageNum - 1);
                    sb.Append("LIMIT ");
                    sb.Append(frag.pageSize);
                    sb.Append(" OFFSET ");
                    sb.Append(end);

                }
                else if (frag.toped > -1)
                {
                    sb.Append("limit ");
                    sb.Append(frag.toped);
                    sb.Append(" ");
                }
            });
        }

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
                    sb.AppendFormat(" SELECT {0} from dual ", frag.insertValues[i]);
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
                sb.Append(" select ");
                if (frag.distincted)
                {
                    sb.Append("distinct ");
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
                        sb.Append("group by ");
                        sb.Append(frag.groupByInner);
                        sb.Append(" ");
                    }
                    if (!string.IsNullOrWhiteSpace(frag.havingInner))
                    {
                        sb.Append("having ");
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
            return string.Format("alter table {0} ALTER COLUMN {1} {2}{3} {4} {5} {6}",
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
            return string.Format("comment on column {1}.{0} is '{2}'", columnName, tableName, caption
                );
        }
        public override string DeleteColumnCaptionBy(string tableName, string columnName)
        { 
            return string.Format("comment on column {1}.{0} is ''",
                columnName, tableName
                );
        }


        public override string AddTableCaptionBy(string tableName, string caption)
        { 
            return string.Format("comment on table {0} is '{1}'", tableName, caption);
        }
        public override string DeleteTableCaptionBy(string tableName)
        { 
            return string.Format("comment on table {0} is ''", tableName);
        }


        public override string RenameTableBy(string oldTableName, string newTableName)
        { 
            return string.Format("alter table 表名 {0} to {1}", oldTableName, newTableName);
        }
        public override string CreateIndexBy(string indexName, string tableName, string columnName, string unique)
        { 
            return string.Format("CREATE {3} INDEX Index_{0}_{2} ON {0} ({1})", tableName, columnName, indexName, unique);
        }
        public override string IsAnyIndexBy(string indexName)
        { 
            return string.Format("  Select count(1) from (SELECT to_regclass('{0}') as c ) t where t.c is not null", indexName);
        }
        public override string CheckSystemTablePermissionsBy(){ 
            return "select 1 from information_schema.columns limit 1 offset 0";
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
