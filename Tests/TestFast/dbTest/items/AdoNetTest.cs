using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace dbTest.items
{
    /// <summary>
    /// 原生 ADO.NET（Microsoft.Data.Sqlite）基准适配器。
    /// 手写 SQL + DataReader 手工映射，作「无 ORM / 无微 ORM」执行+映射下限对照。
    /// Condition / MethodCondition / Join 无表达式→SQL，与 Dapper 一样空实现。
    /// </summary>
    public class AdoNetTest : ITest
    {
        public override void testQueryResult()
        {
            using var conn = new SqliteConnection(sqlLiteDb);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"select * from TestEntity limit {listTake}";
            using var reader = cmd.ExecuteReader();
            var list = new List<TestEntity>(listTake);
            if (!reader.Read())
            {
                return;
            }
            var ord = EntityOrdinals.Capture(reader);
            list.Add(MapEntity(reader, ord));
            while (reader.Read())
            {
                list.Add(MapEntity(reader, ord));
            }
        }

        public override void testQueryAnonymousResult()
        {
            using var conn = new SqliteConnection(sqlLiteDb);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                $"select Id, F_Float, F_Bool, F_DateTime, F_Decimal, F_Double, F_Int64 from TestEntity limit {listTake}";
            using var reader = cmd.ExecuteReader();
            var list = new List<CoreOrmAnonymousDto>(listTake);
            if (!reader.Read())
            {
                return;
            }
            var ord = AnonymousOrdinals.Capture(reader);
            list.Add(MapAnonymous(reader, ord));
            while (reader.Read())
            {
                list.Add(MapAnonymous(reader, ord));
            }
        }

        public override string testQueryCondition()
        {
            // 无表达式解析；空串避免伪 ToSql 进入 Condition 排行（同 Dapper）
            return "";
        }

        public override string testQueryMethodCondition()
        {
            return "";
        }

        public override void testQueryJoin()
        {
            // 空：Join→SQL 非本项能力
        }

        public override void testQueryLoop()
        {
            for (var i = 0; i < 20; i++)
            {
                using var conn = new SqliteConnection(sqlLiteDb);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "select * from TestEntity where Id=@id";
                cmd.Parameters.AddWithValue("@id", i);
                using var reader = cmd.ExecuteReader();
                var list = new List<TestEntity>(1);
                if (!reader.Read())
                {
                    continue;
                }
                var ord = EntityOrdinals.Capture(reader);
                list.Add(MapEntity(reader, ord));
                while (reader.Read())
                {
                    list.Add(MapEntity(reader, ord));
                }
            }
        }

        static TestEntity MapEntity(SqliteDataReader r, EntityOrdinals o)
        {
            return new TestEntity
            {
                Id = r.GetInt32(o.Id),
                F_Byte = r.IsDBNull(o.F_Byte) ? null : r.GetByte(o.F_Byte),
                F_Int16 = r.IsDBNull(o.F_Int16) ? null : r.GetInt16(o.F_Int16),
                F_Int32 = r.IsDBNull(o.F_Int32) ? null : r.GetInt32(o.F_Int32),
                F_Int64 = r.IsDBNull(o.F_Int64) ? null : r.GetInt64(o.F_Int64),
                F_Double = r.IsDBNull(o.F_Double) ? null : r.GetDouble(o.F_Double),
                F_Float = r.IsDBNull(o.F_Float) ? null : r.GetFloat(o.F_Float),
                F_Decimal = r.IsDBNull(o.F_Decimal) ? null : r.GetDecimal(o.F_Decimal),
                F_Bool = !r.IsDBNull(o.F_Bool) && r.GetBoolean(o.F_Bool),
                F_DateTime = r.IsDBNull(o.F_DateTime) ? null : r.GetDateTime(o.F_DateTime),
                F_String = r.IsDBNull(o.F_String) ? null : r.GetString(o.F_String),
            };
        }

        static CoreOrmAnonymousDto MapAnonymous(SqliteDataReader r, AnonymousOrdinals o)
        {
            return new CoreOrmAnonymousDto
            {
                Id = r.GetInt32(o.Id),
                F_Float = r.IsDBNull(o.F_Float) ? null : r.GetFloat(o.F_Float),
                F_Bool = !r.IsDBNull(o.F_Bool) && r.GetBoolean(o.F_Bool),
                F_DateTime = r.IsDBNull(o.F_DateTime) ? null : r.GetDateTime(o.F_DateTime),
                F_Decimal = r.IsDBNull(o.F_Decimal) ? null : r.GetDecimal(o.F_Decimal),
                F_Double = r.IsDBNull(o.F_Double) ? null : r.GetDouble(o.F_Double),
                F_Int64 = r.IsDBNull(o.F_Int64) ? null : r.GetInt64(o.F_Int64),
            };
        }

        readonly struct EntityOrdinals
        {
            public readonly int Id, F_Byte, F_Int16, F_Int32, F_Int64, F_Double, F_Float, F_Decimal, F_Bool, F_DateTime, F_String;

            EntityOrdinals(SqliteDataReader r)
            {
                Id = r.GetOrdinal("Id");
                F_Byte = r.GetOrdinal("F_Byte");
                F_Int16 = r.GetOrdinal("F_Int16");
                F_Int32 = r.GetOrdinal("F_Int32");
                F_Int64 = r.GetOrdinal("F_Int64");
                F_Double = r.GetOrdinal("F_Double");
                F_Float = r.GetOrdinal("F_Float");
                F_Decimal = r.GetOrdinal("F_Decimal");
                F_Bool = r.GetOrdinal("F_Bool");
                F_DateTime = r.GetOrdinal("F_DateTime");
                F_String = r.GetOrdinal("F_String");
            }

            public static EntityOrdinals Capture(SqliteDataReader r) => new EntityOrdinals(r);
        }

        readonly struct AnonymousOrdinals
        {
            public readonly int Id, F_Float, F_Bool, F_DateTime, F_Decimal, F_Double, F_Int64;

            AnonymousOrdinals(SqliteDataReader r)
            {
                Id = r.GetOrdinal("Id");
                F_Float = r.GetOrdinal("F_Float");
                F_Bool = r.GetOrdinal("F_Bool");
                F_DateTime = r.GetOrdinal("F_DateTime");
                F_Decimal = r.GetOrdinal("F_Decimal");
                F_Double = r.GetOrdinal("F_Double");
                F_Int64 = r.GetOrdinal("F_Int64");
            }

            public static AnonymousOrdinals Capture(SqliteDataReader r) => new AnonymousOrdinals(r);
        }

        public override void testBatchInsert(int count)
        {
            using var conn = new SqliteConnection(sqlLiteDb);
            conn.Open();
            using var tx = conn.BeginTransaction();
            SqliteWriteSql.BatchInsert(conn, TestEntityFactory.NewList("Ado", count), tx);
            tx.Commit();
        }

        public override void testBatchUpdate(int count)
        {
            using var conn = new SqliteConnection(sqlLiteDb);
            conn.Open();
            var users = LoadTake(conn, count);
            using var tx = conn.BeginTransaction();
            foreach (var u in users)
                TestEntityFactory.Mutate(u, "Ado", writeRandom);
            SqliteWriteSql.BatchUpsert(conn, users, tx);
            tx.Commit();
        }

        public override void testBatchUpsert(int count)
        {
            using var conn = new SqliteConnection(sqlLiteDb);
            conn.Open();
            var half = count / 2;
            var existing = LoadTake(conn, half);
            foreach (var u in existing)
                TestEntityFactory.Mutate(u, "A_Up", writeRandom);
            var neu = TestEntityFactory.NewList("A_In", count - half);
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
            {
                var e = TestEntityFactory.New("A_SI", i);
                using var cmd = conn.CreateCommand();
                cmd.CommandText = SqliteWriteSql.SingleInsertSql;
                BindEntity(cmd, e, withId: false);
                cmd.ExecuteNonQuery();
            }
        }

        public override void testSingleUpdate(int loops)
        {
            using var conn = new SqliteConnection(sqlLiteDb);
            conn.Open();
            var users = LoadTake(conn, loops);
            if (users.Count == 0) return;
            for (int i = 0; i < loops; i++)
            {
                var u = users[i % users.Count];
                TestEntityFactory.Mutate(u, "A_SU", writeRandom);
                using var cmd = conn.CreateCommand();
                cmd.CommandText = SqliteWriteSql.SingleUpdateSql;
                cmd.Parameters.AddWithValue("@F_String", (object)u.F_String ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@F_Int32", (object)u.F_Int32 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@F_Decimal", (object)u.F_Decimal ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Id", u.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public override void testSingleUpsert(int loops)
        {
            using var conn = new SqliteConnection(sqlLiteDb);
            conn.Open();
            var existing = LoadTake(conn, loops);
            for (int i = 0; i < loops; i++)
            {
                using var cmd = conn.CreateCommand();
                if (i % 2 == 0 && existing.Count > 0)
                {
                    var u = existing[i % existing.Count];
                    TestEntityFactory.Mutate(u, "A_SX", writeRandom);
                    cmd.CommandText = SqliteWriteSql.SingleUpsertSql;
                    BindEntity(cmd, u, withId: true);
                }
                else
                {
                    cmd.CommandText = SqliteWriteSql.SingleInsertSql;
                    BindEntity(cmd, TestEntityFactory.New("A_SX_I", i), withId: false);
                }
                cmd.ExecuteNonQuery();
            }
        }

        public override void testSingleGet(int loops)
        {
            using var conn = new SqliteConnection(sqlLiteDb);
            conn.Open();
            for (int i = 0; i < loops; i++)
            {
                int id = (i % loops) + 1;
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM TestEntity WHERE Id=@id";
                cmd.Parameters.AddWithValue("@id", id);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var ord = EntityOrdinals.Capture(reader);
                    _ = MapEntity(reader, ord);
                }
            }
        }

        static List<TestEntity> LoadTake(SqliteConnection conn, int count)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT * FROM TestEntity LIMIT {count}";
            using var reader = cmd.ExecuteReader();
            var list = new List<TestEntity>();
            if (!reader.Read()) return list;
            var ord = EntityOrdinals.Capture(reader);
            list.Add(MapEntity(reader, ord));
            while (reader.Read())
                list.Add(MapEntity(reader, ord));
            return list;
        }

        static void BindEntity(SqliteCommand cmd, TestEntity e, bool withId)
        {
            if (withId)
                cmd.Parameters.AddWithValue("@Id", e.Id);
            cmd.Parameters.AddWithValue("@F_Byte", (object)e.F_Byte ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@F_Int16", (object)e.F_Int16 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@F_Int32", (object)e.F_Int32 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@F_Int64", (object)e.F_Int64 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@F_Double", (object)e.F_Double ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@F_Float", (object)e.F_Float ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@F_Decimal", (object)e.F_Decimal ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@F_Bool", e.F_Bool);
            cmd.Parameters.AddWithValue("@F_DateTime", (object)e.F_DateTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@F_String", (object)e.F_String ?? DBNull.Value);
        }
    }
}
