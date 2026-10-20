using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mooSQL.data
{
    /// <summary>
    /// <see cref="SQLSentence.GetDbTableListAll"/> 排序字段。
    /// </summary>
    public enum DbTableListSortBy
    {
        None = 0,
        Name = 1,
        Comment = 2,
        RowCount = 3,
        Schema = 4,
    }

    /// <summary>
    /// 方言侧表列表查询的列/表达式名，供 options 下推到 SQLBuilder。
    /// </summary>
    public class DbTableListSqlColumns
    {
        public string Name { get; set; }
        public string Comment { get; set; }
        public string RowCount { get; set; }
        public string Schema { get; set; }
        /// <summary>库名/catalog 列（MySQL TABLE_SCHEMA、ClickHouse database 等）。</summary>
        public string Database { get; set; }
        /// <summary>未指定 SortBy 时的默认 ORDER BY 片段。</summary>
        public string DefaultOrderBy { get; set; }
    }

    /// <summary>
    /// 全库表列表的过滤与排序选项。
    /// </summary>
    public class GetDbTableListOptions
    {
        /// <summary>表名包含（忽略大小写）。</summary>
        public string NameContains { get; set; }

        /// <summary>表中文名/备注包含（忽略大小写）。</summary>
        public string CommentContains { get; set; }

        /// <summary>
        /// 仅当前连接数据库。
        /// MySQL/ClickHouse/MSSQL 为 database；PostgreSQL 为 datname。
        /// </summary>
        public bool CurrentDatabaseOnly { get; set; }

        /// <summary>
        /// 指定库名（精确匹配，忽略大小写）。非空时优先于 <see cref="CurrentDatabaseOnly"/>。
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// 方言在 Finalize 前可填入当前库名，供 <see cref="CurrentDatabaseOnly"/> 使用。
        /// </summary>
        public string CurrentDatabaseName { get; set; }

        /// <summary>生效的库名范围：DatabaseName 优先，否则 CurrentDatabaseOnly 时用 CurrentDatabaseName。</summary>
        public string EffectiveDatabaseName =>
            !string.IsNullOrEmpty(DatabaseName) ? DatabaseName
            : CurrentDatabaseOnly ? CurrentDatabaseName
            : null;

        /// <summary>是否限定了库范围。</summary>
        public bool HasDatabaseScope =>
            !string.IsNullOrEmpty(DatabaseName) || CurrentDatabaseOnly;

        /// <summary>最小近似行数（含）。</summary>
        public long? MinRowCount { get; set; }

        /// <summary>最大近似行数（含）。</summary>
        public long? MaxRowCount { get; set; }

        /// <summary>排序字段。</summary>
        public DbTableListSortBy SortBy { get; set; } = DbTableListSortBy.None;

        /// <summary>是否降序。</summary>
        public bool SortDescending { get; set; }

        /// <summary>是否需要近似行数（过滤或按行数排序时为 true）。</summary>
        public bool NeedsRowCount =>
            MinRowCount.HasValue || MaxRowCount.HasValue || SortBy == DbTableListSortBy.RowCount;

        /// <summary>表名条件已下推到 SQL。</summary>
        public bool NameFilterPushed { get; set; }
        /// <summary>备注条件已下推到 SQL。</summary>
        public bool CommentFilterPushed { get; set; }
        /// <summary>行数条件已下推到 SQL。</summary>
        public bool RowCountFilterPushed { get; set; }
        /// <summary>库名条件已下推（或已在库循环中收窄）。</summary>
        public bool DatabaseFilterPushed { get; set; }
        /// <summary>排序已下推到 SQL。</summary>
        public bool SortPushedDown { get; set; }

        /// <summary>判断库名是否落入当前选项范围（支持 Schema 为 db 或 db.schema）。</summary>
        public bool MatchesDatabaseScope(string schemaOrDatabase)
        {
            var db = EffectiveDatabaseName;
            if (string.IsNullOrEmpty(db)) return true;
            if (string.IsNullOrEmpty(schemaOrDatabase)) return false;
            if (schemaOrDatabase.Equals(db, StringComparison.OrdinalIgnoreCase)) return true;
            var idx = schemaOrDatabase.IndexOf('.');
            if (idx > 0)
            {
                var head = schemaOrDatabase.Substring(0, idx);
                return head.Equals(db, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        /// <summary>多库循环时：库名是否应纳入扫描。</summary>
        public bool ShouldIncludeDatabase(string databaseName)
        {
            var scope = EffectiveDatabaseName;
            if (string.IsNullOrEmpty(scope)) return true;
            return !string.IsNullOrEmpty(databaseName)
                && databaseName.Equals(scope, StringComparison.OrdinalIgnoreCase);
        }

        public GetDbTableListOptions OrderByName(bool descending = false)
        {
            SortBy = DbTableListSortBy.Name;
            SortDescending = descending;
            return this;
        }

        public GetDbTableListOptions OrderByCount(bool descending = false)
        {
            SortBy = DbTableListSortBy.RowCount;
            SortDescending = descending;
            return this;
        }

        public GetDbTableListOptions OrderByComment(bool descending = false)
        {
            SortBy = DbTableListSortBy.Comment;
            SortDescending = descending;
            return this;
        }

        public GetDbTableListOptions OrderBySchema(bool descending = false)
        {
            SortBy = DbTableListSortBy.Schema;
            SortDescending = descending;
            return this;
        }

        /// <summary>
        /// 将可下推的过滤/排序应用到 SQLBuilder；成功项会置对应 *Pushed 标记。
        /// </summary>
        /// <param name="includeSort">多库循环场景应传 false，合并后再内存排序。</param>
        public void ApplyToBuilder(SQLBuilder kit, DbTableListSqlColumns cols, bool includeSort = true)
        {
            if (kit == null || cols == null) return;

            var dbScope = EffectiveDatabaseName;
            // 仅当提供 Database 列时下推库名（勿回退到 Schema：MSSQL/PG 的 Schema 是 owner/nsp）
            if (!string.IsNullOrEmpty(dbScope) && !string.IsNullOrEmpty(cols.Database))
            {
                kit.where(cols.Database, dbScope);
                DatabaseFilterPushed = true;
            }

            if (!string.IsNullOrEmpty(NameContains) && !string.IsNullOrEmpty(cols.Name))
            {
                kit.whereLike(cols.Name, NameContains);
                NameFilterPushed = true;
            }

            if (!string.IsNullOrEmpty(CommentContains) && !string.IsNullOrEmpty(cols.Comment))
            {
                kit.whereLike(cols.Comment, CommentContains);
                CommentFilterPushed = true;
            }

            if (MinRowCount.HasValue && !string.IsNullOrEmpty(cols.RowCount))
            {
                kit.where(cols.RowCount, MinRowCount.Value, ">=");
                RowCountFilterPushed = true;
            }
            if (MaxRowCount.HasValue && !string.IsNullOrEmpty(cols.RowCount))
            {
                kit.where(cols.RowCount, MaxRowCount.Value, "<=");
                RowCountFilterPushed = true;
            }

            if (!includeSort)
            {
                if (SortBy == DbTableListSortBy.None && !string.IsNullOrEmpty(cols.DefaultOrderBy))
                    kit.orderBy(cols.DefaultOrderBy);
                return;
            }

            if (SortBy == DbTableListSortBy.None)
            {
                if (!string.IsNullOrEmpty(cols.DefaultOrderBy))
                    kit.orderBy(cols.DefaultOrderBy);
                return;
            }

            string orderCol = SortBy switch
            {
                DbTableListSortBy.Name => cols.Name,
                DbTableListSortBy.Comment => cols.Comment,
                DbTableListSortBy.RowCount => cols.RowCount,
                DbTableListSortBy.Schema => cols.Schema,
                _ => null,
            };
            if (string.IsNullOrEmpty(orderCol))
            {
                if (!string.IsNullOrEmpty(cols.DefaultOrderBy))
                    kit.orderBy(cols.DefaultOrderBy);
                return;
            }

            kit.orderBy(SortDescending ? orderCol + " DESC" : orderCol);
            SortPushedDown = true;
        }

        /// <summary>
        /// 将可下推过滤追加到 WHERE 片段（用于旁路连接等无法走 SQLBuilder 执行的场景）。
        /// 值按 SQL 字面量转义；PostgreSQL 用 ILIKE。
        /// </summary>
        public void ApplyFiltersToWhereSql(StringBuilder whereAnd, DbTableListSqlColumns cols, bool useILike = false)
        {
            if (whereAnd == null || cols == null) return;
            var likeOp = useILike ? "ILIKE" : "LIKE";

            var dbScope = EffectiveDatabaseName;
            if (!string.IsNullOrEmpty(dbScope) && !string.IsNullOrEmpty(cols.Database))
            {
                whereAnd.Append(" AND ").Append(cols.Database).Append(" = '")
                    .Append(EscapeSqlLiteral(dbScope)).Append("'");
                DatabaseFilterPushed = true;
            }

            if (!string.IsNullOrEmpty(NameContains) && !string.IsNullOrEmpty(cols.Name))
            {
                whereAnd.Append(" AND ").Append(cols.Name).Append(' ').Append(likeOp)
                    .Append(" '%").Append(EscapeSqlLiteral(NameContains)).Append("%'");
                NameFilterPushed = true;
            }
            if (!string.IsNullOrEmpty(CommentContains) && !string.IsNullOrEmpty(cols.Comment))
            {
                whereAnd.Append(" AND ").Append(cols.Comment).Append(' ').Append(likeOp)
                    .Append(" '%").Append(EscapeSqlLiteral(CommentContains)).Append("%'");
                CommentFilterPushed = true;
            }
            if (MinRowCount.HasValue && !string.IsNullOrEmpty(cols.RowCount))
            {
                whereAnd.Append(" AND ").Append(cols.RowCount).Append(" >= ").Append(MinRowCount.Value);
                RowCountFilterPushed = true;
            }
            if (MaxRowCount.HasValue && !string.IsNullOrEmpty(cols.RowCount))
            {
                whereAnd.Append(" AND ").Append(cols.RowCount).Append(" <= ").Append(MaxRowCount.Value);
                RowCountFilterPushed = true;
            }
        }

        static string EscapeSqlLiteral(string value)
        {
            return (value ?? "").Replace("'", "''");
        }

        /// <summary>
        /// 对尚未下推的过滤/排序在内存中补齐。
        /// </summary>
        public List<DbTableInfo> Apply(List<DbTableInfo> list)
        {
            if (list == null) return new List<DbTableInfo>();
            IEnumerable<DbTableInfo> q = list;

            if (!DatabaseFilterPushed && HasDatabaseScope)
            {
                q = q.Where(t => MatchesDatabaseScope(t?.Schema));
            }

            if (!NameFilterPushed && !string.IsNullOrEmpty(NameContains))
            {
                var key = NameContains;
                q = q.Where(t => t?.Name != null
                    && t.Name.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (!CommentFilterPushed && !string.IsNullOrEmpty(CommentContains))
            {
                var key = CommentContains;
                q = q.Where(t => t?.Comment != null
                    && t.Comment.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (!RowCountFilterPushed && (MinRowCount.HasValue || MaxRowCount.HasValue))
            {
                q = q.Where(t =>
                {
                    if (t?.RowCount == null) return false;
                    if (MinRowCount.HasValue && t.RowCount.Value < MinRowCount.Value) return false;
                    if (MaxRowCount.HasValue && t.RowCount.Value > MaxRowCount.Value) return false;
                    return true;
                });
            }

            if (SortPushedDown || SortBy == DbTableListSortBy.None)
                return q.ToList();

            Func<DbTableInfo, IComparable> keySelector = SortBy switch
            {
                DbTableListSortBy.Name => t => t?.Name ?? "",
                DbTableListSortBy.Comment => t => t?.Comment ?? "",
                DbTableListSortBy.Schema => t => t?.Schema ?? "",
                DbTableListSortBy.RowCount => t => t?.RowCount ?? -1L,
                _ => t => t?.Name ?? "",
            };

            return SortDescending
                ? q.OrderByDescending(keySelector).ToList()
                : q.OrderBy(keySelector).ToList();
        }
    }
}
