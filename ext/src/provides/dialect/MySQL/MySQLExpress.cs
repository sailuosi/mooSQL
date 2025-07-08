
using mooSQL.data.builder;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// MYSQL的特性语法
    /// </summary>
    public class MySQLExpress:SQLExpression
    {
        public MySQLExpress(Dialect dia) : base(dia) {
            _paraPrefix = "?";
            _selectAutoIncrement = "Select Last_Insert_Id()";
            _provideType = "MySql.Data.MySqlClient.MySqlClientFactory,MySql.Data";
        }

        public override string wrapKeyword(string value)
        {
            if (value.StartsWith("`") && value.EndsWith("`")) { 
                return value;
            }
            return "`" + value.Replace("`", "``") + "`";
        }

        #region DML语句
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
                sb.Append("distinct ");
            }
            sb.Append(frag.selectInner);
            this.buildSelectFromToOrderPart(frag, sb);

            if (frag.toped > -1)
            {
                sb.Append("limit ");
                sb.Append(frag.toped);
                sb.Append(" ");
            }

            return sb.ToString();
        }

        private bool isMySQL80More() {

            if (DB.versionNumber > 8 || (this.DB.version !=null &&  this.DB.version.StartsWith("8."))) {
                return true;
            }
            return false;
        }

        public override string buildPagedSelect(FragSQL frag)
        {
            if (this.isMySQL80More()) {
                return buildPagedByRowNumber(frag);
            }
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
        /// <summary>
        /// 使mysql的update from 语句，完全支持sqlserver的格式。
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        public override string buildUpdateFrom(FragSQL frag)
        {
            /**
             * 创建MYSQL下的update from 必须使用inner join
             * @return update tablename inner join a on a.pid=tablename.id set ... where ...
             */
            var sb = new StringBuilder();
            // update a set a=b from ... where ...
            //将left join 更改为inner join 
            if (RegxUntils.test(frag.fromInner.ToLower(), @"\sleft\s+join\s")) {
                var reg = new Regex(@"\sleft\s+join\s", RegexOptions.IgnoreCase);
                frag.fromInner = reg.Replace(frag.fromInner, " inner join ");// .ToLower().Replace(@"\sleft\s+join\s"," inner join ");
            }
            sb.AppendFormat("update {0} set ", frag.fromInner);
            if (!string.IsNullOrWhiteSpace(frag.updateTo))
            {
                //如果设置了目标则作适配性修正。如果set列没有表前缀，增加表前缀。
                List<string> fiexedset = new List<string>();
                if (frag.setInner.Contains(","))
                {
                    var sets = frag.setInner.Split(',');

                    foreach (var s in sets)
                    {
                        var t = fixSetField(s, frag);
                        if (!string.IsNullOrWhiteSpace(t))
                        {
                            fiexedset.Add(t);
                        }

                    }

                }
                else {
                    var t = fixSetField(frag.setInner, frag);
                    if (!string.IsNullOrWhiteSpace(t))
                    {
                        fiexedset.Add(t);
                    }
                }
                //没有设置列信息，返回空
                if (fiexedset.Count == 0) {
                    return "";
                }
                sb.Append(" ");
                sb.Append(string.Join(",", fiexedset)); 
                sb.Append(" ");
                
            }
            else {
                sb.Append(" ");
                sb.Append(string.Join(",", frag.setInner));
                sb.Append(" ");
            }
            
            if (!string.IsNullOrWhiteSpace(frag.whereInner))
            {
                sb.AppendFormat(" where {0}", frag.whereInner);
            }
            return sb.ToString();
        }

        private string fixSetField(string setOne, FragSQL frag) {
            var fieldsp = setOne.Split('=');
            if (fieldsp.Length != 2) {
                return "";
            }

            var field = fieldsp[0];

            if (field.Contains(".") == false)
            {
                //要赋值的字段没有表前缀
                var t = frag.updateTo + "." + setOne.TrimStart();
                return t;
            }
            return setOne;
        }
        #endregion
        #region DDL语句

        /// <summary>
        ///  建表时的列注释
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        protected override string buildDDLFieldCaption(DDLField field)
        {
            return string.Format("COMMENT '{0}'", field.Caption);
        }
        /// <summary>
        /// 主键约束关键字
        /// </summary>
        /// <param name="pkname"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        protected override string buildConstrainPK(string pkname, string fields)
        {
            return string.Format("PRIMARY KEY ({0})",  fields);
        }

        public override string buildSoloTableCaption(DDLFragSQL frag)
        {
            return string.Format(" COMMENT '{0}';",  frag.TableCaption);
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
            var sb=new StringBuilder();
            sb.AppendFormat("CREATE TABLE {0} LIKE {1};", frag.Table, frag.SrcTable);
            return sb.ToString();
        }
        /// <summary>
        /// 复制表
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        public override string buildCopyTable(DDLFragSQL frag)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("CREATE TABLE {0} AS SELECT * FROM {1};", frag.Table, frag.SrcTable);
            return sb.ToString();
        }

        public override string buildDropIndex(string indexName, string tableName = null)
        {
            return string.Format("DROP INDEX {0} ON {1}", indexName,tableName);
        }
        /// <summary>
        /// 自增ID的定义
        /// </summary>
        /// <returns></returns>
        public override string getTableAutoIdSQL()
        {
            return "AUTO_INCREMENT";
        }
        /// <summary>
        /// 创建表
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public override string CreateDataBaseBy(string database)
        {
            return string.Format("CREATE DATABASE {0} CHARACTER SET utf8 COLLATE utf8_general_ci ",database);
        }
        public override string AddPrimaryKeyBy(string tableName, string columnName, string indexName)
        {
            return string.Format("ALTER TABLE {0} ADD PRIMARY KEY({2}) /*{1}*/", tableName, indexName, columnName);
        }
        public override string AddColumnToTableBy(string tableName, string columnName, string dataType, string defval, string nullable, string p2, string p3)
        {
            return string.Format("ALTER TABLE {0} ADD {1} {2}{3} {4} {5} {6}",
                tableName, columnName, dataType,
                defval, nullable, p2, p3
                );
        }
        public override string AlterColumnToTableby(string tableName, string columnName, string dataType, string defval, string nullable, string p2, string p3)
        { 
            return string.Format("alter table {0} change  column {1} {1} {2}{3} {4} {5} {6}",
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
        //protected override string TruncateTableSql (){ "TRUNCATE TABLE {0}";

        //protected override string DropTableSql (){ "DROP TABLE {0}";

        public override string DropColumnToTableBy(string tableName, string columnName)
        { 
            return string.Format("ALTER TABLE {0} DROP COLUMN {1}", tableName, columnName);
        }
        public override string DropConstraintBy(string tableName, string constraintName)
        { 
            return string.Format("ALTER TABLE {0} drop primary key;", tableName, constraintName);
        }
        public override string RenameColumnBy(string tableName, string oldName, string newName)
        { 
            return string.Format("alter table {0} change  column {1} {2}", tableName, oldName, newName);
        }
        /// <summary>
        /// 检查系统权限
        /// </summary>
        /// <returns></returns>
        public override string CheckSystemTablePermissionsBy (){ 
            return "select 1 from Information_schema.columns limit 0,1";
        }
        /// <summary>
        /// 表null关键字
        /// </summary>
        /// <returns></returns>
        public override string CreateTableNullBy (){ 
            return "NULL";
        }
        /// <summary>
        /// 建表非空关键字
        /// </summary>
        /// <returns></returns>
        public override string CreateTableNotNullBy (){
            return "NOT NULL";
        }
        /// <summary>
        /// 建表主键关键字
        /// </summary>
        /// <returns></returns>
        public override string CreateTablePirmaryKeyBy (){
            return "PRIMARY KEY";
        }



        public override string AddTableCaptionBy (string tableName, string caption)
        { 
            return string.Format("ALTER TABLE {0} COMMENT='{1}';", tableName, caption);
        }
        public override string DeleteTableCaptionBy (string tableName)
        { 
            return string.Format("ALTER TABLE {0} COMMENT='';", tableName);
        }

        public override string RenameTableBy (string oldTableName, string newTableName)
        { 
            return string.Format("alter table {0} rename {1}", oldTableName, newTableName);
        }
        public override string CreateIndexBy (string indexName, string tableName, string columnName,string unque)
        { 
            return string.Format("CREATE {3} INDEX Index_{0}_{2} ON {0} ({1})", tableName, columnName, indexName,unque);
        }
        /// <summary>
        /// 是否存在索引
        /// </summary>
        /// <param name="indexName"></param>
        /// <returns></returns>
        public override string IsAnyIndexBy (string indexName)
        { 
            return string.Format("SELECT count(*) FROM information_schema.statistics WHERE index_name = '{0}'", indexName);
        }
        #endregion

        #region 字段类型
        /// <summary>
        /// 时间的字段
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public override string getDateTimeColumnType(int length)
        {
            return "DATETIME";
        }
        /// <summary>
        /// 布尔字段
        /// </summary>
        /// <returns></returns>
        public override string getBoolColumnType()
        {
            return "TINYINT(1)";
        }
        /// <summary>
        /// GUId字段
        /// </summary>
        /// <returns></returns>
        public override string getGuidColumnType()
        {
            return "VARCHAR(36)";
        }
        #endregion
    }
}
