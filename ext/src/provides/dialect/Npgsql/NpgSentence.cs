
using mooSQL.linq;
using mooSQL.utils;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
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

        public override string GetColumnCaptionsByTableNameSql => "SELECT c.column_name AS Name, col_description(pclass.oid, c.ordinal_position) AS Caption " +
            "FROM information_schema.columns c " +
            "INNER JOIN pg_class pclass ON pclass.relname = c.table_name " +
            "WHERE c.table_name = '{0}' AND c.table_schema = current_schema() " +
            "ORDER BY c.ordinal_position";

        public override string GetTableInfoListSql =>
            "select cast(c.relname as varchar) as Name, cast(obj_description(c.oid, 'pg_class') as varchar) as Comment " +
            "from pg_class c join pg_namespace n on n.oid = c.relnamespace " +
            "where c.relkind = 'r' and n.nspname = current_schema() " +
            "and c.relname not like 'pg_%' and c.relname not like 'sql_%' " +
            "order by c.relname";
        public override List<DbTableInfo> GetDbTableList()
        {
            var dt = this.DBLive.ExeQuery<DbTableInfo>(GetTableInfoListSql);
            return dt.ToList();
        }

        /// <summary>
        /// 多连接逐库枚举用户表；Schema = {datname}.{nspname}。单库失败跳过。
        /// </summary>
        public override List<DbTableInfo> GetDbTableListAll(GetDbTableListOptions options = null)
        {
            var result = new List<DbTableInfo>();
            if (string.IsNullOrWhiteSpace(DBLive.config?.DBConnectStr))
                return FinalizeDbTableListAll(result, options);

            if (options != null && options.CurrentDatabaseOnly && string.IsNullOrEmpty(options.CurrentDatabaseName))
                options.CurrentDatabaseName = GetCurrentDatabaseName();

            List<string> dbs;
            try
            {
                dbs = DBLive.ExeQuery(
                        "SELECT datname AS NAME FROM pg_database WHERE datallowconn AND datname NOT IN ('template0','template1') ORDER BY datname")
                    .getFieldValues("NAME");
            }
            catch
            {
                dbs = (GetDataBaseList() ?? new List<string>())
                    .Where(d => !string.Equals(d, "template0", StringComparison.OrdinalIgnoreCase)
                                && !string.Equals(d, "template1", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var cols = new DbTableListSqlColumns
            {
                Name = "c.relname",
                Comment = "obj_description(c.oid, 'pg_class')",
                RowCount = "c.reltuples",
                Schema = "n.nspname",
            };

            foreach (var datname in dbs)
            {
                if (string.IsNullOrWhiteSpace(datname)) continue;
                if (options != null && !options.ShouldIncludeDatabase(datname)) continue;
                if (options != null && options.HasDatabaseScope)
                    options.DatabaseFilterPushed = true;

                var sql = new StringBuilder();
                sql.Append(
                    "select cast(n.nspname as varchar) as nsp, cast(c.relname as varchar) as Name, " +
                    "cast(obj_description(c.oid, 'pg_class') as varchar) as Comment, " +
                    "cast(c.reltuples as bigint) as RowCount " +
                    "from pg_class c join pg_namespace n on n.oid = c.relnamespace " +
                    "where c.relkind = 'r' " +
                    "and n.nspname not in ('pg_catalog','information_schema') " +
                    "and n.nspname not like 'pg_toast%' " +
                    "and n.nspname not like 'pg_temp_%' " +
                    "and c.relname not like 'pg_%' and c.relname not like 'sql_%'");
                options?.ApplyFiltersToWhereSql(sql, cols, useILike: true);
                sql.Append(" order by n.nspname, c.relname");

                try
                {
                    var cs = BuildConnectionStringForDatabase(datname);
                    using (var conn = CreateSideConnection(cs))
                    {
                        if (conn.State != ConnectionState.Open)
                            conn.Open();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = sql.ToString();
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    var nsp = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                    var name = reader.IsDBNull(1) ? null : reader.GetString(1);
                                    var comment = reader.IsDBNull(2) ? null : reader.GetString(2);
                                    long? rowCount = reader.IsDBNull(3) ? (long?)null : Convert.ToInt64(reader.GetValue(3));
                                    if (string.IsNullOrEmpty(name)) continue;
                                    result.Add(new DbTableInfo
                                    {
                                        Schema = string.IsNullOrEmpty(nsp) ? datname : datname + "." + nsp,
                                        Name = name,
                                        Comment = comment,
                                        RowCount = rowCount
                                    });
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // 跳过无权限或不可连接的库
                }
            }
            return FinalizeDbTableListAll(result, options);
        }

        protected override string GetCurrentDatabaseName()
        {
            try { return DBLive.ExeQueryScalar<string>("SELECT current_database()", null); }
            catch { return null; }
        }

        public override List<DbSchemaInfo> GetDbSchemaList(GetDbSchemaListOptions options = null)
        {
            EnsureSchemaListCurrentDatabase(options);
            var result = new List<DbSchemaInfo>();
            if (string.IsNullOrWhiteSpace(DBLive.config?.DBConnectStr))
                return result;

            List<string> dbs;
            try
            {
                dbs = DBLive.ExeQuery(
                        "SELECT datname AS NAME FROM pg_database WHERE datallowconn AND datname NOT IN ('template0','template1') ORDER BY datname")
                    .getFieldValues("NAME");
            }
            catch
            {
                dbs = (GetDataBaseList() ?? new List<string>())
                    .Where(d => !string.Equals(d, "template0", StringComparison.OrdinalIgnoreCase)
                                && !string.Equals(d, "template1", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            const string schemaSql =
                "select cast(n.nspname as varchar) as Name, " +
                "case when n.nspname = 'public' then true else false end as IsDefault " +
                "from pg_namespace n " +
                "where n.nspname not in ('pg_catalog','information_schema') " +
                "and n.nspname not like 'pg_toast%' " +
                "and n.nspname not like 'pg_temp_%' " +
                "and n.nspname not like 'pg_toast_temp_%' " +
                "order by n.nspname";

            foreach (var datname in dbs)
            {
                if (string.IsNullOrWhiteSpace(datname)) continue;
                if (options != null && !options.ShouldIncludeDatabase(datname)) continue;
                try
                {
                    var cs = BuildConnectionStringForDatabase(datname);
                    using (var conn = CreateSideConnection(cs))
                    {
                        if (conn.State != ConnectionState.Open)
                            conn.Open();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = schemaSql;
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    var name = reader.IsDBNull(0) ? null : reader.GetString(0);
                                    if (string.IsNullOrEmpty(name)) continue;
                                    var isDefault = !reader.IsDBNull(1) && Convert.ToBoolean(reader.GetValue(1));
                                    result.Add(new DbSchemaInfo
                                    {
                                        Database = datname,
                                        Name = name,
                                        IsDefault = isDefault
                                    });
                                }
                            }
                        }
                    }
                }
                catch { /* skip */ }
            }
            return result;
        }

        /// <summary>为指定 database 改写连接串；子类可覆写以适配厂商连接串构建器。</summary>
        protected virtual string BuildConnectionStringForDatabase(string dbName)
        {
            var builder = new NpgsqlConnectionStringBuilder(DBLive.config.DBConnectStr)
            {
                Database = dbName
            };
            return builder.ConnectionString;
        }

        /// <summary>旁路元数据连接；不触碰 DBLive 当前会话。</summary>
        protected virtual DbConnection CreateSideConnection(string connectionString)
        {
            return new NpgsqlConnection(connectionString);
        }

        public override string GetViewInfoListSql =>
            "select cast(c.relname as varchar) as Name, cast(obj_description(c.oid, 'pg_class') as varchar) as Comment " +
            "from pg_class c join pg_namespace n on n.oid = c.relnamespace " +
            "where c.relkind = 'v' and n.nspname = current_schema() " +
            "order by c.relname";


        public override SQLCmd buildHasTable(string TableName)
        {
            var name = ResolvePgCatalogIdent(TableName);
            var cmd = DBLive.useSQL()
                .from("information_schema.tables")
                .where("table_schema = CURRENT_SCHEMA()")
                .where("table_name", name)
                .toSelectCount();
            return cmd;
        }

        /// <summary>
        /// 解析用于查 PG 系统目录 / information_schema 的标识符：
        /// 带双引号则去引号并保留大小写；未加引号则按 PG 规则折叠为小写。
        /// </summary>
        static string ResolvePgCatalogIdent(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return name;
            var trimmed = name.Trim();
            if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[trimmed.Length - 1] == '"')
                return trimmed.Substring(1, trimmed.Length - 2);
            return trimmed.ToLowerInvariant();
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

        public override bool IsConnectionLost(Exception ex)
        {
            if (ex == null) return false;
            if (MatchInnerSqlState(ex, "08000", "08003", "08006", "57P01")) return true;
            return MatchMessage(ex, "connection refused", "could not connect to server");
        }
    }
}
