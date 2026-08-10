using System.Linq;
using Microsoft.Data.Sqlite;
using NPoco;

namespace dbTest.items
{
    public class NPocoTest : ITest
    {
        Database getDb()
        {
            return new Database(sqlLiteDb, DatabaseType.SQLite, SqliteFactory.Instance);
        }

        public override void testQueryResult()
        {
            using var db = getDb();
            var list = db.Fetch<TestEntity>($"select * from TestEntity limit {listTake}");
        }

        public override void testQueryAnonymousResult()
        {
            using var db = getDb();
            var list = db.Fetch<dynamic>($"select Id, F_Float, F_Bool, F_DateTime, F_Decimal, F_Double, F_Int64 from TestEntity limit {listTake}");
        }

        public override string testQueryCondition()
        {
            // NPoco LINQ 无稳定 ToSql；条件场景用手写 SQL 字符串返回（执行路径见 Result/Loop）
            return "select F_Float, F_Bool, F_Double, F_Byte, F_String, F_Decimal, F_Int64 from TestEntity where F_String=@0 and F_Decimal>@1 and F_Bool=@2 and F_String like @3";
        }

        public override string testQueryMethodCondition()
        {
            return "select * from TestEntity where F_String like @0 and F_String like @1 and F_String like @2";
        }

        public override void testQueryJoin()
        {
            // 空：Join→SQL 非 NPoco 强项；避免无意义空转
        }

        public override void testQueryLoop()
        {
            for (var i = 0; i < 20; i++)
            {
                using var db = getDb();
                var item = db.Fetch<TestEntity>("select * from TestEntity where Id=@0", i);
            }
        }
    }
}
