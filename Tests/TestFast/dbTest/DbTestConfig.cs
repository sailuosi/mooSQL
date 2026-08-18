using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dbTest.items;

namespace dbTest
{
    /// <summary>
    /// 适配器范围：全面（全部 ITest）或对比（仅固定对比组）。
    /// </summary>
    public enum DbTestScope
    {
        /// <summary>仅对比组（默认）：SQLBuilder + ADO.NET + Dapper + CRL + Chloe。</summary>
        Compare = 0,
        /// <summary>发现到的全部 ITest 适配器。</summary>
        Full = 1,
    }

    /// <summary>
    /// dbTest 运行配置。须在 <see cref="TestBase"/> 构造 / BenchmarkRunner 之前设定。
    /// <para>
    /// BenchmarkDotNet 默认 out-of-process：子进程不会跑 <c>Main</c>，也收不到交互菜单结果，
    /// 且不一定继承宿主 <c>Environment.SetEnvironmentVariable</c>。
    /// 因此范围会同时写入：
    /// 1) 环境变量 <c>DBTEST_SCOPE</c>（供 BDN Job.WithEnvironmentVariable 显式传入）
    /// 2) 临时文件 <see cref="ScopeFilePath"/>（跨进程可靠回退）
    /// </para>
    /// </summary>
    public static class DbTestConfig
    {
        public const string ScopeEnvName = "DBTEST_SCOPE";

        /// <summary>
        /// 与 SQLite 测试库同目录约定：%TEMP%/mooSQL_dbTest_scope.txt
        /// </summary>
        public static string ScopeFilePath { get; } =
            Path.Combine(Path.GetTempPath(), "mooSQL_dbTest_scope.txt");

        /// <summary>
        /// 对比组：SQLBuilder（Builder）+ ADO.NET + Dapper + CRL + Chloe。
        /// </summary>
        public static readonly string[] CompareProviders =
        {
            nameof(MooSqlBuilderTest),
            nameof(AdoNetTest),
            nameof(DapperTest),
            nameof(CrlTest),
            nameof(ChloeTest),
        };

        private static DbTestScope _scope = DbTestScope.Compare;
        private static bool _loaded;

        static DbTestConfig()
        {
            TryLoadPersisted();
        }

        /// <summary>
        /// 当前范围。赋值时持久化到环境变量与临时文件，供 BDN 子进程读回。
        /// </summary>
        public static DbTestScope Scope
        {
            get
            {
                EnsureLoaded();
                return _scope;
            }
            set
            {
                _scope = value;
                _loaded = true;
                Persist(value);
            }
        }

        public static bool Allow(Type providerType)
        {
            if (providerType == null)
                return false;
            if (Scope == DbTestScope.Full)
                return true;
            return CompareProviders.Contains(providerType.Name);
        }

        /// <summary>
        /// 强制把当前 Scope 再写一遍（Benchmark 启动前调用）。
        /// </summary>
        public static void PersistCurrent()
        {
            Persist(Scope);
        }

        /// <summary>
        /// 解析命令行 / 环境变量。支持：
        /// <c>compare</c> / <c>full</c>；或 <c>DBTEST_SCOPE=Compare|Full</c>。
        /// 临时文件仅供 BDN 子进程回退，不作为「跳过交互菜单」的依据。
        /// </summary>
        public static bool ApplyArgs(string[] args)
        {
            var explicitSet = false;

            // 宿主：仅环境变量视为显式配置（CI / set DBTEST_SCOPE=Full）
            var env = Environment.GetEnvironmentVariable(ScopeEnvName);
            if (!string.IsNullOrWhiteSpace(env)
                && Enum.TryParse(env.Trim(), ignoreCase: true, out DbTestScope fromEnv))
            {
                Scope = fromEnv;
                explicitSet = true;
            }

            if (args == null)
                return explicitSet;

            foreach (var raw in args)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;
                var a = raw.Trim();
                if (a.StartsWith("--", StringComparison.Ordinal))
                    a = a.Substring(2);

                if (string.Equals(a, "compare", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(a, "cmp", StringComparison.OrdinalIgnoreCase))
                {
                    Scope = DbTestScope.Compare;
                    return true;
                }
                if (string.Equals(a, "full", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(a, "all", StringComparison.OrdinalIgnoreCase))
                {
                    Scope = DbTestScope.Full;
                    return true;
                }
                if (a.StartsWith("scope=", StringComparison.OrdinalIgnoreCase)
                    && Enum.TryParse(a.Substring("scope=".Length).Trim(), ignoreCase: true, out DbTestScope fromArg))
                {
                    Scope = fromArg;
                    return true;
                }
            }

            return explicitSet;
        }

        /// <summary>
        /// 控制台输入数字选择范围：1=对比组，2=全部。回车默认对比组。
        /// </summary>
        public static void PromptScope(bool skipIfExplicit)
        {
            if (skipIfExplicit)
                return;
            if (Console.IsInputRedirected)
                return;

            Console.WriteLine();
            Console.WriteLine("选择适配器范围：");
            Console.WriteLine("  1 = 对比组（SQLBuilder + ADO.NET + Dapper + CRL + Chloe）[默认]");
            Console.WriteLine("  2 = 全部 ITest 适配器");
            Console.Write("请输入数字 (1/2，回车=1): ");

            while (true)
            {
                var line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                {
                    Scope = DbTestScope.Compare;
                    break;
                }

                var s = line.Trim();
                if (s == "1" || string.Equals(s, "compare", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(s, "cmp", StringComparison.OrdinalIgnoreCase))
                {
                    Scope = DbTestScope.Compare;
                    break;
                }
                if (s == "2" || string.Equals(s, "full", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(s, "all", StringComparison.OrdinalIgnoreCase))
                {
                    Scope = DbTestScope.Full;
                    break;
                }

                Console.Write("无效输入，请输入 1 或 2: ");
            }
        }

        public static string Describe()
        {
            if (Scope == DbTestScope.Full)
                return "Scope=Full (all ITest providers)";
            return "Scope=Compare [" + string.Join(", ", CompareProviders) + "]";
        }

        private static void EnsureLoaded()
        {
            if (_loaded)
                return;
            TryLoadPersisted();
        }

        private static void TryLoadPersisted()
        {
            var env = Environment.GetEnvironmentVariable(ScopeEnvName);
            if (!string.IsNullOrWhiteSpace(env)
                && Enum.TryParse(env.Trim(), ignoreCase: true, out DbTestScope fromEnv))
            {
                _scope = fromEnv;
                _loaded = true;
                return;
            }

            try
            {
                if (File.Exists(ScopeFilePath))
                {
                    var text = File.ReadAllText(ScopeFilePath).Trim();
                    if (Enum.TryParse(text, ignoreCase: true, out DbTestScope fromFile))
                        _scope = fromFile;
                }
            }
            catch
            {
                // ignore corrupt / locked file
            }

            _loaded = true;
        }

        private static void Persist(DbTestScope value)
        {
            var text = value.ToString();
            Environment.SetEnvironmentVariable(ScopeEnvName, text);
            try
            {
                File.WriteAllText(ScopeFilePath, text);
            }
            catch
            {
                // 子进程仍可能靠 Job 环境变量；写文件失败不阻断宿主
            }
        }
    }
}
