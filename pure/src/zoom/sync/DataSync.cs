///////////////////////////////////////////////////////////
//  DataSync.cs
//  门面工厂（配置 → build → Start）
///////////////////////////////////////////////////////////

using System;

namespace mooSQL.data
{
    /// <summary>
    /// 库表数据同步入口（命名空间 <c>mooSQL.data</c>；物理目录 <c>pure/src/zoom/sync</c>）。
    /// </summary>
    public static class DataSync
    {
        /// <summary>
        /// 创建构建器。
        /// </summary>
        /// <param name="client">持有 CashHolder 的客户端。</param>
        /// <param name="sourcePosition">默认源连接位。</param>
        /// <param name="targetPosition">默认目标连接位。</param>
        /// <param name="batchSize">Host 默认批大小。</param>
        public static DataSyncBuilder Create(
            MooClient client,
            int sourcePosition,
            int targetPosition,
            int batchSize = 2000)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            var opt = new DataSyncOptions
            {
                SourcePosition = sourcePosition,
                TargetPosition = targetPosition,
                BatchSize = batchSize <= 0 ? 2000 : batchSize
            };
            return new DataSyncBuilder(client, opt);
        }

        /// <summary>
        /// 使用已有 <see cref="DBInsCash"/>（取其 client）。
        /// </summary>
        public static DataSyncBuilder Create(
            DBInsCash cash,
            int sourcePosition,
            int targetPosition,
            int batchSize = 2000)
        {
            if (cash == null)
                throw new ArgumentNullException(nameof(cash));
            var client = cash.getClient();
            if (client.CashHolder == null)
                client.CashHolder = cash;
            return Create(client, sourcePosition, targetPosition, batchSize);
        }
    }
}
