using System;
using System.Collections.Generic;
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
    /// </summary>
    public static class DbTestConfig
    {
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

        /// <summary>默认对比模式，便于日常复测。</summary>
        public static DbTestScope Scope { get; set; } = DbTestScope.Compare;

        public static bool Allow(Type providerType)
        {
            if (providerType == null)
                return false;
            if (Scope == DbTestScope.Full)
                return true;
            return CompareProviders.Contains(providerType.Name);
        }

        /// <summary>
        /// 解析命令行 / 环境变量。支持：
        /// <c>compare</c> / <c>full</c>；或 <c>DBTEST_SCOPE=Compare|Full</c>。
        /// </summary>
        /// <returns>是否已由参数/环境变量显式指定范围（显式时跳过交互菜单）。</returns>
        public static bool ApplyArgs(string[] args)
        {
            var explicitSet = false;
            var env = Environment.GetEnvironmentVariable("DBTEST_SCOPE");
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
        /// 输入重定向或已由参数指定时跳过。
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
    }
}
