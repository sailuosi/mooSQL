using System;
using System.Collections.Generic;
using FluentAssertions;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// insertList 指定物理表名重载冒烟（个性化分表写入）。
    /// </summary>
    [Collection("SQLiteIntegration")]
    public class SQLBuilderInsertListTableSqliteTests : IClassFixture<SQLiteTestFixture>
    {
        const string ShardUserTable = "moo_t_user_202406";

        readonly SQLiteTestFixture _fx;

        public SQLBuilderInsertListTableSqliteTests(SQLiteTestFixture fixture)
        {
            _fx = fixture;
            if (!_fx.TableExists(SQLiteTestFixture.UserTable))
                _fx.CreateAllTables();
            EnsureShardUserTable();
            _fx.TruncateBusinessTables();
            _fx.ExecuteSql($"DELETE FROM {ShardUserTable}");
        }

        void EnsureShardUserTable()
        {
            if (_fx.TableExists(ShardUserTable))
                return;
            _fx.ExecuteSql($@"
CREATE TABLE {ShardUserTable} (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT NOT NULL,
  email TEXT,
  age INTEGER,
  created_at TEXT,
  is_active INTEGER NOT NULL DEFAULT 1
)");
        }

        [Fact]
        public void InsertList_WithTableName_WritesToSpecifiedPhysicalTable()
        {
            var now = DateTime.UtcNow;
            var list = new List<SQLiteTestUser>
            {
                new()
                {
                    Id = 9101,
                    Name = "ShardA",
                    Email = "a@shard.test",
                    Age = 20,
                    CreatedAt = now,
                    IsActive = true
                },
                new()
                {
                    Id = 9102,
                    Name = "ShardB",
                    Email = "b@shard.test",
                    Age = 21,
                    CreatedAt = now,
                    IsActive = true
                }
            };

            using var kit = TestDatabaseHelper.UseSQL(_fx.Db);
            var n = kit.insertList(list, ShardUserTable);
            n.Should().Be(2);

            var shardCount = kit.clear().from(ShardUserTable).count();
            var mainCount = kit.clear().from(SQLiteTestFixture.UserTable).count();
            shardCount.Should().Be(2);
            mainCount.Should().Be(0);
        }
    }
}
