using System.Diagnostics;
using LiteOrm.Common;
using LiteOrm.Service;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using mooSQL.data;

namespace LiteOrm.Benchmark
{
    /// <summary>
    /// 轻量对照：仅 LiteOrm vs mooSQL，SQLite，固定条数 Update（默认 500）。
    /// 用法：<c>dotnet run -- --quick-update [count] [rounds]</c>
    /// </summary>
    public static class QuickUpdateCompare
    {
        public static async Task RunAsync(string[] args)
        {
            var count = 500;
            var rounds = 8;
            var warmup = 2;
            if (args.Length > 1 && int.TryParse(args[1], out var c) && c > 0) count = c;
            if (args.Length > 2 && int.TryParse(args[2], out var r) && r > 0) rounds = r;

            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var liteSection = config.GetSection("LiteOrm");
            string? cs = null;
            string? provider = null;
            foreach (var ds in liteSection.GetSection("DataSources").GetChildren())
            {
                if (!string.Equals(ds["Name"], "SQLite", StringComparison.OrdinalIgnoreCase))
                    continue;
                cs = ds["ConnectionString"];
                provider = ds["Name"];
                break;
            }
            cs ??= config.GetConnectionString("SQLite") ?? "Data Source=benchmark-quick.db";
            provider ??= "SQLite";

            // 独立绝对路径文件，避免 cwd / 全量基准库互相污染
            var dbPath = Path.Combine(AppContext.BaseDirectory, "benchmark-quick-update.db");
            cs = "Data Source=" + dbPath;

            Console.WriteLine($"QuickUpdateCompare: provider={provider}, n={count}, rounds={rounds}, warmup={warmup}");
            Console.WriteLine($"Connection: {cs}");

            EnsureSqliteSchema(cs);
            MooSqlDb.EnsureInit(cs, provider);

            using var host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((_, cfg) =>
                {
                    cfg.SetBasePath(AppContext.BaseDirectory);
                    cfg.AddJsonFile("appsettings.json", optional: false);
                    // 强制 Default=SQLite，并指向本对照库文件
                    cfg.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["LiteOrm:Default"] = "SQLite",
                        ["LiteOrm:DataSources:0:Name"] = "SQLite",
                        ["LiteOrm:DataSources:0:ConnectionString"] = cs,
                        ["LiteOrm:DataSources:0:Provider"] = "Microsoft.Data.Sqlite.SqliteConnection, Microsoft.Data.Sqlite",
                        ["LiteOrm:DataSources:0:SyncTable"] = "true",
                    });
                })
                .ConfigureLogging(logging =>
                {
                    foreach (var p in logging.Services.Where(x => x.ServiceType.Name.Contains("LoggerProvider")).ToList())
                        logging.Services.Remove(p);
                })
                .ConfigureServices((_, services) => services.AddLiteOrm())
                .Build();

            await host.StartAsync();
            var sp = host.Services;
            var random = new Random(42);

            // --- seed LiteOrm ---
            await using (var scope = sp.CreateAsyncScope())
            {
                TruncateUsers(cs);
                var dao = scope.ServiceProvider.GetRequiredService<ObjectDAO<BenchmarkUser>>();
                var users = Enumerable.Range(1, count)
                    .Select(i => NewUser($"Lite{i}", 20 + (i % 40), $"l{i}@t.com"))
                    .ToList();
                await dao.BatchInsertAsync(users);
            }

            var liteMs = await TimeLiteOrmAsync(sp, count, warmup, rounds, random);

            // --- seed MooSql（用 Repo.InsertRange，与 UpdateRange 同源）---
            TruncateUsers(cs);
            {
                var users = Enumerable.Range(1, count)
                    .Select(i =>
                    {
                        var u = NewUser($"Moo{i}", 20 + (i % 40), $"m{i}@t.com");
                        u.Id = i; // 显式主键，避免 Identity=0 全行撞 UNIQUE
                        return u;
                    })
                    .ToList();
                var repo = MooSqlDb.Db.useRepo<BenchmarkUser>();
                repo.BatchOptions.InsertRowThreshold = 32;
                repo.BatchOptions.InsertBulkThreshold = 100000;
                var inserted = repo.InsertRange(users);
                Console.WriteLine($"MooSql seed InsertRange => {inserted}");
                var check = repo.GetList(count);
                Console.WriteLine($"MooSql seed GetList => {check?.Count ?? 0}");
                if (check == null || check.Count == 0)
                    throw new InvalidOperationException("MooSql seed empty after InsertRange.");
            }

            // 计时走 Repo.UpdateRange（U2 UpdateBatch；SQLite 为 CTE+VALUES）
            var mooMs = TimeMooSql(count, warmup, rounds, random);

            Console.WriteLine();
            Console.WriteLine("=== Update @ {0} (SQLite) ===", count);
            Console.WriteLine("LiteOrm  mean={0:F2} ms  (min={1:F2}, max={2:F2})", liteMs.Average(), liteMs.Min(), liteMs.Max());
            Console.WriteLine("MooSql   mean={0:F2} ms  (min={1:F2}, max={2:F2})", mooMs.Average(), mooMs.Min(), mooMs.Max());
            var ratio = liteMs.Average() > 0 ? mooMs.Average() / liteMs.Average() : 0;
            Console.WriteLine("Ratio Moo/Lite = {0:F2}×  ({1})", ratio, ratio < 1 ? "Moo faster" : "Lite faster");
            Console.WriteLine("Note: SQLite 上 mooSQL UpdateBatch = CTE+VALUES；LiteOrm 为其 CTE 批更新路径。");
        }

        static async Task<List<double>> TimeLiteOrmAsync(
            IServiceProvider sp, int count, int warmup, int rounds, Random random)
        {
            var times = new List<double>(rounds);
            for (var i = 0; i < warmup + rounds; i++)
            {
                using var scope = sp.CreateScope();
                var view = scope.ServiceProvider.GetRequiredService<IEntityViewServiceAsync<BenchmarkUser>>();
                var svc = scope.ServiceProvider.GetRequiredService<IEntityServiceAsync<BenchmarkUser>>();
                var sm = scope.ServiceProvider.GetRequiredService<SessionManager>();
                var users = await view.SearchAsync(new SectionExpr(0, count));
                foreach (var u in users)
                {
                    u.Name = "L" + Guid.NewGuid().ToString("N")[..8];
                    u.Age = random.Next(20, 60);
                    u.Email = Guid.NewGuid().ToString("N")[..8] + "@t.com";
                }

                var sw = Stopwatch.StartNew();
                await sm.ExecuteInTransactionAsync(async _ =>
                {
                    await svc.BatchUpdateAsync(users);
                });
                sw.Stop();
                if (i >= warmup)
                {
                    times.Add(sw.Elapsed.TotalMilliseconds);
                    Console.WriteLine($"  LiteOrm round {times.Count}/{rounds}: {sw.Elapsed.TotalMilliseconds:F2} ms");
                }
                else
                    Console.WriteLine($"  LiteOrm warmup {i + 1}/{warmup}: {sw.Elapsed.TotalMilliseconds:F2} ms");
            }
            return times;
        }

        static List<double> TimeMooSql(int count, int warmup, int rounds, Random random)
        {
            var times = new List<double>(rounds);
            for (var i = 0; i < warmup + rounds; i++)
            {
                var users = MooSqlDb.Db.useRepo<BenchmarkUser>().GetList(count);
                if (users == null || users.Count == 0)
                    throw new InvalidOperationException("MooSql seed empty.");
                foreach (var u in users)
                {
                    u.Name = "M" + Guid.NewGuid().ToString("N")[..8];
                    u.Age = random.Next(20, 60);
                    u.Email = Guid.NewGuid().ToString("N")[..8] + "@t.com";
                }

                var sw = Stopwatch.StartNew();
                var repo = MooSqlDb.Db.useRepo<BenchmarkUser>();
                repo.BatchOptions.UpdateRowThreshold = 32;
                repo.BatchOptions.UpdateBatchThreshold = 50;
                repo.UpdateRange(users);
                sw.Stop();
                if (i >= warmup)
                {
                    times.Add(sw.Elapsed.TotalMilliseconds);
                    Console.WriteLine($"  MooSql  round {times.Count}/{rounds}: {sw.Elapsed.TotalMilliseconds:F2} ms");
                }
                else
                    Console.WriteLine($"  MooSql  warmup {i + 1}/{warmup}: {sw.Elapsed.TotalMilliseconds:F2} ms");
            }
            return times;
        }

        static void EnsureSqliteSchema(string cs)
        {
            using var conn = new SqliteConnection(cs);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS BenchmarkUser (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  Name TEXT,
  Age INTEGER,
  Email TEXT,
  CreateTime TEXT,
  Uid TEXT,
  Salary TEXT,
  IsActive INTEGER,
  Score REAL,
  LoginCount INTEGER,
  Remark TEXT
);";
            cmd.ExecuteNonQuery();
        }

        static void TruncateUsers(string cs)
        {
            using var conn = new SqliteConnection(cs);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM BenchmarkUser; DELETE FROM sqlite_sequence WHERE name='BenchmarkUser';";
            try { cmd.ExecuteNonQuery(); }
            catch { /* sequence 表可能不存在 */ }
        }

        static BenchmarkUser NewUser(string name, int age, string email)
        {
            return new BenchmarkUser
            {
                Name = name,
                Age = age,
                Email = email,
                CreateTime = DateTime.Now,
                Uid = Guid.NewGuid(),
                Salary = age * 100m + 0.50m,
                IsActive = (age & 1) == 0,
                Score = age * 1.25,
                LoginCount = age * 7L,
                Remark = "quick-" + name
            };
        }
    }
}
