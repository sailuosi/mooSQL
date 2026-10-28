using System;
using mooSQL.data.model;

namespace mooSQL.data
{
    public abstract partial class Dialect
    {
        /// <summary>
        /// 解析当前方言的插入取 Id 模式：优先 Returning（需列名），否则 PostSelect，否则 Unsupported。
        /// </summary>
        public virtual IdentityFetchMode GetInsertIdentityMode(string identityColumn = null)
        {
            var flags = Option?.ProviderFlags;
            var hasReturning = flags != null && flags.IsInsertReturningSupported;
            var hasPost = expression != null && !string.IsNullOrWhiteSpace(expression.selectAutoIncrement);

            if (hasReturning && !string.IsNullOrWhiteSpace(identityColumn))
                return IdentityFetchMode.Returning;
            if (hasPost)
                return IdentityFetchMode.PostSelect;
            if (hasReturning)
                return IdentityFetchMode.Returning; // 缺列名时由 Build 抛清澈异常
            return IdentityFetchMode.Unsupported;
        }

        /// <summary>
        /// 将已生成的 INSERT <see cref="SQLCmd"/> 改写为取 Id 命令（<see cref="QueryType.InsertReturnId"/>）。
        /// </summary>
        /// <param name="insert">普通插入命令（通常来自 <c>toInsert()</c>）。</param>
        /// <param name="identityColumn">RETURNING/OUTPUT 时的标识列名；PostSelect 可忽略。</param>
        public virtual SQLCmd BuildInsertReturnIdCmd(SQLCmd insert, string identityColumn = null)
        {
            if (insert == null || insert.Empty)
                throw new InvalidOperationException("BuildInsertReturnIdCmd: insert SQLCmd is empty.");
            if (insert.HasSegments)
                throw new InvalidOperationException(
                    "BuildInsertReturnIdCmd: multi-segment insert (param/batch split) is not supported. Use single-row InsertReturnId or L2 keys.");

            var mode = GetInsertIdentityMode(identityColumn);
            switch (mode)
            {
                case IdentityFetchMode.PostSelect:
                    return AppendPostSelectIdentity(insert);
                case IdentityFetchMode.Returning:
                    if (string.IsNullOrWhiteSpace(identityColumn))
                        throw new InvalidOperationException(
                            "BuildInsertReturnIdCmd: identity column name is required for RETURNING/OUTPUT dialects.");
                    return AppendReturningIdentity(insert, identityColumn);
                default:
                    throw new NotSupportedException(
                        "InsertReturnId is not supported by the current dialect (no selectAutoIncrement and RETURNING not enabled).");
            }
        }

        /// <summary>PostSelect：同一 CommandText 追加 <c>; SELECT last_id</c>。</summary>
        protected virtual SQLCmd AppendPostSelectIdentity(SQLCmd insert)
        {
            var selectId = expression.selectAutoIncrement?.Trim();
            if (string.IsNullOrWhiteSpace(selectId))
                throw new NotSupportedException("PostSelect identity SQL is empty.");

            var sep = string.IsNullOrEmpty(expression.SentenceSeprator)
                ? ";"
                : expression.SentenceSeprator;
            var baseSql = (insert.sql ?? "").TrimEnd();
            while (baseSql.EndsWith(";", StringComparison.Ordinal))
                baseSql = baseSql.Substring(0, baseSql.Length - 1).TrimEnd();

            var sql = baseSql + sep + " " + selectId;
            return CloneAsInsertReturnId(insert, sql);
        }

        /// <summary>Returning：追加 <c>RETURNING col</c>（PG/MariaDB/DuckDB 等）。MSSQL OUTPUT 可由子类覆盖。</summary>
        protected virtual SQLCmd AppendReturningIdentity(SQLCmd insert, string identityColumn)
        {
            var col = QuoteIdentityColumn(identityColumn);
            var baseSql = (insert.sql ?? "").TrimEnd();
            while (baseSql.EndsWith(";", StringComparison.Ordinal))
                baseSql = baseSql.Substring(0, baseSql.Length - 1).TrimEnd();

            var sql = baseSql + " RETURNING " + col;
            return CloneAsInsertReturnId(insert, sql);
        }

        /// <summary>标识列名引用（默认方言 wrapKeyword）。</summary>
        protected virtual string QuoteIdentityColumn(string identityColumn)
        {
            if (expression == null || string.IsNullOrWhiteSpace(identityColumn))
                return identityColumn;
            // 已带限定/引号时不重复包
            if (identityColumn.IndexOfAny(new[] { '.', '"', '[', '`' }) >= 0)
                return identityColumn;
            return expression.wrapKeyword(identityColumn);
        }

        /// <summary>克隆为 <see cref="QueryType.InsertReturnId"/> 命令。</summary>
        protected static SQLCmd CloneAsInsertReturnId(SQLCmd insert, string sql)
        {
            var cmd = new SQLCmd(sql, insert.para);
            cmd.type = QueryType.InsertReturnId;
            cmd.TargetTable = insert.TargetTable;
            cmd.signal = insert.signal;
            cmd.cmdType = insert.cmdType;
            cmd.timeout = insert.timeout;
            return cmd;
        }

        /// <summary>是否可用单语句多行取全量 Id（RETURNING / OUTPUT）。</summary>
        public virtual bool SupportsMultiRowInsertReturnId(string identityColumn = null)
            => GetInsertIdentityMode(identityColumn) == IdentityFetchMode.Returning;
    }
}
