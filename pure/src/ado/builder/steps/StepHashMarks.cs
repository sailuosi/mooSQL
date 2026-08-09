namespace mooSQL.data
{
    /// <summary>子步骤磁带边界哨兵，防止拼接歧义。</summary>
    public static class StepHashMarks
    {
        public const int ChildBegin = unchecked((int)0xC11D0001);
        public const int ChildEnd = unchecked((int)0xC11D0002);
    }
}
