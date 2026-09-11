using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;

namespace mooSQL.Pure.Tests.SqlSnapshot
{
    /// <summary>
    /// SQLClip 对齐 API 的 toSelect SQL 快照。基准：baselines.clip.sqlite.json。
    /// </summary>
    public class SQLClipSqlSnapshotTests : IDisposable
    {
        public static IEnumerable<object[]> CaseNames()
            => SQLClipSqlSnapshotCatalog.All().Select(c => new object[] { c.Name });

        [PrepareOnlyTheory]
        [MemberData(nameof(CaseNames))]
        public void Snapshot_ToSelect(string name)
        {
            var c = SQLClipSqlSnapshotCatalog.All().First(x => x.Name == name);
            ClipSqlSnap.AssertSql(c.Name, c.Build);
        }

        [PrepareOnlyFact]
        public void CaptureAllBaselines()
        {
            var rows = new List<(string, string)>();
            foreach (var c in SQLClipSqlSnapshotCatalog.All())
            {
                var clip = ClipSqlSnap.Clip();
                c.Build(clip);
                rows.Add((c.Name, ClipSqlSnap.Export(clip)));
            }
            ClipSqlSnap.WriteAll(rows);
            Assert.True(System.IO.File.Exists(ClipSqlSnap.BaselinePath), ClipSqlSnap.BaselinePath);
            Assert.Equal(rows.Count, rows.Select(r => r.Item1).Distinct().Count());
        }

        public void Dispose() => ClipSqlSnap.FlushIfDirty();
    }
}
