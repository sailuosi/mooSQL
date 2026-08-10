using System.Linq;
using Microsoft.Data.Sqlite;
using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace dbTest.items
{
    public class SqlKataTest : ITest
    {
        static readonly SqliteCompiler Compiler = new SqliteCompiler();

        QueryFactory getFactory(SqliteConnection conn)
        {
            return new QueryFactory(conn, Compiler);
        }

        public override void testQueryResult()
        {
            using var conn = new SqliteConnection(sqlLiteDb);
            conn.Open();
            var list = getFactory(conn).Query("TestEntity").Limit(listTake).Get<TestEntity>().ToList();
        }

        public override void testQueryAnonymousResult()
        {
            using var conn = new SqliteConnection(sqlLiteDb);
            conn.Open();
            var list = getFactory(conn).Query("TestEntity")
                .Select("Id", "F_Float", "F_Bool", "F_DateTime", "F_Decimal", "F_Double", "F_Int64")
                .Limit(listTake)
                .Get()
                .ToList();
        }

        public override string testQueryCondition()
        {
            var q = new Query("TestEntity")
                .Where("F_String", "111")
                .Where("F_Decimal", ">", 0)
                .Where("F_Bool", true)
                .WhereLike("F_String", "abc%")
                .Select("F_Float", "F_Bool", "F_Double", "F_Byte", "F_String", "F_Decimal", "F_Int64");
            return Compiler.Compile(q).Sql;
        }

        public override string testQueryMethodCondition()
        {
            var q = new Query("TestEntity")
                .WhereLike("F_String", "abc%")
                .WhereLike("F_String", "%ddd")
                .WhereContains("F_String", "333");
            return Compiler.Compile(q).Sql;
        }

        public override void testQueryJoin()
        {
            var q = new Query("TestEntity as a")
                .Limit(100)
                .Join("TestEntityItem as b", j => j.On("a.Id", "b.TestEntityId"))
                .Select("b.Name as a4", "a.Id");
            var sql = Compiler.Compile(q).Sql;
            _ = sql;
        }

        public override void testQueryLoop()
        {
            for (var i = 0; i < 20; i++)
            {
                using var conn = new SqliteConnection(sqlLiteDb);
                conn.Open();
                var item = getFactory(conn).Query("TestEntity").Where("Id", i).Get<TestEntity>().ToList();
            }
        }
    }
}
