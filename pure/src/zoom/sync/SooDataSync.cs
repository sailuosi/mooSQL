///////////////////////////////////////////////////////////
//  SooDataSync.cs
//  门面工厂（配置 → build → Start）
///////////////////////////////////////////////////////////

using System;

namespace mooSQL.data
{
    /// <summary>
    /// 库表数据同步入口（命名空间 <c>mooSQL.data</c>；物理目录 <c>pure/src/zoom/sync</c>）。
    /// Pure 层只接受 <see cref="DBInstance"/>；连接位 index 由业务侧继承本类后自行解析。
    /// </summary>
    public class SooDataSync
    {
        /// <summary>
        /// 创建构建器（源/目标实例直接传入）。
        /// </summary>
        /// <param name="source">源库实例。</param>
        /// <param name="target">目标库实例。</param>
        /// <param name="batchSize">Host 默认批大小。</param>
        public static SooDataSyncBuilder Create(
            DBInstance source,
            DBInstance target,
            int batchSize = 2000)
        {
            return new SooDataSync().Build(source, target, batchSize);
        }

        /// <summary>
        /// 实例入口，供业务子类复用或覆盖（例如先按连接位解析再调用）。
        /// </summary>
        protected virtual SooDataSyncBuilder Build(
            DBInstance source,
            DBInstance target,
            int batchSize = 2000)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            var opt = new SooDataSyncOptions
            {
                Source = source,
                Target = target,
                BatchSize = batchSize <= 0 ? 2000 : batchSize
            };
            return new SooDataSyncBuilder(opt);
        }
    }
}
