using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using mooSQL.data;
using mooSQL.data.cluster;
using mooSQL.data.model;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;

namespace mooSQL.Pure.Tests
{
    public class WriteFanoutExecutorTests : IDisposable
    {
        private readonly string _masterPath;
        private readonly string _slavePath;

        public WriteFanoutExecutorTests()
        {
            _masterPath = Path.Combine(Path.GetTempPath(), $"moo_dw_master_{Guid.NewGuid():N}.db");
            _slavePath = Path.Combine(Path.GetTempPath(), $"moo_dw_slave_{Guid.NewGuid():N}.db");
        }

        public void Dispose()
        {
            TryDelete(_masterPath);
            TryDelete(_slavePath);
        }

        [Fact]
        public void Fanout_writes_to_slave_with_independent_executor()
        {
            var client = new MooClient { dialectFactory = new DialectFactory() };
            var cash = new DBInsCash(client);
            client.CashHolder = cash;

            var master = AddSqlite(cash, 0, _masterPath);
            var slave = AddSqlite(cash, 100, _slavePath);
            EnsureDwTable(master);
            EnsureDwTable(slave);
            client.configureGroup(0, g => g.master(0).enableDualWrite(100));

            // 经 SQLBuilder 全路径：组级 DualWrite 自动 fan-out
            var kit = TestDatabaseHelper.UseSQL(master);
            var rows = kit.exeNonQuery(new SQLCmd(
                "INSERT INTO dw_probe(id, name) VALUES(1, 'dual')"));

            rows.Should().Be(1);
            CountRows(master).Should().Be(1, "主库应有数据");
            CountRows(slave).Should().Be(1, "从库应独立落库，不能再打到主库连接");
        }

        [Fact]
        public void Fanout_slave_survives_when_master_transaction_rolls_back()
        {
            var client = new MooClient { dialectFactory = new DialectFactory() };
            var cash = new DBInsCash(client);
            client.CashHolder = cash;

            var master = AddSqlite(cash, 0, _masterPath);
            var slave = AddSqlite(cash, 100, _slavePath);
            EnsureDwTable(master);
            EnsureDwTable(slave);

            var slaves = new List<DBInstance> { slave };
            var cmd = new SQLCmd("INSERT INTO dw_probe(id, name) VALUES(2, 'tx')");
            var exe = new DBExecutor(master);
            exe.beginTransaction();
            try
            {
                WriteFanoutExecutor.ExecuteNonQuery(cmd, master, slaves, exe, DualWriteErrorPolicy.MasterWins);
                CountRows(slave).Should().Be(1, "从库应已自动提交，不受主库事务影响");
                exe.Context.session.RollbackTransaction();
            }
            finally
            {
                exe.Dispose();
            }

            CountRows(master).Should().Be(0, "主库事务回滚后无数据");
            CountRows(slave).Should().Be(1, "从库数据仍在");
        }

        private static DBInstance AddSqlite(DBInsCash cash, int index, string path)
        {
            var cfg = new DataBase
            {
                dbType = DataBaseType.SQLite,
                DBConnectStr = $"Data Source={path}",
                index = index,
                name = $"dw_{index}"
            };
            cash.addDataBase(index, cfg);
            var db = cash.getInstance(index);
            db.client = cash.getClient();
            return db;
        }

        private static void EnsureDwTable(DBInstance db)
        {
            db.ExeNonQuery(new SQLCmd(@"
CREATE TABLE IF NOT EXISTS dw_probe (
  id INTEGER PRIMARY KEY,
  name TEXT NOT NULL
)"));
        }

        private static int CountRows(DBInstance db)
        {
            var dt = db.ExeQuery(new SQLCmd("SELECT COUNT(*) AS c FROM dw_probe"));
            return Convert.ToInt32(dt.Rows[0][0]);
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // ignore locked temp files
            }
        }
    }
}
