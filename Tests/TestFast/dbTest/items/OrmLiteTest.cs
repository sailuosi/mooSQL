using System.Linq;
using ServiceStack.OrmLite;

namespace dbTest.items
{
    public class OrmLiteTest : ITest
    {
        OrmLiteConnectionFactory getFactory()
        {
            return new OrmLiteConnectionFactory(sqlLiteDb, SqliteDialect.Provider);
        }

        public override void testQueryResult()
        {
            using var db = getFactory().Open();
            var list = db.Select(db.From<TestEntity>().Limit(listTake));
        }

        public override void testQueryAnonymousResult()
        {
            using var db = getFactory().Open();
            var list = db.Select(db.From<TestEntity>().Limit(listTake).Select(b => new
            {
                b.Id,
                b.F_Float,
                b.F_Bool,
                b.F_DateTime,
                b.F_Decimal,
                b.F_Double,
                b.F_Int64
            }));
        }

        public override string testQueryCondition()
        {
            using var db = getFactory().Open();
            var filter = GetSelectFilter();
            var expr = db.From<TestEntity>().Where(filter)
                .Select(b => new { b.F_Float, b.F_Bool, b.F_Double, b.F_Byte, b.F_String, b.F_Decimal, b.F_Int64 });
            return expr.ToSelectStatement();
        }

        public override string testQueryMethodCondition()
        {
            using var db = getFactory().Open();
            var filter = GetMethodFilter();
            return db.From<TestEntity>().Where(filter).ToSelectStatement();
        }

        public override void testQueryJoin()
        {
            using var db = getFactory().Open();
            var expr = db.From<TestEntity>().Limit(100)
                .Join<TestEntityItem>((a, b) => a.Id == b.TestEntityId)
                .Select<TestEntity, TestEntityItem>((a, b) => new { a4 = b.Name, a.Id });
            var sql = expr.ToSelectStatement();
            _ = sql;
        }

        public override void testQueryLoop()
        {
            for (var i = 0; i < 20; i++)
            {
                using var db = getFactory().Open();
                var item = db.Select(db.From<TestEntity>().Where(b => b.Id == i));
            }
        }
    }
}
