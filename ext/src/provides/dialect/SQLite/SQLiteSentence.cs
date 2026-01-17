// 基础功能说明：

using mooSQL.data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data;
public class SQLiteSentence :SQLSentence
{

    public SQLiteSentence(Dialect dia) : base(dia) { 
    
    }

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


    public override string GetDataBaseSql => "SHOW DATABASES";

    public override string GetColumnInfosByTableNameSql => "SELECT  column_name AS Name," +
        " CASE WHEN  left(COLUMN_TYPE,LOCATE('(',COLUMN_TYPE)-1)='' THEN COLUMN_TYPE ELSE  left(COLUMN_TYPE,LOCATE('(',COLUMN_TYPE)-1) END   AS DataType," +
        "CAST(SUBSTRING(COLUMN_TYPE,LOCATE('(',COLUMN_TYPE)+1,LOCATE(')',COLUMN_TYPE)-LOCATE('(',COLUMN_TYPE)-1) AS signed) AS ColumnLength," +
        " numeric_scale as Scale,column_default  AS  `DefaultValue`,column_comment  AS  `Comment`," +
        "CASE WHEN COLUMN_KEY = 'PRI' THEN true ELSE false END AS `IsPrimary`," +
        " CASE WHEN EXTRA='auto_increment' THEN true ELSE false END as IsIdentity," +
        "CASE WHEN is_nullable = 'YES' THEN true ELSE false END AS `IsNullable` " +
        "FROM Information_schema.columns where TABLE_NAME='{0}' and  TABLE_SCHEMA=(select database()) ORDER BY TABLE_NAME";

    public override string GetTableInfoListSql => "select TABLE_NAME as Name,TABLE_COMMENT as Comment from information_schema.tables" +
        " where  TABLE_SCHEMA=(select database())  AND TABLE_TYPE='BASE TABLE'";

    public override string GetViewInfoListSql => "select TABLE_NAME as Name,TABLE_COMMENT as Description from information_schema.tables " +
        "where  TABLE_SCHEMA=(select database()) AND TABLE_TYPE='VIEW'";




}
