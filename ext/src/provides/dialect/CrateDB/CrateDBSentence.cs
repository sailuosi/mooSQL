using System;
using System.Data;

namespace mooSQL.data
{
    /// <summary>
    /// CrateDB 元数据 SQL：继承 NpgSentence，避开不可靠的 pg_catalog / 序列能力。
    /// </summary>
    public class CrateDBSentence : NpgSentence
    {
        public CrateDBSentence(Dialect dialect) : base(dialect)
        {
        }

        /// <summary>Crate 无可靠 nextval/generate_series 发号；显式不支持。</summary>
        public override string GetReserveSequenceValuesSql(int count, string sequenceName)
        {
            throw new NotSupportedException(
                "CrateDB 不支持 SERIAL/SEQUENCE 发号；请使用 GUID 或外部发号。");
        }

        /// <summary>列出当前可见 schema（连接 Database 参数映射为 schema，默认 doc）。</summary>
        public override string GetDataBaseSql =>
            "SELECT schema_name AS datname FROM information_schema.schemata ORDER BY schema_name";

        /// <summary>列信息：仅 information_schema，无 col_description / nextval 身份探测。</summary>
        public override string GetColumnInfosByTableNameSql =>
            "SELECT c.column_name AS Name, c.data_type AS DataType, " +
            "CASE WHEN c.data_type = 'numeric' THEN c.numeric_precision " +
            "ELSE c.character_maximum_length END AS ColumnLength, " +
            "CASE WHEN c.data_type = 'numeric' THEN c.numeric_scale ELSE NULL END AS Scale, " +
            "c.column_default AS DefaultValue, " +
            "CAST(NULL AS varchar) AS Comment, " +
            "false AS IsPrimary, " +
            "false AS IsIdentity, " +
            "CASE WHEN c.is_nullable = 'YES' THEN true ELSE false END AS IsNullable " +
            "FROM information_schema.columns c " +
            "WHERE c.table_name = '{0}' AND c.table_schema = CURRENT_SCHEMA() " +
            "ORDER BY c.ordinal_position";

        /// <summary>Crate 无列注释函数；返回空 Caption。</summary>
        public override string GetColumnCaptionsByTableNameSql =>
            "SELECT c.column_name AS Name, CAST(NULL AS varchar) AS Caption " +
            "FROM information_schema.columns c " +
            "WHERE c.table_name = '{0}' AND c.table_schema = CURRENT_SCHEMA() " +
            "ORDER BY c.ordinal_position";

        public override string GetTableInfoListSql =>
            "SELECT table_name AS Name, CAST(NULL AS varchar) AS Comment " +
            "FROM information_schema.tables " +
            "WHERE table_schema = CURRENT_SCHEMA() AND table_type = 'BASE TABLE' " +
            "ORDER BY table_name";

        public override string GetViewInfoListSql =>
            "SELECT table_name AS Name, CAST(NULL AS varchar) AS Description " +
            "FROM information_schema.views " +
            "WHERE table_schema = CURRENT_SCHEMA() " +
            "ORDER BY table_name";

        public override bool? IsView(string tabelOrViewName, string dbName = null)
        {
            if (string.IsNullOrWhiteSpace(tabelOrViewName)) return null;
            var c = DBLive.useSQL()
                .from("information_schema.views")
                .where("table_schema = CURRENT_SCHEMA()")
                .where("table_name", tabelOrViewName.ToLowerInvariant())
                .count();
            return c > 0;
        }

        public override bool IsExitsTableIndex(string table, string indexName)
        {
            if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(indexName))
                return false;
            // Crate 索引/约束多体现在 table_constraints；无 pg_indexes 时用此探测
            var c = DBLive.useSQL()
                .from("information_schema.table_constraints")
                .where("table_schema = CURRENT_SCHEMA()")
                .where("table_name", table.ToLowerInvariant())
                .where("constraint_name", indexName.ToLowerInvariant())
                .count();
            return c > 0;
        }

        public override string GetTablePKName(string table)
        {
            if (string.IsNullOrWhiteSpace(table)) return string.Empty;
            return DBLive.useSQL()
                .select("constraint_name")
                .from("information_schema.table_constraints")
                .where("table_schema = CURRENT_SCHEMA()")
                .where("table_name", table.ToLowerInvariant())
                .where("constraint_type", "PRIMARY KEY")
                .top(1)
                .queryRowString("");
        }
    }
}
