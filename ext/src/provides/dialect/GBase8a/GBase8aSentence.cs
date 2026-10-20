// 基础功能说明：
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data;
/// <summary>
/// 注意！此处方言SQL模版尚未进行测试，只是复制了mysql的内容！！！！
/// </summary>
public class GBase8aSentence :SQLSentence
{

    public GBase8aSentence(Dialect dia) : base(dia) { 
    
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

    public override string GetColumnInfosByTableNameSql => "SELECT                                    \r\n                                    column_name AS ColumnName,\r\n                                    CASE WHEN  left(COLUMN_TYPE,LOCATE('(',COLUMN_TYPE)-1)='' THEN COLUMN_TYPE ELSE  left(COLUMN_TYPE,LOCATE('(',COLUMN_TYPE)-1) END   AS DataType,\r\n                                    CAST(SUBSTRING(COLUMN_TYPE,LOCATE('(',COLUMN_TYPE)+1,LOCATE(')',COLUMN_TYPE)-LOCATE('(',COLUMN_TYPE)-1) AS signed) AS ColumnLength,\r\n                                    numeric_scale as DecimalDigits,\r\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\tcolumn_default  AS  `DefaultValue`,\r\n                                    column_comment  AS  `ColumnDescription`,\r\n                                    CASE WHEN COLUMN_KEY = 'PRI'\r\n                                    THEN true ELSE false END AS `IsPrimaryKey`,\r\n                                    CASE WHEN EXTRA='auto_increment' THEN true ELSE false END as IsIdentity,\r\n                                    CASE WHEN is_nullable = 'YES'\r\n                                    THEN true ELSE false END AS `IsNullable`\r\n                                    FROM\r\n                                    Information_schema.columns where TABLE_NAME='{0}' and  TABLE_SCHEMA=(select database()) ORDER BY TABLE_NAME";

    public override string GetColumnCaptionsByTableNameSql => "SELECT column_name AS Name, column_comment AS Caption " +
        "FROM information_schema.columns WHERE TABLE_NAME='{0}' AND TABLE_SCHEMA=(select database()) ORDER BY ORDINAL_POSITION";

    public override string GetTableInfoListSql => "select TABLE_NAME as Name,TABLE_COMMENT as Description from information_schema.tables\r\n                         where  TABLE_SCHEMA=(select database())  AND TABLE_TYPE='BASE TABLE'";

    public override List<DbTableInfo> GetDbTableListAll(GetDbTableListOptions options = null)
    {
        if (options != null && options.CurrentDatabaseOnly && string.IsNullOrEmpty(options.CurrentDatabaseName))
            options.CurrentDatabaseName = GetCurrentDatabaseName();

        var kit = DBLive.useSQL()
            .select("TABLE_SCHEMA as Schema, TABLE_NAME as Name, TABLE_COMMENT as Comment, TABLE_ROWS as RowCount")
            .from("information_schema.tables")
            .where("TABLE_TYPE", "BASE TABLE")
            .where("TABLE_SCHEMA not in ('information_schema','mysql','performance_schema','sys','gbase')");

        options?.ApplyToBuilder(kit, new DbTableListSqlColumns
        {
            Name = "TABLE_NAME",
            Comment = "TABLE_COMMENT",
            RowCount = "TABLE_ROWS",
            Schema = "TABLE_SCHEMA",
            Database = "TABLE_SCHEMA",
            DefaultOrderBy = "TABLE_SCHEMA, TABLE_NAME",
        });
        if (options == null)
            kit.orderBy("TABLE_SCHEMA, TABLE_NAME");

        return FinalizeDbTableListAll(kit.query<DbTableInfo>().ToList(), options);
    }

    protected override string GetCurrentDatabaseName()
    {
        try { return DBLive.ExeQueryScalar<string>("SELECT DATABASE()", null); }
        catch { return null; }
    }

    public override List<DbSchemaInfo> GetDbSchemaList(GetDbSchemaListOptions options = null)
    {
        EnsureSchemaListCurrentDatabase(options);
        var current = GetCurrentDatabaseName();
        var kit = DBLive.useSQL()
            .select("SCHEMA_NAME as Name, SCHEMA_NAME as Database")
            .from("information_schema.schemata")
            .where("SCHEMA_NAME not in ('information_schema','mysql','performance_schema','sys','gbase')");
        if (options != null && options.HasDatabaseScope && !string.IsNullOrEmpty(options.EffectiveDatabaseName))
            kit.where("SCHEMA_NAME", options.EffectiveDatabaseName);
        kit.orderBy("SCHEMA_NAME");
        var list = kit.query<DbSchemaInfo>().ToList();
        foreach (var s in list)
            s.IsDefault = !string.IsNullOrEmpty(current)
                && string.Equals(s.Name, current, StringComparison.OrdinalIgnoreCase);
        return list;
    }

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
        var c = DBLive.useSQL()
            .from("information_schema.statistics")
            .where("table_schema = DATABASE()")
            .where("table_name", table)
            .where("index_name", indexName)
            .count();
        return c > 0;
    }

    public override string GetViewInfoListSql => "select TABLE_NAME as Name,TABLE_COMMENT as Description from information_schema.tables\r\n                         where  TABLE_SCHEMA=(select database()) AND TABLE_TYPE='VIEW'\r\n                         ";

    public override string GetTablePKName(string table)
    {
        return DBLive.useSQL()
            .select("k.CONSTRAINT_NAME")
            .from("INFORMATION_SCHEMA.KEY_COLUMN_USAGE k")
            .innerJoin("INFORMATION_SCHEMA.TABLE_CONSTRAINTS c ON c.CONSTRAINT_CATALOG = k.CONSTRAINT_CATALOG AND c.CONSTRAINT_SCHEMA = k.CONSTRAINT_SCHEMA AND c.CONSTRAINT_NAME = k.CONSTRAINT_NAME AND c.TABLE_NAME = k.TABLE_NAME AND c.CONSTRAINT_TYPE='PRIMARY KEY'")
            .where("k.TABLE_NAME", table)
            .where("k.TABLE_SCHEMA = DATABASE()")
            .top(1)
            .queryRowString("");
    }

    public override bool IsConnectionLost(Exception ex)
    {
        if (ex == null) return false;
        if (MatchInnerErrorNumber(ex, 2002, 2003, 2006, 2013, 1045)) return true;
        return MatchMessage(ex, "server has gone away", "Lost connection");
    }

}
