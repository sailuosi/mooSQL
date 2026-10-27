using Chloe;
using Chloe.Infrastructure;
using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace dbTest.items
{
    public class ChloeTest : ITest
    {
        class sqlLiteDbConnectionFactory : IDbConnectionFactory
        {
            public System.Data.IDbConnection CreateConnection()
            {
                return new SqliteConnection(sqlLiteDb);
            }
        }
        IDbContext getContext()
        {
            return new Chloe.SQLite.SQLiteContext(new sqlLiteDbConnectionFactory());
        }
        public override void testQueryResult()
        {
            var query = getContext().Query<TestEntity>();
            var list = query.Take(listTake).ToList();
        }

        public override string testQueryCondition()
        {
            var filter = GetSelectFilter();
            var query = getContext().Query<TestEntity>();
            var sql = query.Where(filter).Select(b => new { b.F_Float, b.F_Bool, b.F_Double, b.F_Byte, b.F_String, b.F_Decimal, b.F_Int64 }).ToString();
            return sql;
        }
        public override string testQueryMethodCondition()
        {
            var filter = GetMethodFilter();
            var query = getContext().Query<TestEntity>();
            var sql = query.Where(filter).ToString();
            return sql;
        }

        public override void testQueryAnonymousResult()
        {
            var query = getContext().Query<TestEntity>();
            var list = query.Take(listTake).Select(b => new
            {
                b.Id,
                b.F_Float,
                b.F_Bool,
                b.F_DateTime,
                b.F_Decimal,
                b.F_Double,
                b.F_Int64
            }).ToList();
        }
        public override void testQueryJoin()
        {
            var query = getContext().Query<TestEntity>().Take(100);
            var join = query.Select(b => new { a1 = b.Id, a2 = b.F_String }).Join<TestEntityItem>(JoinType.InnerJoin, (a, b) => a.a1 == b.TestEntityId);
            var query3 = join.Select((a, b) => new { a3 = a.a1, a4 = b.Name })
                .Join<TestEntity>(JoinType.InnerJoin, (a, b) => a.a3 == b.Id).Select((a, b) => new
            {
                a.a4,
                b.Id
            });
            var sql = query3.ToString();
            //Console.WriteLine($"{GetType().Name}: {sql}");
        }

        public override void testQueryLoop()
        {
            for (var i = 0; i < 20; i++)
            {
                var item = getContext().Query<TestEntity>().Where(b => b.Id == i).ToList();
            }
        }
        public override void testInclude()
        {
            var query = getContext().Query<Blog>();
            query = query.Include(b => b.BlogUser);
            query = query.IncludeMany(b => b.Posts).ThenInclude(b => b.Blog);
            //var result = query.ToList();
            var result2 = query.Select(b => new { url = b.Url, b.Id, post = b.Posts, user = b.BlogUser }).ToString();//异常 NotSupportedException:“b.Posts”
        }
        public override void testBatchInsert(int count)
        {
            var db = getContext();
            db.InsertRange(TestEntityFactory.NewList("Chloe", count));
        }

        public override void testBatchUpdate(int count)
        {
            var db = getContext();
            var users = db.Query<TestEntity>().Take(count).ToList();
            foreach (var u in users)
            {
                TestEntityFactory.Mutate(u, "Chloe", writeRandom);
                db.Update(u);
            }
        }

        public override void testBatchUpsert(int count)
        {
            var db = getContext();
            var half = count / 2;
            var existing = db.Query<TestEntity>().Take(half).ToList();
            foreach (var u in existing)
            {
                TestEntityFactory.Mutate(u, "Ch_U", writeRandom);
                db.Update(u);
            }
            db.InsertRange(TestEntityFactory.NewList("Ch_I", count - half));
        }

        public override void testSingleInsert(int loops)
        {
            var db = getContext();
            for (int i = 0; i < loops; i++)
                db.Insert(TestEntityFactory.New("Ch_SI", i));
        }

        public override void testSingleUpdate(int loops)
        {
            var db = getContext();
            var users = db.Query<TestEntity>().Take(loops).ToList();
            if (users.Count == 0) return;
            for (int i = 0; i < loops; i++)
            {
                var u = users[i % users.Count];
                TestEntityFactory.Mutate(u, "Ch_SU", writeRandom);
                db.Update(u);
            }
        }

        public override void testSingleUpsert(int loops)
        {
            var db = getContext();
            var existing = db.Query<TestEntity>().Take(loops).ToList();
            for (int i = 0; i < loops; i++)
            {
                if (i % 2 == 0 && existing.Count > 0)
                {
                    var u = existing[i % existing.Count];
                    TestEntityFactory.Mutate(u, "Ch_SX", writeRandom);
                    db.Update(u);
                }
                else
                    db.Insert(TestEntityFactory.New("Ch_SX_I", i));
            }
        }

        public override void testSingleGet(int loops)
        {
            var db = getContext();
            for (int i = 0; i < loops; i++)
            {
                int id = (i % loops) + 1;
                _ = db.Query<TestEntity>().Where(x => x.Id == id).FirstOrDefault();
            }
        }
    }

}
