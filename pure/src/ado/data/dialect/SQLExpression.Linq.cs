// LINQ 可翻译函数 — 逐步从 Ext DbFunc 迁入 Pure 方言

using System;

namespace mooSQL.data
{
    public abstract partial class SQLExpression
    {
        /// <summary>SQL BETWEEN 谓词片段（方言可 override）。</summary>
        public virtual string between(string column, string low, string high)
            => $"{column} BETWEEN {low} AND {high}";

        /// <summary>SQL NOT BETWEEN 谓词片段。</summary>
        public virtual string notBetween(string column, string low, string high)
            => $"{column} NOT BETWEEN {low} AND {high}";

        /// <summary>IS NULL 谓词。</summary>
        public virtual string isNull(string expr) => $"{expr} IS NULL";

        /// <summary>IS NOT NULL 谓词。</summary>
        public virtual string isNotNull(string expr) => $"{expr} IS NOT NULL";
    }
}
