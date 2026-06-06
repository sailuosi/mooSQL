namespace mooSQL.data.translation
{
    /// <summary>DbFunc 表达式注册项（SQL 模板 + 编译元数据）。</summary>
    public sealed class DbFuncExpressionEntry
    {
        public string? SqlTemplate { get; init; }
        public bool IsPredicate { get; init; }
        public bool PreferServerSide { get; init; }
        public bool ServerSideOnly { get; init; }
        public int Precedence { get; init; } = 100;
        public bool IsPure { get; init; } = true;
        public string? DialectConfiguration { get; init; }
        /// <summary>列表 IN / NOT IN 谓词（由 Ext 编译层专用翻译，非字符串模板）。</summary>
        public bool IsInListPredicate { get; init; }
        public bool IsNotInListPredicate { get; init; }
        /// <summary>窗口函数（ROW_NUMBER 等 ISqlExtension 链）。</summary>
        public bool IsWindowFunction { get; init; }
        /// <summary>聚合函数（COUNT/SUM/AVG 等 ISqlExtension 链）。</summary>
        public bool IsAggregate { get; init; }
        /// <summary>DateDiff(part, start, end) — 由 <see cref="SQLExpression"/> 方言片段翻译。</summary>
        public bool IsDateDiffPredicate { get; init; }
        /// <summary>NullIf(value, compareTo) — 由 <see cref="SQLExpression.nullIf"/> 方言片段翻译。</summary>
        public bool IsNullIfPredicate { get; init; }
        /// <summary>Concat(params …) — 由 <see cref="SQLExpression.stringConcat"/> 链折叠翻译。</summary>
        public bool IsConcatPredicate { get; init; }
        /// <summary>DateAdd(part, amount, date) — 由 <see cref="SQLExpression"/> 方言 dateAdd* 片段翻译。</summary>
        public bool IsDateAddPredicate { get; init; }
        /// <summary>DatePart(part, date) — 由 <see cref="SQLExpression"/> 方言 datePart* 片段翻译。</summary>
        public bool IsDatePartPredicate { get; init; }
        /// <summary>Collate(expr, collation) — 由 <see cref="SQLExpression.collate"/> / collateDb2 方言片段翻译。</summary>
        public bool IsCollatePredicate { get; init; }
        /// <summary>IsNullOrWhiteSpace(str) — 由 <see cref="SQLExpression.isNullOrWhiteSpace"/> 方言片段翻译。</summary>
        public bool IsNullOrWhiteSpacePredicate { get; init; }
        /// <summary>窗口 OVER 子句 — 由 <see cref="WindowOverClause"/> IR 渲染（Phase F P2/P3）。</summary>
        public bool IsWindowOverPredicate { get; init; }
    }
}
