///////////////////////////////////////////////////////////
//  DataSyncOptions.cs
///////////////////////////////////////////////////////////

using System;

namespace mooSQL.data.sync
{
    /// <summary>DataSync Host 级选项。</summary>
    public sealed class DataSyncOptions
    {
        /// <summary>源连接位。</summary>
        public int SourcePosition { get; set; }

        /// <summary>目标连接位。</summary>
        public int TargetPosition { get; set; }

        /// <summary>默认行同步策略。</summary>
        public DataSyncMode DefaultMode { get; set; } = DataSyncMode.AppendOnly;

        /// <summary>是否在同步前 Ensure 目标表结构。</summary>
        public bool EnsureTargetSchema { get; set; }

        /// <summary>结构同步模式（表结构枚举）。</summary>
        public SyncMode SchemaMode { get; set; } = SyncMode.CreateIfMissing;

        /// <summary>是否倾向 Bulk 写入。</summary>
        public bool UseBulk { get; set; } = true;

        /// <summary>表间并行度（1=串行）。</summary>
        public int MaxDegreeOfParallelism { get; set; } = 1;

        /// <summary>允许 ReplaceFull 破坏性清空。</summary>
        public bool AllowDestructiveReplace { get; set; }

        /// <summary>Host 默认批/页大小。</summary>
        public int BatchSize { get; set; } = 2000;

        /// <summary>批与批间歇。</summary>
        public TimeSpan BatchInterval { get; set; } = TimeSpan.Zero;

        /// <summary>一轮追平后的轮次间歇；null 表示追平后 Idle。</summary>
        public TimeSpan? HostInterval { get; set; }

        /// <summary>批失败策略。</summary>
        public DataSyncFailureAction FailureAction { get; set; } = DataSyncFailureAction.SkipBatch;

        /// <summary>进度行回调（对齐 SQLBuilder.print）。</summary>
        public Action<string>? OnPrint { get; set; }
    }
}
