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
    /// toXxx SQL 快照：仅比对 SQL 文本，不执行。
    /// 基准文件：baselines.sqlite.json；新增用例缺键时自动写入；全量重写请跑 CaptureAllBaselines。
    /// </summary>
    public static class SqlSnap
    {
        public const string Seed = "s_";

        private static readonly object Gate = new();
        private static Dictionary<string, string>? _sql;
        private static bool _dirty;

        public static string BaselinePath =>
            Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..",
                "src", "TestPure", "SqlSnapshot", "baselines.sqlite.json"));

        public static bool ForceUpdate =>
            string.Equals(Environment.GetEnvironmentVariable("UPDATE_SQL_BASELINES"), "1", StringComparison.Ordinal);

        public static SQLBuilder Kit(DataBaseType dbType = DataBaseType.SQLite)
        {
            var kit = TestDatabaseHelper.CreateSQLBuilder(dbType);
            kit.setSeed(Seed);
            return kit;
        }

        public static string Export(SQLBuilder kit, string toXxx)
        {
            SQLCmd cmd = toXxx switch
            {
                "toSelect" => kit.toSelect(),
                "toSelectCount" => kit.toSelectCount(),
                "toSelectExist" => kit.toSelectExist(),
                "toInsert" => kit.toInsert(),
                "toInsertFrom" => kit.toInsertFrom(),
                "toInsertWithDuplicateUpdate" => kit.toInsertWithDuplicateUpdate("ON DUPLICATE KEY UPDATE"),
                "toUpdate" => kit.toUpdate(),
                "toUpdateFrom" => kit.toUpdateFrom(),
                "toDelete" => kit.toDelete(),
                "toMergeInto" => kit.toMergeInto(),
                _ => throw new ArgumentOutOfRangeException(nameof(toXxx), toXxx, "未知出口")
            };
            if (cmd == null) return "";
            // 快照比对可执行形态：与 prepare 一致先 Resolve 延迟参数
            if (cmd.para != null)
                return cmd.para.ResolveDelayParas(cmd.sql);
            return cmd.sql ?? "";
        }

        public static void AssertSql(string caseName, Action<SQLBuilder> build, string toXxx = "toSelect",
            DataBaseType dbType = DataBaseType.SQLite)
        {
            using var kit = Kit(dbType);
            build(kit);
            AssertOrRecord(caseName, Export(kit, toXxx));
        }

        public static void AssertMerge(string caseName, Func<SQLBuilder, SQLCmd> build,
            DataBaseType dbType = DataBaseType.MSSQL)
        {
            using var kit = Kit(dbType);
            var cmd = build(kit);
            if (cmd == null)
            {
                AssertOrRecord(caseName, "");
                return;
            }
            var sql = cmd.para != null ? cmd.para.ResolveDelayParas(cmd.sql) : (cmd.sql ?? "");
            AssertOrRecord(caseName, sql);
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
