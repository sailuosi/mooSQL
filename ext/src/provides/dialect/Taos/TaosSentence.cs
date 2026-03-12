using mooSQL.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 注意！此处方言SQL模版尚未进行测试，只是复制了mysql的内容！！！！
    /// </summary>
    public class TaosSentence : SQLSentence
    {

        public TaosSentence(Dialect dia) : base(dia) { 
        
        }
        /// <inheritdoc/>
        public override SQLBuilder getTableColumns(string tableName, SQLBuilder builder)
        {
            //sql = "SELECT COLUMN_NAME FName,DATA_TYPE FType,CHARACTER_MAXIMUM_LENGTH FLen,COLUMN_COMMENT FDesc FROM information_schema.columns WHERE table_name='
            builder.from("t", (t) =>
            {
                t.select("co.COLUMN_NAME as columnName, co.DATA_TYPE as columnType, co.CHARACTER_MAXIMUM_LENGTH as columnLen,COLUMN_COMMENT as columnDesc ")
                    .from("information_schema.columns co")
                    .where("table_name", tableName);
            });

            return builder;
        }
        /// <inheritdoc/>
        public override string GetDataBaseSql => "SHOW DATABASES";
        /// <inheritdoc/>
        public override string GetColumnInfosByTableNameSql => "SELECT  column_name AS Name, CASE WHEN  left(COLUMN_TYPE,LOCATE('(',COLUMN_TYPE)-1)='' THEN COLUMN_TYPE ELSE  left(COLUMN_TYPE,LOCATE('(',COLUMN_TYPE)-1) END   AS DataType, CAST(SUBSTRING(COLUMN_TYPE,LOCATE('(',COLUMN_TYPE)+1,LOCATE(')',COLUMN_TYPE)-LOCATE('(',COLUMN_TYPE)-1) AS signed) AS ColumnLength,  numeric_scale as Scale, column_default  AS  `DefaultValue`,   column_comment  AS  `Comment`,   CASE WHEN COLUMN_KEY = 'PRI'   THEN true ELSE false END AS `IsPrimary`,    CASE WHEN EXTRA='auto_increment' THEN true ELSE false END as IsIdentity,  CASE WHEN is_nullable = 'YES'   THEN true ELSE false END AS `IsNullable`   FROM  Information_schema.columns where TABLE_NAME='{0}' and  TABLE_SCHEMA=(select database()) ORDER BY TABLE_NAME";
        /// <inheritdoc/>
        public override string GetTableInfoListSql => "select TABLE_NAME as Name,TABLE_COMMENT as Comment from information_schema.tables\r\n                         where  TABLE_SCHEMA=(select database())  AND TABLE_TYPE='BASE TABLE'";
        /// <inheritdoc/>
        public override string GetViewInfoListSql => "select TABLE_NAME as Name,TABLE_COMMENT as Description from information_schema.tables\r\n                         where  TABLE_SCHEMA=(select database()) AND TABLE_TYPE='VIEW'\r\n                         ";

        public override bool? IsView(string tabelOrViewName, string dbName = null)
        {
            if (string.IsNullOrWhiteSpace(tabelOrViewName)) return null;
            var kit = DBLive.useSQL()
                .select("TABLE_TYPE")
                .from("information_schema.TABLES")
                .where("table_name", tabelOrViewName);
            if (!string.IsNullOrWhiteSpace(dbName))
                kit = kit.where("table_schema", dbName);
            else
                kit = kit.where("table_schema = DATABASE()");
            var t = kit.queryRowString(null);
            if (string.IsNullOrEmpty(t)) return false;
            return string.Equals(t, "VIEW", StringComparison.OrdinalIgnoreCase);
        }

        public override bool? IsExitsTableCol(string table, string col)
        {
            if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(col)) return null;
            var c = DBLive.useSQL()
                .from("information_schema.columns")
                .where("table_schema = DATABASE()")
                .where("table_name", table)
                .where("column_name", col)
                .count();
            return c > 0;
        }

        public override bool IsExitsTableIndex(string table, string indexName)
        {
            if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(indexName)) return false;
            // TDengine indexes metadata: information_schema.INS_INDEXES
            var c = DBLive.useSQL()
                .from("information_schema.INS_INDEXES")
                .where("table_name", table)
                .where("index_name", indexName)
                .count();
            return c > 0;
        }
    }
}
