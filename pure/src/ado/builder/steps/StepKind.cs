namespace mooSQL.data
{
    /// <summary>
    /// 编排步骤家族；门面 <c>Enqueue</c> 按此更新计数（无 StepDelta）。
    /// </summary>
    public enum StepKind : byte
    {
        Unknown = 0,

        Select,
        Distinct,
        TopSkipTake,
        OrderBy,
        GroupBy,
        Having,
        RowNumber,
        SelectMisc,

        From,
        Join,
        PivotUnpivot,

        Where,
        WhereControl,
        ClearWhere,

        Set,
        SetTable,
        SetRow,
        ClearSelect,

        Cte,
        Union,
        Merge,

        Control,
        ClearPage,
        Other
    }
}
