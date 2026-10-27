using System;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
namespace dbTest.items
{
    public class DapperTest : ITest
    {
        public override void testQueryAnonymousResult()
        {
            using (var _connection = new SqliteConnection(sqlLiteDb))
            {
                _connection.Open();
                var sql = $"select * from TestEntity limit {listTake}";
                var list = _connection.Query(sql);
            }
        }

        public override string testQueryCondition()
        {
            return "";
        }

        public override void testQueryResult()
        {
            using (var _connection = new SqliteConnection(sqlLiteDb))
            {
                _connection.Open();
                var sql = $"select * from TestEntity limit {listTake}";
                var list = _connection.Query<TestEntity>(sql);
            }
        }
        public override void testQueryJoin()
        {

        }
        public override void testQueryLoop()
        {
            for (var i = 0; i < 20; i++)
            {
                using (var _connection = new SqliteConnection(sqlLiteDb))
                {
                    _connection.Open();
                    var sql = $"select * from TestEntity where id={i}";
                    var list = _connection.Query(sql);
                }
            }
        }
        public override void testInclude()
        {
           
        }

        public override void testBatchInsert(int count)
        {
            using var conn = new SqliteConnection(sqlLiteDb);
            conn.Open();
            using var tx = conn.BeginTransaction();
            SqliteWriteSql.BatchInsert(conn, TestEntityFactory.NewList("Dapper", count), tx);
            tx.Commit();
        }

        public override void testBatchUpdate(int count)
        {
            using var conn = new SqliteConnection(sqlLiteDb);
            conn.Open();
            var users = conn.Query<TestEntity>($"SELECT * FROM TestEntity LIMIT {count}").ToList();
            using var tx = conn.BeginTransaction();
            foreach (var u in users)
                TestEntityFactory.Mutate(u, "Dapper", writeRandom);
            SqliteWriteSql.BatchUpsert(conn, users, tx);
            tx.Commit();
        }

        public override void testBatchUpsert(int count)
        {
            using var conn = new SqliteConnection(sqlLiteDb);
            conn.Open();
            var half = count / 2;
            var existing = conn.Query<TestEntity>($"SELECT * FROM TestEntity LIMIT {half}").ToList();
            foreach (var u in existing)
                TestEntityFactory.Mutate(u, "D_Up", writeRandom);
            var neu = TestEntityFactory.NewList("D_In", count - half);
            using var tx = conn.BeginTransaction();
            if (existing.Count > 0)
                SqliteWriteSql.BatchUpsert(conn, existing, tx);
            SqliteWriteSql.BatchInsert(conn, neu, tx);
            tx.Commit();
        }

        public override void testSingleInsert(int loops)
        {
            using var conn = new SqliteConnection(sqlLiteDb);
            conn.Open();
            for (int i = 0; i < loops; i++)
                conn.Execute(SqliteWriteSql.SingleInsertSql, TestEntityFactory.New("D_SI", i));
        }

        public override void testSingleUpdate(int loops)
        {
            using var conn = new SqliteConnection(sqlLiteDb);
            conn.Open();
            var users = conn.Query<TestEntity>($"SELECT * FROM TestEntity LIMIT {loops}").ToList();
            if (users.Count == 0) return;
            for (int i = 0; i < loops; i++)
            {
                var u = users[i % users.Count];
                TestEntityFactory.Mutate(u, "D_SU", writeRandom);
                conn.Execute(SqliteWriteSql.SingleUpdateSql, u);
            }
        }

        public override void testSingleUpsert(int loops)
        {
            using var conn = new SqliteConnection(sqlLiteDb);
            conn.Open();
            var existing = conn.Query<TestEntity>($"SELECT * FROM TestEntity LIMIT {loops}").ToList();
            for (int i = 0; i < loops; i++)
            {
                if (i % 2 == 0 && existing.Count > 0)
                {
                    var u = existing[i % existing.Count];
                    TestEntityFactory.Mutate(u, "D_SX", writeRandom);
                    conn.Execute(SqliteWriteSql.SingleUpsertSql, u);
                }
                else
                    conn.Execute(SqliteWriteSql.SingleInsertSql, TestEntityFactory.New("D_SX_I", i));
            }
        }

        public override void testSingleGet(int loops)
        {
            using var conn = new SqliteConnection(sqlLiteDb);
            conn.Open();
            for (int i = 0; i < loops; i++)
            {
                int id = (i % loops) + 1;
                conn.QueryFirstOrDefault<TestEntity>("SELECT * FROM TestEntity WHERE Id=@Id", new { Id = id });
            }
        }
    }
}
