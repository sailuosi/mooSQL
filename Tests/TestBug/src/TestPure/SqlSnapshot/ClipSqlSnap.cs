using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using Stj = System.Text.Json;

namespace mooSQL.Pure.Tests.SqlSnapshot
{
    /// <summary>
    /// SQLClip toSelect SQL 快照辅助（独立基准文件，不与 Builder 混写）。
    /// </summary>
    public static class ClipSqlSnap
    {
        public const string Seed = "s_";

        private static readonly object Gate = new();
        private static Dictionary<string, string>? _sql;
        private static bool _dirty;

        public static string BaselinePath =>
            Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..",
                "src", "TestPure", "SqlSnapshot", "baselines.clip.sqlite.json"));

        public static bool ForceUpdate =>
            string.Equals(Environment.GetEnvironmentVariable("UPDATE_SQL_BASELINES"), "1", StringComparison.Ordinal);

        public static SQLClip Clip(DataBaseType dbType = DataBaseType.SQLite)
        {
            var clip = TestDatabaseHelper.CreateSQLClip(dbType);
            clip.Context.Builder.setSeed(Seed);
            return clip;
        }

        public static string Export(SQLClip clip)
        {
            var cmd = clip.toSelect();
            if (cmd == null) return "";
            if (cmd.para != null)
                return cmd.para.ResolveDelayParas(cmd.sql);
            return cmd.sql ?? "";
        }

        public static void AssertSql(string caseName, Action<SQLClip> build,
            DataBaseType dbType = DataBaseType.SQLite)
        {
            var clip = Clip(dbType);
            build(clip);
            AssertOrRecord(caseName, Export(clip));
        }

        public static void AssertOrRecord(string caseName, string actualSql)
        {
            lock (Gate)
            {
                EnsureLoaded();
                if (!ForceUpdate && _sql!.TryGetValue(caseName, out var expected))
                {
                    actualSql.Should().Be(expected,
                        "case={0}; 若当前输出即为新基准：UPDATE_SQL_BASELINES=1 后重跑，或执行 CaptureAllBaselines", caseName);
                    return;
                }

                _sql![caseName] = actualSql;
                _dirty = true;
            }
        }

        public static void WriteAll(IEnumerable<(string Name, string Sql)> rows)
        {
            lock (Gate)
            {
                _sql = rows.ToDictionary(r => r.Name, r => r.Sql, StringComparer.Ordinal);
                _dirty = true;
                FlushIfDirty();
            }
        }

        public static void FlushIfDirty()
        {
            lock (Gate)
            {
                if (!_dirty || _sql == null) return;
                var dir = Path.GetDirectoryName(BaselinePath)!;
                Directory.CreateDirectory(dir);
                var ordered = _sql.OrderBy(kv => kv.Key, StringComparer.Ordinal)
                    .ToDictionary(kv => kv.Key, kv => kv.Value);
                var json = Stj.JsonSerializer.Serialize(ordered, new Stj.JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                File.WriteAllText(BaselinePath, json);
                _dirty = false;
            }
        }

        private static void EnsureLoaded()
        {
            if (_sql != null) return;
            if (File.Exists(BaselinePath))
            {
                var json = File.ReadAllText(BaselinePath);
                _sql = Stj.JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                       ?? new Dictionary<string, string>(StringComparer.Ordinal);
            }
            else
            {
                _sql = new Dictionary<string, string>(StringComparer.Ordinal);
            }
        }
    }
}
