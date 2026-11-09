using System;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// 源生成 Accessor 安装器清单（非查找表）。ModuleInitializer 仅 Add；
    /// 由 <see cref="MooClient.RegisterGeneratedAccessors"/> 装入具体 Client。
    /// </summary>
    public static class EntityAccessorBootstrap
    {
        private static readonly object _gate = new object();
        private static readonly List<Action<MooClient>> _installers = new List<Action<MooClient>>();

        /// <summary>
        /// 注册一条「装到 Client」的安装器（SG ModuleInitializer 调用）。
        /// </summary>
        public static void Add(Action<MooClient> install)
        {
            if (install is null) throw new ArgumentNullException(nameof(install));
            lock (_gate)
            {
                _installers.Add(install);
            }
        }

        /// <summary>
        /// 将已登记的安装器全部应用到指定 Client。
        /// </summary>
        public static void Apply(MooClient client)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            Action<MooClient>[] snapshot;
            lock (_gate)
            {
                snapshot = _installers.ToArray();
            }
            foreach (var install in snapshot)
                install(client);
        }

        /// <summary>
        /// 清除安装器清单（测试隔离）。
        /// </summary>
        public static void Clear()
        {
            lock (_gate)
            {
                _installers.Clear();
            }
        }
    }
}
