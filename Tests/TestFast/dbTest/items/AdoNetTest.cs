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
    }
}
