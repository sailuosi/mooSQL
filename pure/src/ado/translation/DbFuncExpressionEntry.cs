namespace mooSQL.data.translation
{
    /// <summary>DbFunc 表达式注册项（SQL 模板 + 编译元数据）。</summary>
    public sealed class DbFuncExpressionEntry
    {
        public string? SqlTemplate { get;set; }
        public bool IsPredicate { get;set; }
        public bool PreferServerSide { get;set; }
        public bool ServerSideOnly { get;set; }
        public int Precedence { get;set; } = 100;
        public bool IsPure { get;set; } = true;
        public string? DialectConfiguration { get;set; }
        /// <summary>列表 IN / NOT IN 谓词（由 Ext 编译层专用翻译，非字符串模板）。</summary>
        public bool IsInListPredicate { get;set; }
        public bool IsNotInListPredicate { get;set; }
        /// <summary>窗口函数（ROW_NUMBER 等 ISqlExtension 链）。</summary>
        public bool IsWindowFunction { get;set; }
        /// <summary>聚合函数（COUNT/SUM/AVG 等 ISqlExtension 链）。</summary>
        public bool IsAggregate { get;set; }
        /// <summary>DateDiff(part, start, end) — 由 <see cref="SQLExpression"/> 方言片段翻译。</summary>
        public bool IsDateDiffPredicate { get;set; }
        /// <summary>NullIf(value, compareTo) — 由 <see cref="SQLExpression.nullIf"/> 方言片段翻译。</summary>
        public bool IsNullIfPredicate { get;set; }
        /// <summary>Concat(params …) — 由 <see cref="SQLExpression.stringConcat"/> 链折叠翻译。</summary>
        public bool IsConcatPredicate { get;set; }
        /// <summary>DateAdd(part, amount, date) — 由 <see cref="SQLExpression"/> 方言 dateAdd* 片段翻译。</summary>
        public bool IsDateAddPredicate { get;set; }
        /// <summary>DatePart(part, date) — 由 <see cref="SQLExpression"/> 方言 datePart* 片段翻译。</summary>
        public bool IsDatePartPredicate { get;set; }
        /// <summary>Collate(expr, collation) — 由 <see cref="SQLExpression.collate"/> / collateDb2 方言片段翻译。</summary>
        public bool IsCollatePredicate { get;set; }
        /// <summary>IsNullOrWhiteSpace(str) — 由 <see cref="SQLExpression.isNullOrWhiteSpace"/> 方言片段翻译。</summary>
        public bool IsNullOrWhiteSpacePredicate { get;set; }
        /// <summary>窗口 OVER 子句 — 由 <see cref="WindowOverClause"/> IR 渲染（Phase F P2/P3）。</summary>
        public bool IsWindowOverPredicate { get;set; }
    }
}
