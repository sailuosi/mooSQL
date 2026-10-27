using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace dbTest.items
{
    public class FreeSqlTest : ITest
    {

        static IFreeSql db = new FreeSql.FreeSqlBuilder()
    .UseConnectionString(FreeSql.DataType.Sqlite, sqlLiteDb)
    .UseAutoSyncStructure(false) //自动同步实体结构到数据库
    .Build();
        public override void testQueryResult()
        {
            var query = db.Select<TestEntity>();
            var list = query.Take(listTake).ToList();
        }

        public override string testQueryCondition()
        {
            var filter = GetSelectFilter();
            var query = db.Select<TestEntity>();
            var sql = query.Where(filter).ToSql(b => new { b.F_Float, b.F_Bool, b.F_Double, b.F_Byte, b.F_String, b.F_Decimal, b.F_Int64 });
            //输出的拼接串，存在注入风险
            return sql;
        }
        public override string testQueryMethodCondition()
        {
            var filter = GetMethodFilter();
            var query = db.Select<TestEntity>();
            var sql = query.Where(filter).ToSql();
            //输出的拼接串，存在注入风险
            return sql;
        }

        public override void testQueryAnonymousResult()
        {
            var query = db.Select<TestEntity>();
            var list = query.Take(listTake).ToList(b => new
            {
                b.Id,
                b.F_Float,
                b.F_Bool,
                b.F_DateTime,
                b.F_Decimal,
                b.F_Double,
                b.F_Int64
            });
        }
        public override void testQueryJoin()
        {
            db.Aop.CurdAfter += (s, e) =>
            {
                //Console.WriteLine($"{GetType().Name}: {e.Sql}");
            };
            var query = db.Select<TestEntity>().Take(100);
            var query2 = query.WithTempQuery(b => new { a1 = b.Id, a2 = b.F_String });
            var query3 = query2.From<TestEntityItem>().InnerJoin((a, b) => a.a1 == b.TestEntityId);
            var query4 = query3.WithTempQuery((a, b) => new { a3 = a.a1, a4 = b.Name }).From<TestEntity>().InnerJoin((a, b) => a.a3 == b.Id);
            var sql = query4.WithTempQuery((a, b) => new
            {
                a.a4,
                b.Id
            }).ToSql();
            //var sql = query.ToString();
            //Console.WriteLine($"{GetType().Name}: {sql}");
        }
        public override void testQueryLoop()
        {
            for (var i = 0; i < 20; i++)
            {
                var query = db.Select<TestEntity>();
                var item = query.Where(b => b.Id == i).ToList();
            }
        }
        public override void testInclude()
        {
            var query = db.Queryable<Blog>();
            query.Include(b => b.BlogUser);
            query.IncludeMany(b => b.Posts, q => q.Include(x => x.Blog));
            var result2 = query.ToSql(b => new { url = b.Url, b.Id, post = b.Posts, user = b.BlogUser });
        }
        public override void testBatchInsert(int count)
        {
            db.Insert(TestEntityFactory.NewList("Free", count)).ExecuteAffrows();
        }

        public override void testBatchUpdate(int count)
        {
            var users = db.Select<TestEntity>().Take(count).ToList();
            foreach (var u in users)
                TestEntityFactory.Mutate(u, "Free", writeRandom);
            db.Update<TestEntity>().SetSource(users).ExecuteAffrows();
        }

        public override void testBatchUpsert(int count)
        {
            var half = count / 2;
            var existing = db.Select<TestEntity>().Take(half).ToList();
            foreach (var u in existing)
                TestEntityFactory.Mutate(u, "F_U", writeRandom);
            var all = existing.Concat(TestEntityFactory.NewList("F_I", count - half)).ToList();
            db.InsertOrUpdate<TestEntity>().SetSource(all).ExecuteAffrows();
        }

        public override void testSingleInsert(int loops)
        {
            for (int i = 0; i < loops; i++)
                db.Insert(TestEntityFactory.New("F_SI", i)).ExecuteAffrows();
        }

        public override void testSingleUpdate(int loops)
        {
            var users = db.Select<TestEntity>().Take(loops).ToList();
            if (users.Count == 0) return;
            for (int i = 0; i < loops; i++)
            {
                var u = users[i % users.Count];
                TestEntityFactory.Mutate(u, "F_SU", writeRandom);
                db.Update<TestEntity>().SetSource(u).ExecuteAffrows();
            }
        }

        public override void testSingleUpsert(int loops)
        {
            var existing = db.Select<TestEntity>().Take(loops).ToList();
            for (int i = 0; i < loops; i++)
            {
                if (i % 2 == 0 && existing.Count > 0)
                {
                    var u = existing[i % existing.Count];
                    TestEntityFactory.Mutate(u, "F_SX", writeRandom);
                    db.InsertOrUpdate<TestEntity>().SetSource(u).ExecuteAffrows();
                }
                else
                    db.InsertOrUpdate<TestEntity>().SetSource(TestEntityFactory.New("F_SX_I", i)).ExecuteAffrows();
            }
        }

        public override void testSingleGet(int loops)
        {
            for (int i = 0; i < loops; i++)
            {
                int id = (i % loops) + 1;
                _ = db.Select<TestEntity>().Where(x => x.Id == id).First();
            }
        }
        public void testJson()
        {
            db.UseJsonMap();
            var item = db.Queryable<testJsonColumn>().Where(b => b.Id > 0).Take(3).First();
            Console.WriteLine($"EntityItem :{item.EntityItem}");
            var item2 = db.Queryable<testJsonColumn>().Where(b => b.Id > 0).Take(3).ToList(b => new
            {
                EntityItem = b.EntityItem,
                Id = b.Id
            }).First();
            Console.WriteLine($"EntityItem :{item2.EntityItem}");
            var query = db.Queryable<testJsonColumn>().Where(b => b.Id > 0).Take(3);
            var sql = query.ToSql(b => new testJsonColumnDto
            {
                EntityItem = b.EntityItem
            });
            Console.WriteLine(sql);
            var item3 = query.ToList(b => new testJsonColumnDto
            {
                EntityItem = b.EntityItem
            }).First();
        }
    }

}
