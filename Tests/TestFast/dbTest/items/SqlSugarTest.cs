using Dm.util;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace dbTest.items
{
    public class SqlSugarTest : ITest
    {
        SqlSugarClient getDb()
        {
            return new SqlSugarClient(new ConnectionConfig()
            {
                ConnectionString = sqlLiteDb,
                DbType = DbType.Sqlite,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.SystemTable,
                AopEvents = new AopEvents
                {
                    //OnLogExecuting = (s, e) =>
                    //{
                    //    Console.WriteLine($"SqlSugar query {s}");
                    //}
                }
            });
        }
        public override void testQueryResult()
        {
            var db = getDb();
            var list = db.Queryable<TestEntity>().Take(listTake).ToList();
        }
        public override void testQueryAnonymousResult()
        {
            var db = getDb();
            var list = db.Queryable<TestEntity>().Take(listTake).Select(b => new
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
        public  void testQueryAnonymousResult2()
        {
            var db = getDb();
            var list = db.Queryable<TestEntity>().Take(listTake).Select(b =>b.Id).ToList();
        }
        public override string testQueryCondition()
        {
            var db = getDb();
            var filter = GetSelectFilter();
            var query = db.Queryable<TestEntity>().Where(filter);
            var sql = query.Select(b => new { b.F_Float, b.F_Bool, b.F_Double, b.F_Byte, b.F_String, b.F_Decimal, b.F_Int64 }).ToSqlString();
            return sql;
        }
        public override string testQueryMethodCondition()
        {
            var db = getDb();
            var filter = GetMethodFilter();
            var query = db.Queryable<TestEntity>().Where(filter);
            var sql = query.ToSqlString();
            return sql;
        }

        public override void testQueryJoin()
        {
            var db = getDb();
            var query = db.Queryable<TestEntity>().Take(100);
            var query2 = query.Select(b => new { a1 = b.Id, a2 = b.F_String });
            var query3 = query2.InnerJoin<TestEntityItem>((a, c) => a.a1 == c.TestEntityId);
            var query4 = query3.SelectMergeTable((a, c) => new { a3 = a.a1, a4 = c.Name })
                .InnerJoin<TestEntity>((d, e) => d.a3 == e.Id).Select((d, e) => new
                {
                    d.a4,
                    e.Id
                })
            ;
            var sql = query4.ToSqlString();
            //Console.WriteLine($"{GetType().Name}: {sql}");
        }
        public void testQueryJoin2()
        {
            var db = getDb();
            try
            {
                var query = db.Queryable<TestEntity>().Take(100);
                var query2 = query.Select(b => new { a1 = b.Id, a2 = b.F_String });
                var query3 = query2.InnerJoin<TestEntityItem>((a, c) => a.a1 == c.TestEntityId);
                //飘忽不定的select
                var query4 = query3.Select((a, c) => new { a3 = a.a1, a4 = c.Name })
                    .InnerJoin<TestEntity>((d, e) => d.a3 == e.Id).Select((d, e) => new
                    {
                        d.a4,
                        e.Id
                    })
                ;
                var sql = query4.ToSqlString();
                Console.WriteLine($"{GetType().Name}: {sql}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"{GetType().Name}: {e}");
            }
        }
        public override void testQueryLoop()
        {
            for (var i = 0; i < 20; i++)
            {
                var query = getDb().Queryable<TestEntity>();
                var item = query.Where(b => b.Id == i).ToList();
            }
        }
        public override void testInclude()
        {
            var query = getDb().Queryable<Blog>();
            query.Includes(b => b.BlogUser);
            query.Includes(b => b.Posts, b => b.Blog);
            var result2 = query.Select(b => new { url = b.Url, b.Id, post = b.Posts, user = b.BlogUser }).ToSql();
        }
        public override void testInsert()
        {
            for (int i = 0; i < 30; i++)
            {
                var db = getDb();
                //插入失败，SQLITE没处理自增主键
                db.Insertable(new TestEntity() { F_Bool = true, F_Byte = 1, F_DateTime = DateTime.Now, F_Decimal = 100.23M, F_Double = 23.22, F_Float = 1.22F, F_Int16 = 22, F_Int32 = 333, F_Int64 = 333, F_String = "string" + i }).ExecuteReturnIdentity();
            }
        }
        public void testJson()
        {
            var db = getDb();
            var item = db.Queryable<testJsonColumn>().Where(b => b.Id > 0).Take(3).First();
            Console.WriteLine($"EntityItem :{item.EntityItem}");
            var item2 = db.Queryable<testJsonColumn>().Where(b => b.Id > 0).Take(3).Select(b => new
            {
                EntityItem = b.EntityItem,
                Id = b.Id
            }).First();
            Console.WriteLine($"EntityItem :{item2.EntityItem}");
            var query = db.Queryable<testJsonColumn>().Where(b => b.Id > 0).Take(3).Select(b => new testJsonColumnDto
            {
                EntityItem = b.EntityItem
            });
            Console.WriteLine(query.ToSqlString());//查询出了id列
            var item3 = query.First();
            Console.WriteLine($"EntityItem :{item3.EntityItem}");//为空

        }
    }

}
