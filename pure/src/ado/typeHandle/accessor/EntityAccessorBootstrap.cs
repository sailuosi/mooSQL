using System;

namespace mooSQL.data
{
    /// <summary>
    /// 写侧 Accessor 安装器入口；实现委托 <see cref="GeneratedClientBootstrap"/>（Client 中心，非查找表）。
    /// </summary>
    public static class EntityAccessorBootstrap
    {
        /// <inheritdoc cref="GeneratedClientBootstrap.Add"/>
        public static void Add(Action<MooClient> install) => GeneratedClientBootstrap.Add(install);

        /// <inheritdoc cref="GeneratedClientBootstrap.Apply"/>
        public static void Apply(MooClient client) => GeneratedClientBootstrap.Apply(client);

        /// <inheritdoc cref="GeneratedClientBootstrap.Clear"/>
        public static void Clear() => GeneratedClientBootstrap.Clear();
    }
}
