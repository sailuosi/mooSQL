///////////////////////////////////////////////////////////
//  SyncTableJob.cs
///////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using mooSQL.data.sync;

namespace mooSQL.data
{
    /// <summary>单表同步任务（非泛型基类，便于 Host 枚举）。</summary>
    public abstract class SyncTableJob
    {
        /// <summary>源实体类型。</summary>
        public Type SourceType { get; protected set; } = typeof(object);

        /// <summary>目标实体类型。</summary>
        public Type TargetType { get; protected set; } = typeof(object);

        /// <summary>源侧时序列（模式 1）。</summary>
        public string? OrderByColumn { get; set; }

        /// <summary>目标水位列；空则同 OrderByColumn。</summary>
        public string? WatermarkColumnOnTarget { get; set; }

        /// <summary>Upsert 用修改时间列。</summary>
        public string? ModifiedColumn { get; set; }

        /// <summary>显式主键列/属性（覆盖实体 IsPrimaryKey）。</summary>
        public string? KeyColumn { get; set; }

        /// <summary>行策略；空则用 Host 默认。</summary>
        public DataSyncMode? Mode { get; set; }

        /// <summary>失败策略；空则用 Host 默认。</summary>
        public DataSyncFailureAction? FailureAction { get; set; }

        /// <summary>批/页大小覆盖。</summary>
        public int? BatchSize { get; set; }

        /// <summary>源连接位覆盖。</summary>
        public int? SourcePosition { get; set; }

        /// <summary>目标连接位覆盖。</summary>
        public int? TargetPosition { get; set; }

        /// <summary>自定义 SQL（模式 2）。</summary>
        public Action<SQLBuilder>? FromSql { get; set; }

        /// <summary>源查询追加条件（模式 1/3）。</summary>
        public Action<SQLBuilder>? OnSourceQuery { get; set; }

        /// <summary>
        /// ReplaceFull 时目标清空钩子（建议模式 2 窗口 DELETE）。
        /// 已设 from 表名；调用方补 where 后由框架 doDelete。
        /// </summary>
        public Action<SQLBuilder>? OnTargetClear { get; set; }

        /// <summary>
        /// 目标侧追加条件（MirrorKeys 扫目标键 / 删孤儿）；空则目标全表键扫（不复用 OnSourceQuery）。
        /// </summary>
        public Action<SQLBuilder>? OnTargetQuery { get; set; }

        /// <summary>解析拉源模式。</summary>
        public DataSyncPullMode ResolvePullMode()
        {
            if (FromSql != null)
                return DataSyncPullMode.FromSql;
            if (!string.IsNullOrWhiteSpace(OrderByColumn))
                return DataSyncPullMode.Watermark;
            return DataSyncPullMode.EntityByPk;
        }

        /// <summary>
        /// 执行本表同步（一轮内）。
        /// 返回 true = 中止整个 Start（FailureAction.Abort 或批失败 Abort）；
        /// 返回 false = 本表结束（成功或配置跳过），Host 继续后续表。
        /// </summary>
        public abstract bool Execute(DataSyncHost host);
    }

    /// <summary>同构/异构表任务。</summary>
    public sealed class SyncTableJob<TSrc, TDst> : SyncTableJob
        where TSrc : class, new()
        where TDst : class, new()
    {
        /// <summary>行映射；同构可空（Identity）。</summary>
        public Func<TSrc, TDst>? Map { get; set; }

        /// <summary>批映射。</summary>
        public Func<IReadOnlyList<TSrc>, IReadOnlyList<TDst>>? MapBatch { get; set; }

        /// <summary>写入前赋值。</summary>
        public Action<TDst>? OnAssign { get; set; }

        /// <summary>写入前赋值（带源）。</summary>
        public Action<TDst, TSrc>? OnAssignWithSource { get; set; }

        /// <summary>批赋值。</summary>
        public Action<IList<TDst>>? OnAssignBatch { get; set; }

        /// <summary>创建同构/异构任务。</summary>
        public SyncTableJob()
        {
            SourceType = typeof(TSrc);
            TargetType = typeof(TDst);
        }

        /// <inheritdoc />
        public override bool Execute(DataSyncHost host)
        {
            var mode = Mode ?? host.Options.DefaultMode;
            switch (mode)
            {
                case DataSyncMode.AppendOnly:
                    return AppendOnlyRunner.Run(this, host);
                case DataSyncMode.AppendWatermark:
                    return AppendWatermarkRunner.Run(this, host);
                case DataSyncMode.UpsertByModified:
                    return UpsertByModifiedRunner.Run(this, host);
                case DataSyncMode.ReplaceFull:
                    return ReplaceFullRunner.Run(this, host);
                case DataSyncMode.MirrorKeys:
                    return MirrorKeysRunner.Run(this, host);
                default:
                    // 未知模式：跳过本表；仅当 Host FailureAction=Abort 时中止整轮
                    host.AppendProgress("ERR " + typeof(TSrc).Name + ": unsupported mode " + mode + "（已跳过本表）");
                    return host.Options.FailureAction == DataSyncFailureAction.Abort;
            }
        }
    }
}
