using System.Linq;
using mooSQL.data;
using mooSQL.linq.Linq;

namespace dbTest.items
{
    /// <summary>
    /// mooSQL Ext Queryable（useQueryable）基准适配器。
    /// </summary>
    public class MooSqlQueryableTest : ITest
    {
        public MooSqlQueryableTest()
        {
            MooSqlDb.EnsureInit();
        }

        public override void testQueryResult()
        {
            var list = MooSqlDb.Db.useQueryable<TestEntity>()
                .Take(listTake)
                .ToList();
        }

        public override void testQueryAnonymousResult()
        {
            var list = MooSqlDb.Db.useQueryable<TestEntity>()
                .Take(listTake)
                .Select(b => new
                {
                    b.Id,
                    b.F_Float,
                    b.F_Bool,
                    b.F_DateTime,
                    b.F_Decimal,
                    b.F_Double,
                    b.F_Int64
                })
                .ToList();
        }

        public override string testQueryCondition()
        {
            var query = MooSqlDb.Db.useQueryable<TestEntity>()
                .Where(GetSelectFilter())
                .Select(b => new { b.F_Float, b.F_Bool, b.F_Double, b.F_Byte, b.F_String, b.F_Decimal, b.F_Int64 });
            return GetSqlText(query);
        }

        public override string testQueryMethodCondition()
        {
            var query = MooSqlDb.Db.useQueryable<TestEntity>().Where(GetMethodFilter());
            return GetSqlText(query);
        }

        public override void testQueryLoop()
        {
            for (var i = 0; i < 20; i++)
            {
                var id = i;
                var item = MooSqlDb.Db.useQueryable<TestEntity>()
                    .Where(b => b.Id == id)
                    .ToList();
            }
        }

        private static string GetSqlText<T>(IQueryable<T> query)
        {
            if (query is IExpressionQuery expr)
            {
                return expr.SqlText ?? string.Empty;
            }

            return query?.ToString() ?? string.Empty;
        }
    }
}
