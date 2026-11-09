using System;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// 源生成「装到 Client」安装器清单（非查找表）。
    /// ModuleInitializer 仅 <see cref="Add"/>；由 <see cref="MooClient.RegisterGeneratedMaterializers"/> /
    /// <see cref="MooClient.RegisterGeneratedAccessors"/> 执行 <see cref="Apply"/>。
    /// </summary>
    public static class GeneratedClientBootstrap
    {
        private static readonly object _gate = new object();
        private static readonly List<Action<MooClient>> _installers = new List<Action<MooClient>>();

        /// <summary>
        /// 登记一条安装器（在目标 Client 上 RegisterMaterializer / RegisterAccessor 等）。
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
        /// 将已登记安装器全部应用到指定 Client。
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
