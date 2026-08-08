using System.Linq;
using mooSQL.data;

namespace dbTest.items
{
    /// <summary>
    /// mooSQL SQLBuilder 基准适配器。
    /// </summary>
    public class MooSqlBuilderTest : ITest
    {
        public MooSqlBuilderTest()
        {
            MooSqlDb.EnsureInit();
        }

        public override void testQueryResult()
        {
            var list = MooSqlDb.Db.useSQL()
                .setTable("TestEntity")
                .top(listTake)
                .query<TestEntity>()
                .ToList();
        }

        public override void testQueryAnonymousResult()
        {
            var list = MooSqlDb.Db.useSQL()
                .setTable("TestEntity")
                .select("Id, F_Float, F_Bool, F_DateTime, F_Decimal, F_Double, F_Int64")
                .top(listTake)
                .query<TestEntity2>()
                .ToList();
        }

        public override string testQueryCondition()
        {
            var sql = MooSqlDb.Db.useSQL()
                .setTable("TestEntity")
                .select("F_Float, F_Bool, F_Double, F_Byte, F_String, F_Decimal, F_Int64")
                .where("F_String", "111")
                .where("F_Decimal", 0m, ">")
                .where("F_Bool", true)
                .whereLikeLeft("F_String", "abc")
                .toSelect()
                .sql;
            return sql;
        }

        public override string testQueryMethodCondition()
        {
            var sql = MooSqlDb.Db.useSQL()
                .setTable("TestEntity")
                .whereLikeLeft("F_String", "abc")
                .where("F_String", "%ddd", "LIKE")
                .whereLike("F_String", "333")
                .toSelect()
                .sql;
            return sql;
        }

        public override void testQueryLoop()
        {
            for (var i = 0; i < 20; i++)
            {
                var item = MooSqlDb.Db.useSQL()
                    .setTable("TestEntity")
                    .where("Id", i)
                    .query<TestEntity>()
                    .ToList();
            }
        }
    }
}
