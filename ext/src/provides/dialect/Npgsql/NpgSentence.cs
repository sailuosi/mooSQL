
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

        /// <summary>备用模板；正式路径见 <see cref="GetDbColumnsByTableName"/>。</summary>
        public override string GetColumnInfosByTableNameSql =>
            "SELECT c.column_name AS Name, c.udt_name AS DbTypeText, " +
            "CASE WHEN c.udt_name = 'numeric' THEN c.numeric_precision " +
            "ELSE c.character_maximum_length END AS MaxLength, " +
            "CASE WHEN c.udt_name = 'numeric' THEN c.numeric_precision ELSE NULL END AS Precision, " +
            "CASE WHEN c.udt_name = 'numeric' THEN c.numeric_scale ELSE NULL END AS Scale, " +
            "c.column_default AS DefaultValue, " +
            "col_description(pclass.oid, c.ordinal_position) AS Comment, " +
            "CASE WHEN pk.attname IS NOT NULL THEN true ELSE false END AS IsPrimary, " +
            "CASE WHEN c.column_default LIKE 'nextval%' THEN true ELSE false END AS IsIdentity, " +
            "CASE WHEN c.is_nullable = 'YES' THEN true ELSE false END AS IsNullable, " +
            "c.ordinal_position AS Position " +
            "FROM information_schema.columns c " +
            "INNER JOIN pg_namespace n ON n.nspname = c.table_schema " +
            "INNER JOIN pg_class pclass ON pclass.relname = c.table_name AND pclass.relnamespace = n.oid " +
            "LEFT JOIN ( " +
            "  SELECT a.attname, cls.relname, ns.nspname " +
            "  FROM pg_constraint con " +
            "  INNER JOIN pg_class cls ON con.conrelid = cls.oid " +
            "  INNER JOIN pg_namespace ns ON ns.oid = cls.relnamespace " +
            "  INNER JOIN pg_attribute a ON a.attrelid = cls.oid AND a.attnum = ANY(con.conkey) " +
            "  WHERE con.contype = 'p' " +
            ") pk ON pk.attname = c.column_name AND pk.relname = c.table_name AND pk.nspname = c.table_schema " +
            "WHERE c.table_name = '{0}' AND c.table_schema = current_schema() " +
            "ORDER BY c.ordinal_position";

        public override string GetColumnCaptionsByTableNameSql => "SELECT c.column_name AS Name, col_description(pclass.oid, c.ordinal_position) AS Caption " +
            "FROM information_schema.columns c " +
            "INNER JOIN pg_namespace n ON n.nspname = c.table_schema " +
            "INNER JOIN pg_class pclass ON pclass.relname = c.table_name AND pclass.relnamespace = n.oid " +
            "WHERE c.table_name = '{0}' AND c.table_schema = current_schema() " +
            "ORDER BY c.ordinal_position";

        /// <summary>
        /// 用 SQLBuilder 查列元数据；按 <see cref="Dialect.CurVersion"/> 分支自增探测（PG10+ 含 is_identity）。
        /// </summary>
        public override List<DbColumnInfo> GetDbColumnsByTableName(string tableName)
        {
            tableName = ResolvePgCatalogIdent(tableName);
            var isIdentityExpr = BuildPgIsIdentitySelectExpr();

            var kit = DBLive.useSQL()
                .select(
                    "c.column_name AS Name, c.udt_name AS DbTypeText, " +
                    "CASE WHEN c.udt_name = 'numeric' THEN c.numeric_precision " +
                    "ELSE c.character_maximum_length END AS MaxLength, " +
                    "CASE WHEN c.udt_name = 'numeric' THEN c.numeric_precision ELSE NULL END AS Precision, " +
                    "CASE WHEN c.udt_name = 'numeric' THEN c.numeric_scale ELSE NULL END AS Scale, " +
                    "c.column_default AS DefaultValue, " +
                    "col_description(pclass.oid, c.ordinal_position) AS Comment, " +
                    "CASE WHEN pk.attname IS NOT NULL THEN true ELSE false END AS IsPrimary, " +
                    isIdentityExpr + " AS IsIdentity, " +
                    "CASE WHEN c.is_nullable = 'YES' THEN true ELSE false END AS IsNullable, " +
                    "c.ordinal_position AS Position")
                .from("information_schema.columns c")
                .innerJoin("pg_namespace n ON n.nspname = c.table_schema")
                .innerJoin("pg_class pclass ON pclass.relname = c.table_name AND pclass.relnamespace = n.oid")
                .leftJoin(
                    "( SELECT a.attname, cls.relname, ns.nspname " +
                    "FROM pg_constraint con " +
                    "INNER JOIN pg_class cls ON con.conrelid = cls.oid " +
                    "INNER JOIN pg_namespace ns ON ns.oid = cls.relnamespace " +
                    "INNER JOIN pg_attribute a ON a.attrelid = cls.oid AND a.attnum = ANY(con.conkey) " +
                    "WHERE con.contype = 'p' " +
                    ") pk ON pk.attname = c.column_name AND pk.relname = c.table_name AND pk.nspname = c.table_schema")
                .where("c.table_schema = current_schema()")
                .where("c.table_name", tableName)
                .orderBy("c.ordinal_position");

            var tar = kit.query<DbColumnInfo>().ToList();
            foreach (var item in tar)
            {
                if (!string.IsNullOrWhiteSpace(item.DbTypeText))
                {
                    item.FieldType = this.dialect.mapping.GetDataType(item.DbTypeText);
                }
            }
            return tar;
        }

        public override List<DbColumnCaption> GetDbColumnCaptionsByTableName(string tableName)
        {
            return base.GetDbColumnCaptionsByTableName(ResolvePgCatalogIdent(tableName));
        }

        /// <summary>PG 10+ 才有 information_schema.columns.is_identity。</summary>
        protected virtual bool IsPg10OrLater()
        {
            var ver = dialect.CurVersion;
            return ver != null && ver.VersionNumber >= 10;
        }

        /// <summary>按版本组装 IsIdentity 列表达式（别名由调用方追加）。</summary>
        protected virtual string BuildPgIsIdentitySelectExpr()
        {
            if (IsPg10OrLater())
            {
                return "CASE WHEN c.column_default LIKE 'nextval%' OR c.is_identity = 'YES' THEN true ELSE false END";
            }
            return "CASE WHEN c.column_default LIKE 'nextval%' THEN true ELSE false END";
        }

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
