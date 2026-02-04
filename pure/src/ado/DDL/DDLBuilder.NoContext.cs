using mooSQL.data.builder;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 
    /// </summary>
    public partial class DDLBuilder
    {
        /// <summary>
        /// 复制表结构
        /// </summary>
        /// <param name="srcTableName"></param>
        /// <param name="tarTableName"></param>
        /// <returns></returns>
        public SQLCmd toCopyTableScheme(string srcTableName,string targetTableName)
        {
            var frag = new DDLFragSQL()
            {
                Table = targetTableName,
                SrcTable = srcTableName,
            };
            var sql=DBLive.dialect.expression.buildCopyTableSchema(frag);
            var cmd= new SQLCmd(sql);
            return cmd;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="srcTableName"></param>
        /// <param name="targetTableName"></param>
        /// <returns></returns>
        public int doCopyTableScheme(string srcTableName, string targetTableName)
        {
            var cmd = this.toCopyTableScheme(srcTableName,targetTableName);
            var cc = DBLive.ExeNonQuery(cmd);
            return cc;
        }

        public SQLCmd toCopyTable(string srcTableName, string targetTableName)
        {
            var frag = new DDLFragSQL()
            {
                Table = targetTableName,
                SrcTable = srcTableName,
            };
            var sql = DBLive.dialect.expression.buildCopyTable(frag);
            var cmd = new SQLCmd(sql);
            return cmd;
        }
        /// <summary>
        /// 复制表，包含结构和数据
        /// </summary>
        /// <param name="srcTableName"></param>
        /// <param name="tarTableName"></param>
        /// <returns></returns>
        public int doCopyTable(string srcTableName, string targetTableName)
        {
            var cmd = this.toCopyTable(srcTableName, targetTableName);
            var cc = DBLive.ExeNonQuery(cmd);
            return cc;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public SQLCmd toTruncateTable(string tableName) {
            var frag = new DDLFragSQL()
            {
                Table = tableName
            };
            var sql = DBLive.dialect.expression.buildTruncateTable(frag);
            var cmd = new SQLCmd(sql);
            return cmd;
        }
        /// <summary>
        /// 废弃表
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public int doTruncateTable(string tableName)
        {
            var cmd = this.toTruncateTable(tableName);
            var cc = DBLive.ExeNonQuery(cmd);
            return cc;
        }



        public int doDropIndex(string srcTableName) {
            return 0;
        }

        public int doDropProcedure(string srcTableName) {
            return 0;
        }
        /// <summary>
        /// 无上下文影响的直接删除某表某字段
        /// </summary>
        /// <param name="table"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public SQLCmd toDropColumn(string table, string column) {
            var frag = new DDLFragSQL()
            {
                Table = table,
                Columns = new List<DDLField>() {
                    new DDLField(){
                        FieldName=column
                    }
                }
            };
            var str = this.DBLive.dialect.expression.buildDropColumn(frag);
            var cmd = new SQLCmd(str, ps);
            return cmd;
        }
        /// <summary>
        /// 快捷的列删除
        /// </summary>
        /// <param name="table"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public int doDropColumn(string table, string column)
        {
            var cmd = this.toDropColumn(table, column);
            return DBLive.ExeNonQuery(cmd);
        }

        /// <summary>
        /// 获取重命名SQL
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="oldColumnName"></param>
        /// <param name="newColumnName"></param>
        /// <returns></returns>
        public SQLCmd toRenameColumn(string tableName, string oldColumnName, string newColumnName)
        {
            tableName =SQLPit.wrapTable(tableName);
            oldColumnName = SQLPit.wrapField(oldColumnName);
            newColumnName = SQLPit.wrapField(newColumnName);
            var sql= SQLPit.RenameColumnBy(tableName, oldColumnName, newColumnName);
            return new SQLCmd(sql);
        }
        /// <summary>
        /// 字段重命名
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="oldColumnName"></param>
        /// <param name="newColumnName"></param>
        /// <returns></returns>
        public int doRenameColumn(string tableName, string oldColumnName, string newColumnName)
        {
            var cmd = toRenameColumn(tableName, oldColumnName, newColumnName);
            return DBLive.ExeNonQuery(cmd);
        }
        /// <summary>
        /// 是否有字段的备注
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public bool hasColumnCaption(string columnName, string tableName)
        {
            string sQL = SQLPit.IsAnyColumnCaptionBy(columnName, tableName);
            DataTable dataTable = DBLive.ExeQuery(sQL);
            return dataTable.Rows != null && dataTable.Rows.Count > 0;
        }

        public SQLCmd toDropConstraint(string tableName, string constraintName)
        {
            tableName = SQLPit.wrapTable(tableName);
            var sql= SQLPit.DropConstraintBy(tableName, constraintName);
            return new SQLCmd(sql);
        }
        /// <summary>
        /// 删除约束
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="constraintName"></param>
        /// <returns></returns>
        public int doDropConstraint(string tableName, string constraintName)
        {
            var cmd= toDropConstraint(tableName, constraintName);
            return DBLive.ExeNonQuery(cmd);
        }

        /// <summary>
        /// 重命名表
        /// </summary>
        /// <param name="oldTableName"></param>
        /// <param name="newTableName"></param>
        /// <returns></returns>
        public SQLCmd toRenameTable(string oldTableName, string newTableName)
        {
            var sql= SQLPit.RenameTableBy(oldTableName, newTableName);
            return new SQLCmd(sql);
        }
        /// <summary>
        /// 重命名表
        /// </summary>
        /// <param name="oldTableName"></param>
        /// <param name="newTableName"></param>
        /// <returns></returns>
        public int doRenameTable(string oldTableName, string newTableName)
        {
            return DBLive.ExeNonQuery(toRenameTable(oldTableName, newTableName));
        }

        /// <summary>
        /// 删除表注释
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public SQLCmd toDeleteTableCaption(string tableName)
        {
            return new SQLCmd( SQLPit.DeleteTableCaptionBy(tableName));
        }
        /// <summary>
        /// 删除表注释
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public int doDeleteTableCaption(string tableName)
        {
            var cc = DBLive.ExeNonQuery(toDeleteTableCaption(tableName));
            return cc;
        }
    }
}
