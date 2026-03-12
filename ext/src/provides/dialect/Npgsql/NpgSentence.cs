
using mooSQL.linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public class NpgSentence:SQLSentence
    {
        public NpgSentence(Dialect dialect):base(dialect) { 
        
        }
        public override string GetReserveSequenceValuesSql(int count, string sequenceName)
        {
#if NET6_0_OR_GREATER
            return FormattableString.Invariant($"SELECT nextval('{dialect.clauseTranslator.TranslateValue(sequenceName, ConvertType.SequenceName)}') FROM generate_series(1, {count})");
#else
            return $"SELECT nextval('{dialect.clauseTranslator.TranslateValue(sequenceName, ConvertType.SequenceName)}') FROM generate_series(1, {count})";
#endif

        }



        public override string GetDataBaseSql => "SELECT datname FROM pg_database";

        public override string GetColumnInfosByTableNameSql => "select pcolumn.column_name as Name,pcolumn.udt_name as DataType," +
            "   case when pcolumn.udt_name='numeric' then pcolumn.numeric_precision " +
            "   else pcolumn.character_maximum_length end as ColumnLength, " +
            "   case when pcolumn.udt_name='numeric' then pcolumn.numeric_scale " +
            "    else null end as Scale, " +
            "  pcolumn.column_default as DefaultValue," +
            " col_description(pclass.oid, pcolumn.ordinal_position) as Comment," +
            " case when pkey.colname = pcolumn.column_name " +
            " then true else false end as IsPrimary, " +
            " case when pcolumn.column_default like 'nextval%' " +
            " then true else false end as IsIdentity, " +
            " case when pcolumn.is_nullable = 'YES' " +
            " then true else false end as IsNullable " +
            " from (select * from pg_tables where tablename = '{0}' and schemaname='public') ptables inner join pg_class pclass " +
            "  on ptables.tablename = pclass.relname inner join (SELECT * " +
            " FROM information_schema.columns  ) pcolumn on pcolumn.table_name = ptables.tablename " +
            " left join ( select  pg_class.relname,pg_attribute.attname as colname from " +
            "  pg_constraint  inner join pg_class    on pg_constraint.conrelid = pg_class.oid " +
            " inner join pg_attribute on pg_attribute.attrelid = pg_class.oid " +
            "  and  pg_attribute.attnum = pg_constraint.conkey[1] " +
            " inner join pg_type on pg_type.oid = pg_attribute.atttypid " +
            " where pg_constraint.contype='p'  ) pkey on pcolumn.table_name = pkey.relname " +
            " order by ptables.tablename";

        public override string GetTableInfoListSql => "select cast(relname as varchar) as Name,   cast(obj_description(relfilenode,'pg_class') as varchar) as Comment from pg_class c   where  relkind = 'r' and relname not like 'pg_%' and relname not like 'sql_%' order by relname";

        public override string GetViewInfoListSql => "select cast(relname as varchar) as Name,cast(Description as varchar) from pg_description   join pg_class on pg_description.objoid = pg_class.oid  where objsubid = 0 and relname in (SELECT viewname from pg_views   WHERE schemaname ='public')";


        public override SQLCmd buildHasTable(string TableName)
        {
            var cmd = DBLive.useSQL()
                .from("information_schema.tables")
                .where("table_schema = CURRENT_SCHEMA()")
                .where("table_name", TableName)
                .toSelectCount();
            return cmd;
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
}
