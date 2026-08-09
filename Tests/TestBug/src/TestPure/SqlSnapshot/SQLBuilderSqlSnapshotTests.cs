using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace mooSQL.Pure.Tests.SqlSnapshot
{
    /// <summary>
    /// SQLBuilder 全面 toXxx SQL 快照。基准：baselines.sqlite.json。
    /// 全量固化：UPDATE_SQL_BASELINES=1 dotnet test --filter CaptureAllBaselines
    /// 单条新增缺键时会自动写入基准文件。
    /// </summary>
    public class SQLBuilderSqlSnapshotTests : IDisposable
    {
        public static IEnumerable<object[]> CaseNames()
            => SQLBuilderSqlSnapshotCatalog.All().Select(c => new object[] { c.Name });

        public static IEnumerable<object[]> MergeNames()
            => SQLBuilderSqlSnapshotCatalog.Merges().Select(c => new object[] { c.Name });

        [Theory]
        [MemberData(nameof(CaseNames))]
        public void Snapshot_ToXxx(string name)
        {
            var c = SQLBuilderSqlSnapshotCatalog.All().First(x => x.Name == name);
            SqlSnap.AssertSql(c.Name, c.Build, c.ToXxx, c.DbType);
        }

        [Theory]
        [MemberData(nameof(MergeNames))]
        public void Snapshot_MergeInto(string name)
        {
            var c = SQLBuilderSqlSnapshotCatalog.Merges().First(x => x.Name == name);
            SqlSnap.AssertMerge(c.Name, c.Build, c.DbType);
        }

        [Fact]
        public void CaptureAllBaselines()
        {
            var rows = new List<(string, string)>();
            foreach (var c in SQLBuilderSqlSnapshotCatalog.All())
            {
                using var kit = SqlSnap.Kit(c.DbType);
                c.Build(kit);
                rows.Add((c.Name, SqlSnap.Export(kit, c.ToXxx)));
            }
            foreach (var c in SQLBuilderSqlSnapshotCatalog.Merges())
            {
                using var kit = SqlSnap.Kit(c.DbType);
                rows.Add((c.Name, c.Build(kit)?.sql ?? ""));
            }
            SqlSnap.WriteAll(rows);
            Assert.True(System.IO.File.Exists(SqlSnap.BaselinePath), SqlSnap.BaselinePath);
            Assert.Equal(rows.Count, rows.Select(r => r.Item1).Distinct().Count());
        }

        public void Dispose() => SqlSnap.FlushIfDirty();
    }
}
