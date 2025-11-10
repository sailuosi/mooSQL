
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

            var ver = dialect.CurVersion;
            if(ver !=null && ver.VersionNumber >= 8) { 
            //if (DB.versionNumber > 8 || (this.DB.version !=null &&  this.DB.version.StartsWith("8."))) {
                return true;
            }
            return false;
        }

        public override string buildPagedSelect(FragSQL frag)
        {
            if (this.isMySQL80More()==false) {
                //LIMIT 10, 10;  -- 跳过前10条，取第11-20条
                return this.buildPagedSelectTail(frag, (sb) => {
                    if (frag.pageSize > -1)
                    {
                        int end = frag.pageSize * (frag.pageNum - 1);
                        sb.Append("LIMIT ");
                        sb.Append(end);
                        sb.Append(" , ");
                        sb.Append(frag.pageSize);

                    }
                    else if (frag.toped > -1)
                    {
                        sb.Append("limit ");
                        sb.Append(frag.toped);
                        sb.Append(" ");
                    }
                });
                //return buildPagedByRowNumber(frag);
            }
            //LIMIT 每页行数 OFFSET 跳过的行数;
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

            var setPart= this.buildSetPart(frag);
            sb.Append(setPart);
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

        private string gotTableField(string onJoin,string tableName) {
        
            var tbs= onJoin.Split('=');
            foreach (var tb in tbs) { 
                if (tb.Contains(".")  ) {

                    var tbsp = tb.Split('.');
                    if(tbsp[0].Trim() == tableName)
                    return tbsp[1];
                }
            }
            return "";
        }
        /// <summary>
        /// 不支持原始的merge into 语句，使用update from / insert from 来替代。
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        public override string buildMergeInto(FragMergeInto frag)
        {
            return this.buildMergeIntoFallBack(frag);
        }
        protected override string buildSetPart(FragSQL frag) { 
            return buildSetPart(frag.setPart, frag.updateTo);
        }
        private string buildSetPart(List<FragSetPart> sets,string toTableAlias)
        {
            bool isFirst = true;
            StringBuilder sb = new StringBuilder();
            foreach (var item in sets)
            {
                if (!isFirst)
                {
                    sb.Append(",");
                }
                else
                {
                    isFirst = false;
                }
                if (item.field.Contains(".") == false) { 
                    sb.Append(toTableAlias);
                    sb.Append(".");
                }
                sb.Append(item.field);
                sb.Append("=");
                sb.Append(item.value);
                sb.Append(" ");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 当数据库不支持merge into时，衰退为一个update from /insert from 语句组合。
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        protected string buildMergeIntoFallBack(FragMergeInto frag)
        {
            var sb = new StringBuilder();
            var setTableNick = "t_tar";
            if (frag.intoAlias.HasText()) { 
                setTableNick = frag.intoAlias;
            }
            var updateFromPart = "";
            var insertFromPart = "";
            var joinWH = frag.onPart;
            if (joinWH.Contains(frag.intoTable + ".")) { 
                joinWH = joinWH.Replace(frag.intoTable + ".", setTableNick + ".");
            }
            if (frag.usingAlias.HasText())
            {

                updateFromPart = string.Format("{0} AS {1} INNER JOIN  ({2}) as {3} ON {4} ",
                    frag.intoTable, setTableNick, frag.usingTable, frag.usingAlias, joinWH);
                insertFromPart = string.Format("{0} AS {1} Right JOIN  ({2}) as {3} ON {4} ",
                    frag.intoTable, setTableNick, frag.usingTable, frag.usingAlias, joinWH);
            }
            else
            {
                updateFromPart = string.Format("{0} AS {1} INNER JOIN  {2} ON {3} ",
                    frag.intoTable, setTableNick, frag.usingTable, joinWH);
                insertFromPart = string.Format("{0} AS {1} Right JOIN  {2} ON {3} ",
                    frag.intoTable, setTableNick, frag.usingTable, joinWH);
            }
            foreach (var when in frag.mergeWhens) { 
                if (when.action == MergeAction.update && when.setInner!=null && when.setInner.Count>0 )
                {
                    //创建更新部分
                    var fu = new FragSQL();
                    fu.updateTo = setTableNick;
                    fu.setPart = when.setInner;
                    fu.fromInner = updateFromPart;
                    if (!string.IsNullOrWhiteSpace(when.whenWhere)) { 
                        fu.whereInner = when.whenWhere;
                    }
                    sb.Append(buildUpdate(fu));
                    sb.Append(";");
                    sb.AppendLine();
                }            
            }

            foreach (var when in frag.mergeWhens) { 
                if (when.action== MergeAction.insert && !string.IsNullOrWhiteSpace(when.fieldInner))
                {
                    var fi = new FragSQL();

                    fi.insertInto = frag.intoTable;
                    fi.insertCols = when.fieldInner;
                    fi.insertValue = when.valueInner;
                    fi.fromInner = insertFromPart;
                    //需要把join条件解构，拆出目标的那个字段，放到where条件中
                    string field = "";
                    if (!string.IsNullOrWhiteSpace(frag.intoAlias)) {
                        field = this.gotTableField(frag.onPart, frag.intoAlias);
                    }
                    if (string.IsNullOrWhiteSpace(field))
                    {
                        field = this.gotTableField(frag.onPart, frag.intoTable);
                    }

                    if (!string.IsNullOrWhiteSpace(field))
                    {
                        fi.whereInner = string.Format("{0}.{1} IS NULL", setTableNick, field);
                        sb.Append(buildInsert(fi));
                        sb.Append(";");
                        sb.AppendLine();
                    }
                    else {
                        //拆不出来，使用 not exist 处理
                        /*
                         WHERE NOT EXISTS (
        SELECT 1 FROM products_archive pa 
        WHERE pa.product_id = p.product_id
    );
                         */
                    
                    }

    ;
                }               
            }

            

            return sb.ToString();
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
        /// <summary>
        /// 表名的注释定义
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 复制表
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
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
        /// <summary>
        /// 删除索引
        /// </summary>
        /// <param name="indexName"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
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
        /// <summary>
        /// 添加主键
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnName"></param>
        /// <param name="indexName"></param>
        /// <returns></returns>
        public override string AddPrimaryKeyBy(string tableName, string columnName, string indexName)
        {
            return string.Format("ALTER TABLE {0} ADD PRIMARY KEY({2}) /*{1}*/", tableName, indexName, columnName);
        }
        /// <summary>
        /// 添加列
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnName"></param>
        /// <param name="dataType"></param>
        /// <param name="defval"></param>
        /// <param name="nullable"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <returns></returns>
        public override string AddColumnToTableBy(string tableName, string columnName, string dataType, string defval, string nullable, string p2, string p3)
        {
            return string.Format("ALTER TABLE {0} ADD {1} {2}{3} {4} {5} {6}",
                tableName, columnName, dataType,
                defval, nullable, p2, p3
                );
        }
        /// <summary>
        /// 修改列
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnName"></param>
        /// <param name="dataType"></param>
        /// <param name="defval"></param>
        /// <param name="nullable"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <returns></returns>
        public override string AlterColumnToTableby(string tableName, string columnName, string dataType, string defval, string nullable, string p2, string p3)
        { 
            return string.Format("alter table {0} change  column {1} {1} {2}{3} {4} {5} {6}",
                tableName, columnName, dataType,
                defval, nullable, p2, p3
                );
        }
        /// <summary>
        /// 建表
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="detail"></param>
        /// <returns></returns>
        public override string CreateTableBy(string tableName, string detail)
        { 
            return string.Format("CREATE TABLE {0}( {1} $PrimaryKey)", tableName, detail);
        }
        /// <summary>
        /// 建字段
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="dataType"></param>
        /// <param name="defval"></param>
        /// <param name="nullable"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <returns></returns>
        public override string CreateTableColumnBy(string columnName, string dataType, string defval, string nullable, string p2, string p3)
        { 
            return string.Format("{0} {1}{2} {3} {4} {5}",
                columnName, dataType,
                defval, nullable, p2, p3
                );
        }
        //protected override string TruncateTableSql (){ "TRUNCATE TABLE {0}";

        //protected override string DropTableSql (){ "DROP TABLE {0}";
        /// <summary>
        /// 删除列
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public override string DropColumnToTableBy(string tableName, string columnName)
        { 
            return string.Format("ALTER TABLE {0} DROP COLUMN {1}", tableName, columnName);
        }
        /// <summary>
        /// 删除主键
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="constraintName"></param>
        /// <returns></returns>
        public override string DropConstraintBy(string tableName, string constraintName)
        { 
            return string.Format("ALTER TABLE {0} drop primary key;", tableName, constraintName);
        }
        /// <summary>
        /// 重命名列
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="oldName"></param>
        /// <param name="newName"></param>
        /// <returns></returns>
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


        /// <summary>
        /// 修改表注释
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        public override string AddTableCaptionBy (string tableName, string caption)
        { 
            return string.Format("ALTER TABLE {0} COMMENT='{1}';", tableName, caption);
        }
        /// <summary>
        /// 删除表注释
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public override string DeleteTableCaptionBy (string tableName)
        { 
            return string.Format("ALTER TABLE {0} COMMENT='';", tableName);
        }
        /// <summary>
        /// 重命名表
        /// </summary>
        /// <param name="oldTableName"></param>
        /// <param name="newTableName"></param>
        /// <returns></returns>
        public override string RenameTableBy (string oldTableName, string newTableName)
        { 
            return string.Format("alter table {0} rename {1}", oldTableName, newTableName);
        }
        /// <summary>
        /// 建索引
        /// </summary>
        /// <param name="indexName"></param>
        /// <param name="tableName"></param>
        /// <param name="columnName"></param>
        /// <param name="unque"></param>
        /// <returns></returns>
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
