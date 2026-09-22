using mooSQL.data.model;
using mooSQL.linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace mooSQL.data;

/// <summary>
/// 达梦数据字典：Oracle 系 user_*/all_* 视图，IDENTITY_COLUMN 探测。
/// </summary>
public class DMSentence : SQLSentence
{
    public DMSentence(Dialect dia) : base(dia) { }

    private string? SchemasFilter { get; set; }

    public override SQLBuilder getTableColumns(string tableName, SQLBuilder builder)
    {
        builder.from("t", (t) =>
        {
            t.select("co.COLUMN_NAME as columnName, co.DATA_TYPE as columnType, co.CHARACTER_MAXIMUM_LENGTH as columnLen,COLUMN_COMMENT as columnDesc ")
                .from("information_schema.columns co")
                .where("table_name", tableName);
        });
        return builder;
    }

    public override string GetReserveSequenceValuesSql(int count, string sequenceName)
    {
#if NET6_0_OR_GREATER
        return FormattableString.Invariant($"SELECT {dialect.clauseTranslator.TranslateValue(sequenceName, ConvertType.SequenceName)}.nextval ID from DUAL connect by level <= {count}");
#else
        return $"SELECT {dialect.clauseTranslator.TranslateValue(sequenceName, ConvertType.SequenceName)}.nextval ID from DUAL connect by level <= {count}";
#endif
    }

    public override string GetDataBaseSql => throw new NotSupportedException();

    public override string GetColumnInfosByTableNameSql => "select  " +
        " t1.COLUMN_NAME as Name " +
        " ,t1.DATA_TYPE AS DbTypeText " +
        " ,CASE WHEN t1.DATA_TYPE='NUMBER' then NVL(t1.DATA_PRECISION,0) else t1.CHAR_LENGTH end as MaxLength " +
        " ,t1.DATA_SCALE as Scale " +
        " ,case WHEN t1.NULLABLE='Y' then 1 else 0  end as IsNullable " +
        " ,case WHEN t3.keyname is null then 0 else 1 end as IsPrimary " +
        ",t2.COMMENTS as \"Comment\" " +
        " from all_tab_columns  t1 " +
        " LEFT JOIN all_col_comments t2 on t1.Table_Name = t2.table_name and T1.COLUMN_NAME=T2.COLUMN_NAME " +
        "   left join " +
        "(" +
        "  select distinct cu.COLUMN_name KEYNAME  from user_cons_columns cu, user_constraints au  " +
        " where cu.constraint_name = au.constraint_name " +
        " and au.constraint_type = 'P' and au.table_name=upper('{0}') " +
        ") t3  on t3.keyname = t1.COLUMN_NAME " +
        " where t1.table_name=upper('{0}')";

    public override string GetColumnCaptionsByTableNameSql => "SELECT t1.COLUMN_NAME AS Name, t2.COMMENTS AS Caption " +
        "FROM all_tab_columns t1 " +
        "LEFT JOIN all_col_comments t2 ON t1.TABLE_NAME = t2.TABLE_NAME AND t1.COLUMN_NAME = t2.COLUMN_NAME " +
        "WHERE t1.TABLE_NAME = UPPER('{0}') ORDER BY t1.COLUMN_ID";

    public override string GetTableInfoListSql => "SELECT  table_name name from user_tables where " +
        " table_name!='HELP' " +
        "  AND table_name NOT LIKE '%$%' " +
        "  AND table_name NOT LIKE 'LOGMNRC_%' " +
        "  AND table_name!='LOGMNRP_CTAS_PART_MAP' " +
        " AND table_name!='LOGMNR_LOGMNR_BUILDLOG' " +
        " AND table_name!='SQLPLUS_PRODUCT_PROFILE' ";

    public override string GetViewInfoListSql => "select view_name name  from user_views " +
        "WHERE VIEW_name NOT LIKE '%$%' " +
        "AND VIEW_NAME !='PRODUCT_PRIVS' " +
        "AND VIEW_NAME NOT LIKE 'MVIEW_%' ";

    public override bool CreateDatabase(string DatabaseName, string databaseDirectory = null)
        => throw new NotImplementedException();

    public override SQLCmd buildHasTable(string TableName)
    {
        return DBLive.useSQL()
            .from("user_tables")
            .where("table_name", TableName.ToUpperInvariant())
            .toSelect();
    }

    public override bool? IsView(string tabelOrViewName, string dbName = null)
    {
        if (string.IsNullOrWhiteSpace(tabelOrViewName)) return null;
        var name = DBLive.useSQL()
            .select("view_name")
            .from("user_views")
            .where("view_name", tabelOrViewName.ToUpperInvariant())
            .queryRowString(null);
        return !string.IsNullOrEmpty(name);
    }

    public override bool? IsExitsTableCol(string table, string col)
    {
        if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(col)) return null;
        var c = DBLive.useSQL()
            .from("user_tab_columns")
            .where("table_name", table.ToUpperInvariant())
            .where("column_name", col.ToUpperInvariant())
            .count();
        return c > 0;
    }

    public override bool IsExitsTableIndex(string table, string indexName)
    {
        if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(indexName)) return false;
        var c = DBLive.useSQL()
            .from("user_indexes")
            .where("table_name", table.ToUpperInvariant())
            .where("index_name", indexName.ToUpperInvariant())
            .count();
        return c > 0;
    }

    public override string GetTablePKName(string table)
    {
        if (string.IsNullOrWhiteSpace(table)) return string.Empty;
        return DBLive.useSQL()
            .select("constraint_name")
            .from("user_constraints")
            .where("table_name", table.ToUpperInvariant())
            .where("constraint_type", "P")
            .top(1)
            .queryRowString("");
    }

    public override List<ColumnInfo> GetColumns(GetSchemaOptions options)
    {
        const string sql = @"
					SELECT
						(SELECT USER FROM DUAL) || '.' || c.TABLE_NAME as TableID,
						c.COLUMN_NAME                                  as Name,
						c.DATA_TYPE                                    as DataType,
						CASE c.NULLABLE WHEN 'Y' THEN 1 ELSE 0 END     as IsNullable,
						c.COLUMN_ID                                    as Ordinal,
						c.DATA_LENGTH                                  as Length,
						c.CHAR_LENGTH                                  as CharLength,
						c.DATA_PRECISION                               as Precision,
						c.DATA_SCALE                                   as Scale,
						CASE c.IDENTITY_COLUMN WHEN 'YES' THEN 1 ELSE 0 END as IsIdentity,
						cc.COMMENTS                                    as Description
					FROM USER_TAB_COLUMNS c
						JOIN USER_COL_COMMENTS cc ON
							c.TABLE_NAME  = cc.TABLE_NAME AND
							c.COLUMN_NAME = cc.COLUMN_NAME
					";

        return DBLive.ExeQuery(sql, null, rd =>
        {
            var tableId = rd.GetString(0);
            var name = rd.GetString(1);
            var dataType = rd.IsDBNull(2) ? null : rd.GetString(2);
            var isNullable = rd.GetInt32(3) != 0;
            var ordinal = rd.IsDBNull(4) ? 0 : rd.GetInt32(4);
            var dataLength = rd.IsDBNull(5) ? (int?)null : rd.GetInt32(5);
            var charLength = rd.IsDBNull(6) ? (int?)null : rd.GetInt32(6);

            return new ColumnInfo
            {
                TableID = tableId,
                Name = name,
                DataType = dataType,
                IsNullable = isNullable,
                Ordinal = ordinal,
                Precision = rd.IsDBNull(7) ? (int?)null : rd.GetInt32(7),
                Scale = rd.IsDBNull(8) ? (int?)null : rd.GetInt32(8),
                IsIdentity = rd.GetInt32(9) != 0,
                Description = rd.IsDBNull(10) ? null : rd.GetString(10),
                Length = dataType == "CHAR" || dataType == "NCHAR" || dataType == "NVARCHAR2" || dataType == "VARCHAR2" || dataType == "VARCHAR"
                    ? charLength : dataLength
            };
        }).ToList();
    }

    public override bool IsConnectionLost(Exception ex)
    {
        if (ex == null) return false;
        return MatchMessage(ex,
            "connection refused",
            "无法连接",
            "Communication link failure",
            "Network error",
            "broken pipe");
    }
}
