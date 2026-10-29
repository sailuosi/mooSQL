///////////////////////////////////////////////////////////
//  SooDataSyncMode.cs
//  行同步策略（与结构 SyncMode 区分）
///////////////////////////////////////////////////////////

namespace mooSQL.data
{
    /// <summary>
    /// 数据行同步策略。勿与表结构 <see cref="SyncMode"/> 混淆。
    /// </summary>
    public enum SooDataSyncMode
    {
        /// <summary>仅插入目标不存在的主键行；可配时序水位。</summary>
        AppendOnly = 0,

        /// <summary>按水位拉取后直接插入，不做主键探活。</summary>
        AppendWatermark = 1,

        /// <summary>按主键插入或按修改时间更新。</summary>
        UpsertByModified = 2,

        /// <summary>先清空目标范围再全量 Bulk（需破坏性闸门）。</summary>
        ReplaceFull = 3,

        /// <summary>
        /// 键镜像：先按源补齐目标缺失行，再删除目标中源不存在的主键（需破坏性闸门）。
        /// </summary>
        MirrorKeys = 4
    }

    /// <summary>批失败时的行为。</summary>
    public enum SooDataSyncFailureAction
    {
        /// <summary>默认：跳过本批并继续。</summary>
        SkipBatch = 0,

        /// <summary>中断本次 Start。</summary>
        Abort = 1
    }

    /// <summary>Host 生命周期状态。</summary>
    public enum SooDataSyncHostState
    {
        /// <summary>空闲。</summary>
        Idle = 0,
        /// <summary>运行中。</summary>
        Running = 1,
        /// <summary>已暂停。</summary>
        Paused = 2,
        /// <summary>正在停止。</summary>
        Stopping = 3
    }

    /// <summary>源侧拉取模式（由配置判定）。</summary>
    public enum SooDataSyncPullMode
    {
        /// <summary>时序字段 + 水位游标。</summary>
        Watermark = 1,

        /// <summary>自定义 SQL + 翻页。</summary>
        FromSql = 2,

        /// <summary>仅实体，按主键翻页。</summary>
        EntityByPk = 3
    }
}
