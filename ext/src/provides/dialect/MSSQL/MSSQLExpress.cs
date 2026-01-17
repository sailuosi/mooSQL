
using mooSQL.data.builder;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public class MSSQLExpress:SQLExpression
    {
        public MSSQLExpress(Dialect dia) : base(dia)
        {
            _paraPrefix = "@";
            _selectAutoIncrement = "Select Scope_Identity()";
            _provideType = "System.Data.SqlClient.SqlClientFactory,System.Data.SqlClient";
        }

        public override string wrapKeyword(string value)
        {
            if (value.StartsWith("[") && value.EndsWith("]")) { 
                return value;
            }
            return "[" + value + "]";
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

        public override string buildSelect(FragSQL frag)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT ");
            if (frag.distincted)
            {
                sb.Append("distinct ");
            }
            if (frag.toped > -1)
            {
                sb.Append("top ");
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
            sb.Append("from ");
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
                sb.Append("where ");
                sb.Append(frag.whereInner);
                sb.Append(" ");
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

            if (!string.IsNullOrWhiteSpace(frag.orderbyInner))
            {
                sb.Append("order by ");
                sb.Append(frag.orderbyInner);
                sb.Append(" ");
            }



            return sb.ToString();
        }

        /*
         * 
         */
        public override string buildPagedSelect(FragSQL frag)
        {
            var ver = this.dialect.CurVersion;

            if (ver !=null && ver.VersionNumber >= 11) {
                //当 SQL server的版本在 2012以后，翻页支持 OFFSET 40 ROWS FETCH NEXT 10 ROWS ONLY; 这样的语法
                var sb = new StringBuilder();
                var tar = this.buildPagedSelectTail(frag, (sb) => {
                    if (frag.pageSize > -1)
                    {
                        int end = frag.pageSize * (frag.pageNum - 1);
                        sb.Append("OFFSET ");
                        sb.Append(end);
                        sb.Append(" ROWS FETCH NEXT ");
                        sb.Append(frag.pageSize);
                        sb.Append(" ROWS ONLY ");

                    }
                    else if (frag.toped > -1)
                    {
                        sb.Append(" FETCH FIRST ");
                        sb.Append(frag.toped);
                        sb.Append(" ROWS ONLY ");
                    }
                });
                return tar;
            }
            return this.buildPagedByRowNumber(frag);
        }

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
                sb.Append(" select ");
                if (frag.distincted)
                {
                    sb.Append("distinct ");
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

        public override string buildUpdateFrom(FragSQL frag)
        {
            var sb = new StringBuilder();
            // update a set a=b from ... where ...
            sb.AppendFormat("update {0} set {1} ", frag.updateTo, this.buildSetPart(frag));
            sb.AppendFormat(" from {0} ", frag.fromInner);
            if (!string.IsNullOrWhiteSpace(frag.whereInner)) {
                sb.AppendFormat(" where {0}", frag.whereInner);
            }
            return sb.ToString() ;
        }

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
        protected override string buildConstrainPK(string pkname, string fields)
        {
            return string.Format("CONSTRAINT {0} PRIMARY KEY ({1})",pkname,fields);
        }
        /// <summary>
        /// 整体注释的处理
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        protected override string buildDDLFieldsCaption(DDLFragSQL frag)
        {
            return string.Empty;
            //return buildDDLSoloCaptions(frag);
        }
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

        public override string buildCopyTable(DDLFragSQL frag)
        {
            return string.Format("SELECT * INTO {0} FROM {1}", frag.Table, frag.SrcTable);
        }
        public override string buildDropIndex(string indexName, string tableName = null)
        {
            return string.Format("DROP INDEX {0}.{1}", tableName, indexName);
        }

        public override string getTableAutoIdSQL()
        {
            return "IDENTITY(1,1)";
        }
        public override string CreateDataBaseBy(string database){ 
            return string.Format("create database {0}  ",database);
        }
        public override string AddPrimaryKeyBy(string tableName, string columnName,string indexName) {
            return string.Format("ALTER TABLE {0} ADD CONSTRAINT {1} PRIMARY KEY({2})"
                ,tableName,indexName,columnName);
        }
        public override string AddColumnToTableBy(string tableName, string columnName,string dataType,string defval,string nullable,string p2,string p3){
            return string.Format("ALTER TABLE {0} ADD {1} {2}{3} {4} {5} {6}",
                tableName,columnName,dataType,
                defval,nullable,p2,p3
                );
        }
        public override string AlterColumnToTableby(string tableName, string columnName, string dataType, string defval, string nullable, string p2, string p3)
        {
            return string.Format("ALTER TABLE {0} ALTER COLUMN {1} {2}{3} {4} {5} {6}",
                tableName, columnName, dataType,
                defval, nullable, p2, p3
                );
        }
        public override string CreateTableBy(string tableName,string detail){
            return string.Format("CREATE TABLE {0}({1})",tableName,detail);
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

        public override string DropColumnToTableBy(string tableName, string columnName){
            return string.Format("ALTER TABLE {0} DROP COLUMN {1}",tableName,columnName);
        }
        public override string DropConstraintBy(string tableName, string constraintName){
            return string.Format("ALTER TABLE {0} DROP CONSTRAINT  {1}",tableName,constraintName);
        }
        public override string RenameColumnBy(string tableName,string oldName,string newName){
            return string.Format("exec sp_rename '{0}.{1}','{2}','column'",tableName,oldName,newName);
        }
        public override string AddColumnCaptionBy(string tableName, string columnName,string caption){
            return string.Format("EXECUTE sp_addextendedproperty N'MS_Description', '{2}', N'user', N'dbo', N'table', N'{1}', N'column', N'{0}'",
                columnName,tableName,caption
                );
        }
        public override string DeleteColumnCaptionBy(string tableName, string columnName){
            return string.Format("EXEC sp_dropextendedproperty 'MS_Description','user',dbo,'table','{1}','column','{0}'",
                columnName,tableName
                );
        }
        public override string IsAnyColumnCaptionBy(string columnName, string tableName){
            return string.Format("SELECT A.name AS table_name, B.name AS column_name, C.value AS column_description FROM sys.tables A LEFT JOIN sys.extended_properties C ON C.major_id = A.object_id LEFT JOIN sys.columns B ON B.object_id = A.object_id AND C.minor_id = B.column_id INNER JOIN sys.schemas SC ON SC.schema_id = A.schema_id AND SC.name = 'dbo' WHERE A.name = '{1}' and b.name = '{0}'",
                columnName,tableName
                );
        }
        public override string AddTableCaptionBy(string tableName, string caption){
            return string.Format("EXECUTE sp_addextendedproperty N'MS_Description', '{1}', N'user', N'dbo', N'table', N'{0}', NULL, NULL",tableName,caption);
        }
        public override string DeleteTableCaptionBy(string tableName){
            return string.Format("EXEC sp_dropextendedproperty 'MS_Description','user',dbo,'table','{0}' ",tableName);
        }
        public override string IsAnyTableCaptionBy(string tableName){
            return string.Format("SELECT C.class_desc" +
                " FROM sys.tables A " +
                "LEFT JOIN sys.extended_properties C ON C.major_id = A.object_id " +
                "INNER JOIN sys.schemas SC ON  SC.schema_id=A.schema_id AND SC.name='dbo'" +
                " WHERE A.name = '{0}'  AND minor_id=0",tableName);
        }
        public override string RenameTableBy(string oldTableName, string newTableName){
            return string.Format("EXEC sp_rename '{0}','{1}'",oldTableName,newTableName);
        }
        public override string CreateIndexBy(string indexName,string tableName,string columnName, string unique){
            return string.Format("CREATE {3} NONCLUSTERED INDEX Index_{0}_{2} ON {0}({1})",tableName,columnName,indexName,unique);
        }
        public override string IsAnyIndexBy(string indexName){
            return string.Format("select count(*) from sys.indexes where name='{0}'",indexName);
        }
        public override string CheckSystemTablePermissionsBy(){
            return "select top 1 id from sysobjects";
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
            return "DATETIME";
        }
        public override string getBoolColumnType()
        {
            return "BIT";
        }
        public override string getGuidColumnType()
        {
            return "UNIQUEIDENTIFIER";
        }
        #endregion


    }
}
