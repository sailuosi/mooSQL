// 基础功能说明：


using mooSQL.data;
using mooSQL.linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data;
public class OscarSentence : SQLSentence
{
    public OscarSentence(Dialect dia) : base(dia) { }
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

    public override string GetReserveSequenceValuesSql(int count, string sequenceName)
    {
#if NET6_0_OR_GREATER
        return FormattableString.Invariant($"SELECT {dialect.clauseTranslator.TranslateValue(sequenceName, ConvertType.SequenceName)}.nextval ID from DUAL connect by level <= {count}");
#else
        return $"SELECT {dialect.clauseTranslator.TranslateValue(sequenceName, ConvertType.SequenceName)}.nextval ID from DUAL connect by level <= {count}";
#endif

    }



    public override string GetDataBaseSql
    {
        get
        {
            throw new NotSupportedException();
        }
    }

    public override string GetColumnInfosByTableNameSql => "select  " +
        "  t1.COLUMN_NAME as Name " +
        " ,t1.DATA_TYPE AS DataType " +
        " ,CASE WHEN t1.DATA_TYPE='NUMBER' then NVL(t1.DATA_PRECISION,0) else t1.CHAR_LENGTH end as ColumnLength " +
        " ,t1.DATA_SCALE as Scale " +
        ",case WHEN t1.NULLABLE='Y' then 1 else 0  end as IsNullable " +
        " ,case WHEN t3.keyname is null then 0 else 1 end as IsPrimary " +
        " ,t2.COMMENTS as Comment " +
        " from all_tab_columns  t1 " +
        " LEFT JOIN user_col_comments t2 on t1.Table_Name = t2.table_name and T1.COLUMN_NAME=T2.COLUMN_NAME " +
        " left join (" +
        "    select distinct cu.COLUMN_name KEYNAME " +
        "    from user_cons_columns cu, user_constraints au  " +
        "    where cu.constraint_name = au.constraint_name " +
        "     and au.constraint_type = 'P' and au.table_name=upper('{0}') " +
        " ) t3  on t3.keyname = t1.COLUMN_NAME " +
        " where t1.table_name=upper('{0}')";

    public override string GetTableInfoListSql => "SELECT  table_name name from user_tables where\r\n                        table_name!='HELP' \r\n                        AND table_name NOT LIKE '%$%'\r\n                        AND table_name NOT LIKE 'LOGMNRC_%'\r\n                        AND table_name!='LOGMNRP_CTAS_PART_MAP'\r\n                        AND table_name!='LOGMNR_LOGMNR_BUILDLOG'\r\n                        AND table_name!='SQLPLUS_PRODUCT_PROFILE'  \r\n                         ";

    public override string GetViewInfoListSql => "select view_name name  from user_views \r\n                                                WHERE VIEW_name NOT LIKE '%$%'\r\n                                                AND VIEW_NAME !='PRODUCT_PRIVS'\r\n                        AND VIEW_NAME NOT LIKE 'MVIEW_%' ";






    public override bool CreateDatabase(string DatabaseName, string databaseDirectory = null)
    {
        throw new NotImplementedException();
    }

    public override bool? IsView(string tabelOrViewName, string dbName = null)
    {
        if (string.IsNullOrWhiteSpace(tabelOrViewName)) return null;
        var relkind = DBLive.useSQL()
            .select("relkind")
            .from("pg_class")
            .where("relname", tabelOrViewName.ToLowerInvariant())
            .queryRowString(null);
        if (string.IsNullOrEmpty(relkind)) return false;
        return relkind == "v";
    }

    public override bool? IsExitsTableCol(string table, string col)
    {
        if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(col)) return null;
        var c = DBLive.useSQL()
            .from("information_schema.columns")
            .where("table_schema = CURRENT_SCHEMA()")
            .where("table_name", table.ToLowerInvariant())
            .where("column_name", col.ToLowerInvariant())
            .count();
        return c > 0;
    }

    public override bool IsExitsTableIndex(string table, string indexName)
    {
        if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(indexName)) return false;
        var c = DBLive.useSQL()
            .from("pg_indexes")
            .where("schemaname = current_schema()")
            .where("tablename", table.ToLowerInvariant())
            .where("indexname", indexName.ToLowerInvariant())
            .count();
        return c > 0;
    }

    public override string GetTablePKName(string table)
    {
        if (string.IsNullOrWhiteSpace(table)) return string.Empty;
        return DBLive.useSQL()
            .select("pg_constraint.conname")
            .from("pg_constraint")
            .innerJoin("pg_class ON pg_constraint.conrelid = pg_class.oid")
            .where("pg_class.relname", table.ToLowerInvariant())
            .where("pg_constraint.contype", "p")
            .top(1)
            .queryRowString("");
    }
}
