// 基础功能说明：
using System;
using System.Collections.Generic;
using System.Linq;

namespace mooSQL.data;

public class SQLiteSentence : SQLSentence
{
    public SQLiteSentence(Dialect dia) : base(dia)
    {
    }

    public override SQLBuilder getTableColumns(string tableName, SQLBuilder builder)
    {
        builder.ps.Add("Tab", tableName);
        builder.from("t", (t) =>
        {
            t.select("\"name\" as columnName, \"type\" as columnType, 0 as columnLen, '' as columnDesc")
                .from("pragma_table_info(#{Tab})")
                .orderBy("\"cid\"");
        });
        return builder;
    }

    /// <summary>附加库列表（main / temp / ATTACH 名）。</summary>
    public override string GetDataBaseSql => "SELECT name AS NAME FROM pragma_database_list";

    /// <summary>备用模板；正式路径见 <see cref="GetDbColumnsByTableName"/>。notnull 须加引号，否则被解析为关键字。</summary>
    public override string GetColumnInfosByTableNameSql =>
        "SELECT \"cid\" AS Position, \"name\" AS Name, \"type\" AS DbTypeText, \"type\" AS DbTypeTextFull, " +
        "CASE WHEN \"notnull\" = 0 THEN 1 ELSE 0 END AS IsNullable, " +
        "\"dflt_value\" AS DefaultValue, " +
        "CASE WHEN \"pk\" > 0 THEN 1 ELSE 0 END AS IsPrimary, " +
        "0 AS IsIdentity, '' AS Comment " +
        "FROM pragma_table_info('{0}') ORDER BY \"cid\"";

    /// <summary>
    /// SQLite 列元数据：仅 <c>pragma_table_info</c>。
    /// 无列注释；长度/精度从声明类型解析；INTEGER PRIMARY KEY / AUTOINCREMENT 视为自增。
    /// </summary>
    public override List<DbColumnInfo> GetDbColumnsByTableName(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            return new List<DbColumnInfo>();

        var kit = DBLive.useSQL();
        kit.ps.Add("Tab", tableName);
        var tar = kit
            .select(
                "\"cid\" AS Position, " +
                "\"name\" AS Name, " +
                "\"type\" AS DbTypeText, " +
                "\"type\" AS DbTypeTextFull, " +
                "CASE WHEN \"notnull\" = 0 THEN 1 ELSE 0 END AS IsNullable, " +
                "\"dflt_value\" AS DefaultValue, " +
                "CASE WHEN \"pk\" > 0 THEN 1 ELSE 0 END AS IsPrimary, " +
                "0 AS IsIdentity, " +
                "'' AS Comment")
            .from("pragma_table_info(#{Tab})")
            .orderBy("\"cid\"")
            .query<DbColumnInfo>()
            .ToList();

        var tableHasAutoIncrement = TableCreateSqlContainsAutoIncrement(tableName);
        var pkCount = 0;
        for (var i = 0; i < tar.Count; i++)
        {
            if (tar[i].IsPrimary) pkCount++;
        }

        foreach (var item in tar)
        {
            ApplySqliteDeclaredType(item);
            if (!string.IsNullOrWhiteSpace(item.DbTypeText))
            {
                item.FieldType = dialect.mapping.GetDataType(item.DbTypeText, item.DbTypeTextFull);
            }

            // 单列 INTEGER PRIMARY KEY（rowid 别名）或表 DDL 含 AUTOINCREMENT
            if (item.IsPrimary && HasIntegerAffinity(item.DbTypeTextFull ?? item.DbTypeText))
            {
                if (pkCount == 1 || tableHasAutoIncrement)
                    item.IsIdentity = true;
            }
        }

        return tar;
    }

    public override List<DbColumnCaption> GetDbColumnCaptionsByTableName(string tableName)
    {
        var kit = DBLive.useSQL();
        kit.ps.Add("Tab", tableName);
        return kit.select("\"name\" AS Name, '' AS Caption")
            .from("pragma_table_info(#{Tab})")
            .orderBy("\"cid\"")
            .query<DbColumnCaption>()
            .ToList();
    }

    public override string GetTableInfoListSql =>
        "select name as Name, '' as Comment from sqlite_master where type='table' and name not like 'sqlite_%'";

    public override string GetViewInfoListSql =>
        "select name as Name, '' as Comment from sqlite_master where type='view'";

    public override List<DbTableInfo> GetDbTableListAll(GetDbTableListOptions options = null)
    {
        var list = GetDbTableList() ?? new List<DbTableInfo>();
        foreach (var t in list)
        {
            if (string.IsNullOrEmpty(t.Schema))
                t.Schema = "main";
        }
        return FinalizeDbTableListAll(list, options);
    }

    public override List<DbSchemaInfo> GetDbSchemaList(GetDbSchemaListOptions options = null)
    {
        return new List<DbSchemaInfo>
        {
            new DbSchemaInfo { Database = "main", Name = "main", IsDefault = true }
        };
    }

    public override SQLCmd buildHasTable(string TableName)
    {
        var kit = DBLive.useSQL();
        return kit
            .select("count(1)")
            .from("sqlite_master")
            .where("type", "table")
            .where("name", TableName)
            .toSelect();
    }

    public override string GetTablePKName(string table)
    {
        // SQLite 简单主键通常无约束名，返回空；若有命名 CONSTRAINT name PRIMARY KEY 可从 sqlite_master 解析，此处统一返回空
        return string.Empty;
    }

    public override bool? IsView(string tabelOrViewName, string dbName = null)
    {
        if (string.IsNullOrWhiteSpace(tabelOrViewName)) return null;
        var type = DBLive.useSQL()
            .select("type")
            .from("sqlite_master")
            .where("type", "view")
            .where("name", tabelOrViewName)
            .queryRowString(null);
        return !string.IsNullOrEmpty(type);
    }

    public override bool? IsExitsTableCol(string table, string col)
    {
        if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(col)) return null;
        var kit = DBLive.useSQL();
        kit.ps.Add("Tab", table);
        var c = kit.from("pragma_table_info(#{Tab})").where("name", col).count();
        return c > 0;
    }

    public override bool IsExitsTableIndex(string table, string indexName)
    {
        if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(indexName)) return false;
        var kit = DBLive.useSQL();
        kit.ps.Add("Tab", table);
        var row = kit.from("pragma_index_list(#{Tab})").where("name", indexName).queryRow();
        return row != null;
    }

    public override bool IsConnectionLost(Exception ex)
    {
        if (ex == null) return false;
        return MatchMessage(ex,
            "unable to open database file",
            "disk I/O error",
            "database disk image is malformed");
    }

    /// <summary>从声明类型拆出基名与 (len[,scale])；SQLite 无独立长度列。</summary>
    static void ApplySqliteDeclaredType(DbColumnInfo item)
    {
        var raw = (item.DbTypeTextFull ?? item.DbTypeText ?? string.Empty).Trim();
        item.DbTypeTextFull = raw;
        if (string.IsNullOrEmpty(raw))
        {
            item.DbTypeText = string.Empty;
            return;
        }

        var open = raw.IndexOf('(');
        if (open < 0)
        {
            item.DbTypeText = raw;
            return;
        }

        item.DbTypeText = raw.Substring(0, open).Trim();
        var close = raw.IndexOf(')', open + 1);
        if (close <= open) return;

        var inside = raw.Substring(open + 1, close - open - 1);
        var parts = inside.Split(',');
        if (parts.Length >= 1 && int.TryParse(parts[0].Trim(), out var first))
        {
            item.MaxLength = first;
            item.Precision = first;
        }
        if (parts.Length >= 2 && int.TryParse(parts[1].Trim(), out var scale))
            item.Scale = scale;
    }

    /// <summary>SQLite 类型亲和：声明名含 INT 则为 INTEGER 亲和（含 BIGINT 等）。</summary>
    static bool HasIntegerAffinity(string declaredType)
    {
        if (string.IsNullOrWhiteSpace(declaredType)) return false;
        return declaredType.IndexOf("INT", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    bool TableCreateSqlContainsAutoIncrement(string tableName)
    {
        try
        {
            var createSql = DBLive.useSQL()
                .select("sql")
                .from("sqlite_master")
                .where("type", "table")
                .where("name", tableName)
                .queryRowString(null);
            return !string.IsNullOrEmpty(createSql)
                   && createSql.IndexOf("AUTOINCREMENT", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        catch
        {
            return false;
        }
    }
}
