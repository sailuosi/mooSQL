using System;
using System.IO;
using mooSQL.data;

namespace mooSQL.Pure.Tests.TestHelpers
{
    /// <summary>
    /// SooDataSync 双库夹具：源 / 目标各一临时 SQLite。
    /// </summary>
    public sealed class SooDataSyncTestFixture : IDisposable
    {
        public const string TableName = "moo_sync_row";
        public const string DstTableName = "moo_sync_dst";

        readonly string _srcPath;
        readonly string _dstPath;

        public DBInstance Source { get; }
        public DBInstance Target { get; }

        public SooDataSyncTestFixture()
        {
            _srcPath = Path.Combine(Path.GetTempPath(), $"moo_sync_src_{Guid.NewGuid():N}.db");
            _dstPath = Path.Combine(Path.GetTempPath(), $"moo_sync_dst_{Guid.NewGuid():N}.db");
            Source = TestDatabaseHelper.CreateTestDBInstance(
                DataBaseType.SQLite, "Data Source=" + _srcPath + ";Mode=ReadWriteCreate");
            Target = TestDatabaseHelper.CreateTestDBInstance(
                DataBaseType.SQLite, "Data Source=" + _dstPath + ";Mode=ReadWriteCreate");
            EnsureAllTables();
        }

        /// <summary>每测前：确保表存在并清空。</summary>
        public void Reset()
        {
            EnsureAllTables();
            Clear(Source);
            Clear(Target);
        }

        public void Clear(DBInstance db)
        {
            if (TableExists(db, TableName))
                db.useSQL().from(TableName).where("1=1").doDelete();
            if (TableExists(db, DstTableName))
                db.useSQL().from(DstTableName).where("1=1").doDelete();
        }

        public void DropTargetTables()
        {
            DropIfExists(Target, TableName);
            DropIfExists(Target, DstTableName);
        }

        public void EnsureAllTables()
        {
            CreateRowTable(Source, TableName);
            CreateRowTable(Source, DstTableName);
            CreateRowTable(Target, TableName);
            CreateRowTable(Target, DstTableName);
        }

        public int Count(DBInstance db, string table = TableName)
        {
            if (!TableExists(db, table))
                return 0;
            return db.useSQL().from(table).count();
        }

        public void SeedSource(params SooDataSyncTestRow[] rows)
        {
            if (rows == null || rows.Length == 0)
                return;
            Source.useRepo<SooDataSyncTestRow>().InsertRange(rows);
        }

        public void SeedTarget(params SooDataSyncTestRow[] rows)
        {
            if (rows == null || rows.Length == 0)
                return;
            Target.useRepo<SooDataSyncTestRow>().InsertRange(rows);
        }

        static void CreateRowTable(DBInstance db, string name)
        {
            if (TableExists(db, name))
                return;
            db.ExeNonQuery(new SQLCmd($@"
CREATE TABLE {name} (
  id INTEGER PRIMARY KEY,
  title TEXT NOT NULL,
  modified_at TEXT NOT NULL,
  flag INTEGER NOT NULL DEFAULT 0,
  synced_at TEXT
)"), null!);
        }

        static bool TableExists(DBInstance db, string tableName)
        {
            var safe = tableName.Replace("'", "''");
            return db.ExeQueryScalar<int>(
                $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{safe}'", null!) > 0;
        }

        static void DropIfExists(DBInstance db, string tableName)
        {
            if (TableExists(db, tableName))
                db.useDDL().clear().setTable(tableName).doDropTable();
        }

        public void Dispose()
        {
            try
            {
                DropIfExists(Source, TableName);
                DropIfExists(Source, DstTableName);
                DropIfExists(Target, TableName);
                DropIfExists(Target, DstTableName);
            }
            catch { /* ignore */ }

            TryDelete(_srcPath);
            TryDelete(_dstPath);
        }

        static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch { /* ignore */ }
        }
    }

    [SooTable(SooDataSyncTestFixture.TableName)]
    public class SooDataSyncTestRow
    {
        [SooColumn("id", IsPrimaryKey = true)]
        public int Id { get; set; }

        [SooColumn("title")]
        public string Title { get; set; } = string.Empty;

        [SooColumn("modified_at")]
        public DateTime ModifiedAt { get; set; }

        [SooColumn("flag")]
        public int Flag { get; set; }

        [SooColumn("synced_at")]
        public DateTime? SyncedAt { get; set; }
    }

    /// <summary>异构目标实体（同结构不同表名）。</summary>
    [SooTable(SooDataSyncTestFixture.DstTableName)]
    public class SooDataSyncTestDstRow
    {
        [SooColumn("id", IsPrimaryKey = true)]
        public int Id { get; set; }

        [SooColumn("title")]
        public string Title { get; set; } = string.Empty;

        [SooColumn("modified_at")]
        public DateTime ModifiedAt { get; set; }

        [SooColumn("flag")]
        public int Flag { get; set; }

        [SooColumn("synced_at")]
        public DateTime? SyncedAt { get; set; }
    }
}
