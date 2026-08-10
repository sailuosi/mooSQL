namespace mooSQL.data.richRepo.tracking
{
    /// <summary>
    /// 可选：实体自带脏字段袋。多数场景用 WeakTable，无需实现本接口。
    /// </summary>
    public interface ITrackedEntity
    {
        /// <summary>脏字段袋。</summary>
        EntityChangeBag ChangeBag { get; }
    }
}
