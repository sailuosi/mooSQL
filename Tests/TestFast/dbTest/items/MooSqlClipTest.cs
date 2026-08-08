using System.Linq;
using mooSQL.data;

namespace dbTest.items
{
    /// <summary>
    /// mooSQL SQLClip 基准适配器。
    /// </summary>
    public class MooSqlClipTest : ITest
    {
        public MooSqlClipTest()
        {
            MooSqlDb.EnsureInit();
        }

        public override void testQueryResult()
        {
            var clip = MooSqlDb.Db.useClip();
            clip.from<TestEntity>(out var e);
            // 触发 out 别名解析，并限制行数
            var list = clip
                .where(() => e.Id, 0, ">=")
                .select(e)
                .setPage(listTake, 1)
                .queryList()
                .ToList();
        }

        public override void testQueryAnonymousResult()
        {
            var clip = MooSqlDb.Db.useClip();
            clip.from<TestEntity>(out var e);
            var list = clip
                .select(() => new
                {
                    e.Id,
                    e.F_Float,
                    e.F_Bool,
                    e.F_DateTime,
                    e.F_Decimal,
                    e.F_Double,
                    e.F_Int64
                })
                .setPage(listTake, 1)
                .queryList()
                .ToList();
        }

        public override string testQueryCondition()
        {
            var clip = MooSqlDb.Db.useClip();
            clip.from<TestEntity>(out var e);
            var sql = clip
                .where(() => e.F_String, "111")
                .where(() => e.F_Decimal, 0m, ">")
                .where(() => e.F_Bool, true)
                .whereLikeLeft(() => e.F_String, "abc")
                .select(() => new
                {
                    e.F_Float,
                    e.F_Bool,
                    e.F_Double,
                    e.F_Byte,
                    e.F_String,
                    e.F_Decimal,
                    e.F_Int64
                })
                .toSelect()
                .sql;
            return sql;
        }

        public override string testQueryMethodCondition()
        {
            var clip = MooSqlDb.Db.useClip();
            clip.from<TestEntity>(out var e);
            var sql = clip
                .whereLikeLeft(() => e.F_String, "abc")
                .where(() => e.F_String, "%ddd", "LIKE")
                .whereLike(() => e.F_String, "333")
                .select(e)
                .toSelect()
                .sql;
            return sql;
        }

        public override void testQueryLoop()
        {
            for (var i = 0; i < 20; i++)
            {
                var clip = MooSqlDb.Db.useClip();
                clip.from<TestEntity>(out var e);
                var item = clip.where(() => e.Id, i).select(e).queryList().ToList();
            }
        }
    }
}
