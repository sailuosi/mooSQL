using System;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// 各方言常用保留字（OrdinalIgnoreCase）。用于 <see cref="NameQuoteMode.KeywordsOnly"/> / <see cref="NameQuoteMode.IfNeeded"/>。
    /// 非完整标准全集，侧重易被用作表/列名的词。
    /// </summary>
    public static class SqlReservedWords
    {
        static HashSet<string> Set(params string[] words)
            => new HashSet<string>(words, StringComparer.OrdinalIgnoreCase);

        static HashSet<string> Merge(HashSet<string> baseSet, params string[] extra)
        {
            var set = new HashSet<string>(baseSet, StringComparer.OrdinalIgnoreCase);
            foreach (var w in extra)
                set.Add(w);
            return set;
        }

        /// <summary>ANSI / 通用核心保留字。</summary>
        public static readonly HashSet<string> Ansi = Set(
            "add", "all", "alter", "and", "any", "as", "asc", "authorization",
            "between", "both", "by",
            "case", "cast", "check", "collate", "column", "constraint", "create", "cross", "current",
            "default", "delete", "desc", "distinct", "drop",
            "else", "end", "except", "exists",
            "false", "fetch", "for", "foreign", "from", "full",
            "grant", "group",
            "having",
            "in", "index", "inner", "insert", "intersect", "into", "is",
            "join",
            "left", "like", "limit",
            "natural", "not", "null",
            "on", "or", "order", "outer",
            "primary",
            "references", "right",
            "select", "set", "some",
            "table", "then", "to", "true",
            "union", "unique", "update", "using",
            "values",
            "when", "where", "with"
        );

        /// <summary>PostgreSQL / openGauss / CrateDB / 金仓(PG) 等。</summary>
        public static readonly HashSet<string> PostgreSQL = Merge(Ansi,
            "analyse", "analyze", "array", "concurrently", "current_catalog", "current_date",
            "current_role", "current_schema", "current_time", "current_timestamp", "current_user",
            "deferrable", "do", "ilike", "initially", "lateral", "leading", "localtime",
            "localtimestamp", "new", "off", "offset", "old", "only", "placing", "returning",
            "session_user", "symmetric", "trailing", "user", "variadic", "verbose", "window",
            "over", "partition", "range", "rows", "unbounded", "preceding", "following", "current_row"
        );

        /// <summary>SQL Server。</summary>
        public static readonly HashSet<string> SqlServer = Merge(Ansi,
            "backup", "break", "browse", "bulk", "clustered", "compute", "contains", "containstable",
            "continue", "convert", "database", "dbcc", "deny", "disk", "distributed", "double",
            "dump", "errlvl", "escape", "exec", "execute", "exit", "external", "file", "fillfactor",
            "freetext", "freetexttable", "function", "goto", "holdlock", "identity", "identitycol",
            "identity_insert", "if", "key", "kill", "lineno", "load", "merge", "national", "nocheck",
            "nonclustered", "of", "off", "offsets", "open", "opendatasource", "openquery", "openrowset",
            "openxml", "option", "over", "percent", "plan", "precision", "print", "proc", "procedure",
            "public", "raiserror", "read", "readtext", "reconfigure", "replication", "restore",
            "restrict", "return", "revert", "revoke", "rollback", "rowcount", "rowguidcol", "rule",
            "save", "schema", "securityaudit", "semantickeyphrasetable", "semanticsimilaritydetailstable",
            "semanticsimilaritytable", "session_user", "setuser", "shutdown", "statistics", "system_user",
            "tablesample", "textsize", "top", "tran", "transaction", "trigger", "truncate", "tsequal",
            "updatetext", "user", "vary", "view", "waitfor", "while", "within", "writetext",
            "pivot", "unpivot", "output", "match", "path"
        );

        /// <summary>Oracle / 达梦(偏 Oracle) / Oscar。</summary>
        public static readonly HashSet<string> Oracle = Merge(Ansi,
            "access", "audit", "cluster", "comment", "compress", "connect", "date", "decimal",
            "exclusive", "file", "float", "identified", "immediate", "increment", "initial",
            "integer", "level", "lock", "long", "maxextents", "minus", "mode", "modify", "noaudit",
            "nocompress", "nowait", "number", "offline", "online", "pc", "prior", "privileges",
            "public", "raw", "rename", "resource", "row", "rowid", "rownum", "rows", "session",
            "share", "size", "smallint", "start", "successful", "synonym", "sysdate", "systimestamp",
            "trigger", "uid", "user", "validate", "varchar", "varchar2", "view", "whenever"
        );

        /// <summary>MySQL / MariaDB / TiDB / OceanBase(MySQL) 等。</summary>
        public static readonly HashSet<string> MySql = Merge(Ansi,
            "accessible", "analyze", "asensitive", "before", "bigint", "binary", "blob", "call",
            "cascade", "change", "condition", "continue", "convert", "current_user", "cursor",
            "database", "databases", "day_hour", "day_microsecond", "day_minute", "day_second",
            "dec", "decimal", "declare", "delayed", "describe", "deterministic", "distinctrow",
            "div", "dual", "each", "elseif", "enclosed", "escaped", "exit", "explain", "float",
            "float4", "float8", "force", "fulltext", "generated", "get", "high_priority",
            "hour_microsecond", "hour_minute", "hour_second", "if", "ignore", "infile", "inout",
            "insensitive", "int", "int1", "int2", "int3", "int4", "int8", "integer", "interval",
            "iterate", "key", "keys", "kill", "leading", "leave", "linear", "lines", "load",
            "localtime", "localtimestamp", "longblob", "longtext", "loop", "low_priority",
            "master_ssl_verify_server_cert", "match", "maxvalue", "mediumblob", "mediumint",
            "mediumtext", "middleint", "minute_microsecond", "minute_second", "mod", "modifies",
            "no_write_to_binlog", "numeric", "optimize", "option", "optionally", "out", "outfile",
            "precision", "procedure", "purge", "range", "read", "reads", "read_write", "real",
            "regexp", "release", "rename", "repeat", "replace", "require", "resignal", "restrict",
            "return", "revoke", "rlike", "schema", "schemas", "second_microsecond", "sensitive",
            "separator", "show", "signal", "spatial", "specific", "sql", "sqlexception", "sqlstate",
            "sqlwarning", "sql_big_result", "sql_calc_found_rows", "sql_small_result", "ssl",
            "starting", "stored", "straight_join", "terminated", "tinyblob", "tinyint", "tinytext",
            "trigger", "undo", "unlock", "unsigned", "usage", "use", "user", "utc_date", "utc_time",
            "utc_timestamp", "varbinary", "varchar", "varcharacter", "varying", "virtual", "while",
            "write", "xor", "year_month", "zerofill", "partition", "rows"
        );

        /// <summary>SQLite。</summary>
        public static readonly HashSet<string> SQLite = Merge(Ansi,
            "abort", "action", "after", "attach", "autoincrement", "before", "begin", "cascade",
            "conflict", "database", "deferrable", "deferred", "detach", "each", "escape", "exclude",
            "exclusive", "explain", "fail", "filter", "following", "glob", "if", "ignore",
            "immediate", "initially", "instead", "isnull", "key", "match", "no", "notnull", "of",
            "offset", "plan", "pragma", "preceding", "query", "raise", "recursive", "regexp",
            "reindex", "release", "rename", "replace", "restrict", "returning", "rollback", "row",
            "rows", "savepoint", "temp", "temporary", "transaction", "trigger", "unbounded",
            "vacuum", "view", "virtual", "without", "user"
        );

        /// <summary>ClickHouse（常用冲突词）。</summary>
        public static readonly HashSet<string> ClickHouse = Merge(Ansi,
            "array", "asof", "attach", "database", "detach", "dictionary", "engine", "format",
            "function", "global", "granularity", "interval", "kill", "materialized", "optimize",
            "partition", "populate", "prewhere", "projection", "sample", "settings", "system",
            "ttl", "use", "view", "watch", "window", "user", "key", "offset"
        );
    }
}
