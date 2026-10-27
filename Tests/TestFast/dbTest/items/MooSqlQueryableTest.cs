using System.Linq;
using mooSQL.data;
using mooSQL.linq;

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

        /// <summary>
        /// 对齐 Chloe：投影后两段关联（Ext 2-arg InnerJoin + SelectMany；当前翻译为 CROSS APPLY），只生成 SQL。
        /// </summary>
        public override void testQueryJoin()
        {
            var db = MooSqlDb.Db;
            var step1 =
                from a in db.useQueryable<TestEntity>().Take(listTake).Select(b => new { a1 = b.Id, a2 = b.F_String })
                from b in db.useQueryable<TestEntityItem>().InnerJoin(x => a.a1 == x.TestEntityId)
                select new { a3 = a.a1, a4 = b.Name };
            var step2 =
                from a in step1
                from e in db.useQueryable<TestEntity>().InnerJoin(x => a.a3 == x.Id)
                select new { a.a4, e.Id };
            _ = GetSqlText(step2);
        }

        public override void testBatchInsert(int count) => MooSqlWriteOps.BatchInsert(count, "MooQ");
        public override void testBatchUpdate(int count) => MooSqlWriteOps.BatchUpdate(count, "MooQ", writeRandom);
        public override void testBatchUpsert(int count) => MooSqlWriteOps.BatchUpsert(count, "MooQ_U", "MooQ_I", writeRandom);
        public override void testSingleInsert(int loops) => MooSqlWriteOps.SingleInsert(loops, "MooQ_SI");
        public override void testSingleUpdate(int loops) => MooSqlWriteOps.SingleUpdate(loops, "MooQ_SU", writeRandom);
        public override void testSingleUpsert(int loops) => MooSqlWriteOps.SingleUpsert(loops, "MooQ_SX", writeRandom);
        public override void testSingleGet(int loops) => MooSqlWriteOps.SingleGet(loops);

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
