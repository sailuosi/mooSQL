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

        /// <summary>
        /// Include 冒烟：Blog→Posts（includeHis 二次 IN）。依赖 CrlTest.InitData 建表灌数。
        /// </summary>
        public override void testInclude()
        {
            var kit = MooSqlDb.Db.useSQL();
            var blogs = kit.setTable("Blog").top(listTake).query<Blog>().ToList();
            if (blogs == null || blogs.Count == 0)
                return;
            foreach (var b in blogs)
                b.Posts = b.Posts ?? new System.Collections.Generic.List<Post>();

            kit.clear();
            kit.includeHis(
                blogs,
                b => b.Posts,
                b => b.Id,
                (Post p) => p.BlogId,
                "BlogId",
                null);
        }

        /// <summary>
        /// 对齐 Chloe：Take(100) 投影 → InnerJoin Item → 再投影 → InnerJoin Entity → 取 SQL。
        /// </summary>
        public override void testQueryJoin()
        {
            var sql = MooSqlDb.Db.useSQL()
                .select("v2.a4, e2.Id")
                .from("v2", v2 => v2
                    .select("v1.a1 as a3, item.Name as a4")
                    .from("v1", v1 => v1
                        .select("Id as a1, F_String as a2")
                        .from("TestEntity")
                        .top(listTake)
                    )
                    .innerJoin("TestEntityItem item on item.TestEntityId = v1.a1")
                )
                .innerJoin("TestEntity e2 on e2.Id = v2.a3")
                .toSelect()
                .sql;
            _ = sql;
        }
    }
}
