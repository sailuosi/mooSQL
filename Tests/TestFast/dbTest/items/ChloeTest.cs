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
        public override void testInsert()
        {
            for (int i = 0; i < 30; i++)
            {
                var db = getContext();
                db.Insert(new TestEntity() { F_Bool = true, F_Byte = 1, F_DateTime = DateTime.Now, F_Decimal = 100.23M, F_Double = 23.22, F_Float = 1.22F, F_Int16 = 22, F_Int32 = 333, F_Int64 = 333, F_String = "string" + i });
            }
        }
    }

}
