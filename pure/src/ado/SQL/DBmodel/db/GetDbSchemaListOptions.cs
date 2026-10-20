using System;

namespace mooSQL.data
{
    /// <summary>
    /// <see cref="SQLSentence.GetDbSchemaList"/> 库范围选项。
    /// </summary>
    public class GetDbSchemaListOptions
    {
        /// <summary>仅当前连接数据库。</summary>
        public bool CurrentDatabaseOnly { get; set; }

        /// <summary>指定库名（精确匹配，忽略大小写）。非空时优先于 <see cref="CurrentDatabaseOnly"/>。</summary>
        public string DatabaseName { get; set; }

        /// <summary>方言可填入当前库名，供 <see cref="CurrentDatabaseOnly"/>。</summary>
        public string CurrentDatabaseName { get; set; }

        /// <summary>生效库名：DatabaseName 优先，否则 CurrentDatabaseOnly 时用 CurrentDatabaseName。</summary>
        public string EffectiveDatabaseName =>
            !string.IsNullOrEmpty(DatabaseName) ? DatabaseName
            : CurrentDatabaseOnly ? CurrentDatabaseName
            : null;

        public bool HasDatabaseScope =>
            !string.IsNullOrEmpty(DatabaseName) || CurrentDatabaseOnly;

        public bool ShouldIncludeDatabase(string databaseName)
        {
            var scope = EffectiveDatabaseName;
            if (string.IsNullOrEmpty(scope)) return true;
            return !string.IsNullOrEmpty(databaseName)
                && databaseName.Equals(scope, StringComparison.OrdinalIgnoreCase);
        }
    }
}
