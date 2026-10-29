///////////////////////////////////////////////////////////
//  SooDataSyncBuilder.cs
///////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>链式配置；build() 得到 Host，配置期不执行同步。</summary>
    public sealed class SooDataSyncBuilder
    {
        readonly SooDataSyncOptions _options;
        readonly List<SyncTableJob> _jobs = new List<SyncTableJob>();

        internal SooDataSyncBuilder(SooDataSyncOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>目标缺表则 EnsureSchema。</summary>
        public SooDataSyncBuilder ensureSchema(bool enable = true)
        {
            _options.EnsureTargetSchema = enable;
            return this;
        }

        /// <summary>结构同步模式。</summary>
        public SooDataSyncBuilder schemaMode(SyncMode mode)
        {
            _options.SchemaMode = mode;
            return this;
        }

        /// <summary>默认行策略。</summary>
        public SooDataSyncBuilder mode(SooDataSyncMode mode)
        {
            _options.DefaultMode = mode;
            return this;
        }

        /// <summary>是否 Bulk。</summary>
        public SooDataSyncBuilder useBulk(bool enable = true)
        {
            _options.UseBulk = enable;
            return this;
        }

        /// <summary>Host 默认批大小。</summary>
        public SooDataSyncBuilder batchSize(int size)
        {
            _options.BatchSize = size;
            return this;
        }

        /// <summary>批间歇。</summary>
        public SooDataSyncBuilder batchInterval(TimeSpan interval)
        {
            _options.BatchInterval = interval;
            return this;
        }

        /// <summary>轮次间歇（every）。</summary>
        public SooDataSyncBuilder every(TimeSpan interval)
        {
            _options.HostInterval = interval;
            return this;
        }

        /// <summary>表间并行度。</summary>
        public SooDataSyncBuilder parallelTables(int degree)
        {
            _options.MaxDegreeOfParallelism = degree <= 0 ? 1 : degree;
            return this;
        }

        /// <summary>失败策略。</summary>
        public SooDataSyncBuilder failureAction(SooDataSyncFailureAction action)
        {
            _options.FailureAction = action;
            return this;
        }

        /// <summary>允许 ReplaceFull。</summary>
        public SooDataSyncBuilder allowDestructiveReplace(bool allow = true)
        {
            _options.AllowDestructiveReplace = allow;
            return this;
        }

        /// <summary>进度回调（对齐 SQLBuilder.print）。</summary>
        public SooDataSyncBuilder print(Action<string> onPrint)
        {
            _options.OnPrint = onPrint;
            return this;
        }

        /// <summary>同构表：orderBy 时序列。</summary>
        public SooDataSyncBuilder syncTable<T>(string orderByColumn) where T : class, new()
        {
            return syncTable<T, T>(cfg =>
            {
                cfg.orderBy(orderByColumn);
            });
        }

        /// <summary>同构表：配置委托。</summary>
        public SooDataSyncBuilder syncTable<T>(Action<SyncTableJobConfigurator<T, T>> configure)
            where T : class, new()
        {
            return syncTable<T, T>(configure);
        }

        /// <summary>异构表。</summary>
        public SooDataSyncBuilder syncTable<TSrc, TDst>(Action<SyncTableJobConfigurator<TSrc, TDst>> configure)
            where TSrc : class, new()
            where TDst : class, new()
        {
            var job = new SyncTableJob<TSrc, TDst>();
            var cfg = new SyncTableJobConfigurator<TSrc, TDst>(job);
            configure?.Invoke(cfg);
            if (typeof(TSrc) != typeof(TDst) && job.Map == null && job.MapBatch == null)
            {
                throw new InvalidOperationException(
                    "异构 " + typeof(TSrc).Name + " → " + typeof(TDst).Name + " 必须提供 map/mapBatch");
            }
            _jobs.Add(job);
            return this;
        }

        /// <summary>结束配置，得到可 Start 的 Host。</summary>
        public SooDataSyncHost build()
        {
            if (_options.Source == null)
                throw new InvalidOperationException("SooDataSync 未设置源 DBInstance");
            if (_options.Target == null)
                throw new InvalidOperationException("SooDataSync 未设置目标 DBInstance");
            return new SooDataSyncHost(_options, new List<SyncTableJob>(_jobs));
        }
    }

    /// <summary>单表链式配置。</summary>
    public sealed class SyncTableJobConfigurator<TSrc, TDst>
        where TSrc : class, new()
        where TDst : class, new()
    {
        readonly SyncTableJob<TSrc, TDst> _job;

        internal SyncTableJobConfigurator(SyncTableJob<TSrc, TDst> job)
        {
            _job = job;
        }

        /// <summary>源侧时序列（模式 1 水位拉源）。</summary>
        public SyncTableJobConfigurator<TSrc, TDst> orderBy(string column)
        {
            _job.OrderByColumn = column;
            return this;
        }

        /// <summary>目标水位列；空则同 orderBy。</summary>
        public SyncTableJobConfigurator<TSrc, TDst> watermarkOnTarget(string column)
        {
            _job.WatermarkColumnOnTarget = column;
            return this;
        }

        /// <summary>Upsert 用修改时间列；空则同 orderBy。</summary>
        public SyncTableJobConfigurator<TSrc, TDst> modified(string column)
        {
            _job.ModifiedColumn = column;
            return this;
        }

        /// <summary>显式主键列/属性名（覆盖实体主键标注）。</summary>
        public SyncTableJobConfigurator<TSrc, TDst> key(string column)
        {
            _job.KeyColumn = column;
            return this;
        }

        /// <summary>本表行同步策略；空则用 Host 默认。</summary>
        public SyncTableJobConfigurator<TSrc, TDst> mode(SooDataSyncMode mode)
        {
            _job.Mode = mode;
            return this;
        }

        /// <summary>本表批/页大小；覆盖 Host 默认。</summary>
        public SyncTableJobConfigurator<TSrc, TDst> batchSize(int size)
        {
            _job.BatchSize = size;
            return this;
        }

        /// <summary>本表批失败策略；覆盖 Host 默认。</summary>
        public SyncTableJobConfigurator<TSrc, TDst> failureAction(SooDataSyncFailureAction action)
        {
            _job.FailureAction = action;
            return this;
        }

        /// <summary>本表源库实例；覆盖 Host 默认。</summary>
        public SyncTableJobConfigurator<TSrc, TDst> source(DBInstance db)
        {
            _job.Source = db ?? throw new ArgumentNullException(nameof(db));
            return this;
        }

        /// <summary>本表目标库实例；覆盖 Host 默认。</summary>
        public SyncTableJobConfigurator<TSrc, TDst> target(DBInstance db)
        {
            _job.Target = db ?? throw new ArgumentNullException(nameof(db));
            return this;
        }

        /// <summary>自定义拉源 SQL（模式 2）；框架叠加翻页。</summary>
        public SyncTableJobConfigurator<TSrc, TDst> fromSql(Action<SQLBuilder> build)
        {
            _job.FromSql = build;
            return this;
        }

        /// <summary>源查询追加条件（模式 1/3）。</summary>
        public SyncTableJobConfigurator<TSrc, TDst> onSourceQuery(Action<SQLBuilder> build)
        {
            _job.OnSourceQuery = build;
            return this;
        }

        /// <summary>ReplaceFull 目标清空（from 已设；须补 where）。</summary>
        public SyncTableJobConfigurator<TSrc, TDst> onTargetClear(Action<SQLBuilder> build)
        {
            _job.OnTargetClear = build;
            return this;
        }

        /// <summary>
        /// 目标侧条件（MirrorKeys 扫删）。
        /// 若已配 onSourceQuery 则必须同时配置本项，否则跳过本表以防误删。
        /// </summary>
        public SyncTableJobConfigurator<TSrc, TDst> onTargetQuery(Action<SQLBuilder> build)
        {
            _job.OnTargetQuery = build;
            return this;
        }

        /// <summary>行映射；异构必填，同构可省略（Identity）。</summary>
        public SyncTableJobConfigurator<TSrc, TDst> map(Func<TSrc, TDst> map)
        {
            _job.Map = map;
            return this;
        }

        /// <summary>批映射；与 map 二选一或并用时优先本项。</summary>
        public SyncTableJobConfigurator<TSrc, TDst> mapBatch(Func<IReadOnlyList<TSrc>, IReadOnlyList<TDst>> mapBatch)
        {
            _job.MapBatch = mapBatch;
            return this;
        }

        /// <summary>写入前对目标行赋值。</summary>
        public SyncTableJobConfigurator<TSrc, TDst> onAssign(Action<TDst> assign)
        {
            _job.OnAssign = assign;
            return this;
        }

        /// <summary>写入前对目标行赋值（带对应源行）。</summary>
        public SyncTableJobConfigurator<TSrc, TDst> onAssign(Action<TDst, TSrc> assign)
        {
            _job.OnAssignWithSource = assign;
            return this;
        }

        /// <summary>写入前对整批目标行赋值。</summary>
        public SyncTableJobConfigurator<TSrc, TDst> onAssignBatch(Action<IList<TDst>> assign)
        {
            _job.OnAssignBatch = assign;
            return this;
        }
    }
}
