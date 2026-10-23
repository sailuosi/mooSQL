
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
    public class GBase8aExpress:MySQLExpress
    {
        /// <summary>
        /// GBase8aExpress type.
        /// </summary>
        /// <summary>
        /// GBase8aExpress type.
        /// </summary>
        public GBase8aExpress(Dialect dia) : base(dia) {
            _paraPrefix = "?";
            _selectAutoIncrement = "SELECT Last_Insert_Id()";
            _provideType = "MySql.Data.MySqlClient.MySqlClientFactory,MySql.Data";
        }
/// <summary>
/// Quotes an identifier for this dialect.
/// </summary>
 

        public override string wrapKeyword(string value)
        {
            if (value.StartsWith("\"") && value.EndsWith("\"")) { 
                return value;
            }
            return "\"" + value + "\"";
        }

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
            sb.Append(" ");
            sb.Append("FROM ");
            sb.Append(frag.fromInner);
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

            AppendLimitOffset(sb, frag);

            return sb.ToString();
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


        #region DDL
/// <summary>
/// Returns SQL for retrieving an auto-generated identity value.
/// </summary>
 


        public override string getTableAutoIdSQL()
        {
            return "AUTO_INCREMENT";
        }
/// <summary>
/// Builds a CREATE DATABASE statement.
/// </summary>
 

        public override string CreateDataBaseBy(string database)
        {
            return string.Format("CREATE DATABASE {0} CHARACTER SET utf8 COLLATE utf8_general_ci ", database); 
        }
 /// <summary>
 /// Builds an ADD PRIMARY KEY statement.
 /// </summary>
 
        public override string AddPrimaryKeyBy(string tableName, string columnName, string indexName) { 
            return string.Format("ALTER TABLE {0} ADD PRIMARY KEY({2}) /*{1}*/"
                , tableName, indexName, columnName);
        }
 /// <summary>
 /// Builds an ADD COLUMN statement.
 /// </summary>
 
        public override string AddColumnToTableBy(string tableName, string columnName, string dataType, string defval, string nullable, string p2, string p3)
        {
            return string.Format("ALTER TABLE {0} ADD {1} {2}{3} {4} {5} {6}",
                tableName, columnName, dataType,
                defval, nullable, p2, p3
                );
        }
 /// <summary>
 /// Builds an ALTER COLUMN statement.
 /// </summary>
 
        public override string AlterColumnToTableby(string tableName, string columnName, string dataType, string defval, string nullable, string p2, string p3)
        { 
            return string.Format("ALTER TABLE {0} CHANGE COLUMN {1} {1} {2}{3} {4} {5} {6}",
                tableName, columnName, dataType,
                defval, nullable, p2, p3
                );
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
            return string.Format("ALTER TABLE {0} DROP PRIMARY KEY", tableName, constraintName);
        }
 /// <summary>
 /// Builds a RENAME COLUMN statement.
 /// </summary>
 
        public override string RenameColumnBy(string tableName, string oldName, string newName)
        { 
            return string.Format("ALTER TABLE {0} CHANGE COLUMN {1} {2}", tableName, oldName, newName);
        }
 /// <summary>
 /// Builds SQL to check system-table permissions.
 /// </summary>
 
        public override string CheckSystemTablePermissionsBy(){ 
            return "SELECT 1 FROM Information_schema.columns LIMIT 0,1";
        }
 /// <summary>
 /// Returns the NULL column qualifier for CREATE TABLE.
 /// </summary>
 
        public override string CreateTableNullBy(){ return "NULL";
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
/// <summary>
/// Builds SQL to add a table caption/comment.
/// </summary>
 



        public override string AddTableCaptionBy(string tableName, string caption)
        { 
            return string.Format("ALTER TABLE {0} COMMENT='{1}';", tableName, caption);
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
            return string.Format("ALTER TABLE {0} COMMENT=''", tableName);
        }
/// <summary>
/// Builds a RENAME TABLE statement.
/// </summary>
 


        public override string RenameTableBy(string oldTableName, string newTableName)
        { 
            return string.Format("ALTER TABLE {0} RENAME {1}", oldTableName, newTableName);
        }
 /// <summary>
 /// Builds a CREATE INDEX statement.
 /// </summary>
 
        public override string CreateIndexBy(string indexName, string tableName, string columnName, string unique)
        { 
            return string.Format("CREATE {3} INDEX Index_{0}_{2} ON {0} ({1})", tableName, columnName, indexName,unique);
        }
 /// <summary>
 /// Builds SQL that checks whether an index exists.
 /// </summary>
 
        public override string IsAnyIndexBy(string indexName)
        { 
            return string.Format("SELECT COUNT(*) FROM information_schema.statistics WHERE index_name = '{0}'", indexName);
        }
        #endregion
    }
}
