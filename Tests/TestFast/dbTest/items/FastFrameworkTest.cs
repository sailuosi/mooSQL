using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fast.Framework.Implements;
using Fast.Framework.Models;

namespace dbTest.items
{
    public class FastFrameworkTest: ITest
    {
        DbContext getDb()
        {
            return new DbContext(new List<DbOptions> { new DbOptions { ConnectionStrings=sqlLiteDb,
                DbType= Fast.Framework.Enum.DbType.SQLite,
                IsDefault=true,
                  DbId="1",
                ProviderName= "Microsoft.Data.Sqlite", FactoryName="Microsoft.Data.Sqlite.SqliteFactory,Microsoft.Data.Sqlite", } });
        }
        public override void testQueryResult()
        {
            var query = getDb().Query<TestEntity>();
            var list = query.Take(listTake).ToList();
        }

        public override string testQueryCondition()
        {
            var filter = GetSelectFilter();
            var query = getDb().Query<TestEntity>();
            var sql = query.Where(filter).Select(b => new { b.F_Float, b.F_Bool, b.F_Double, b.F_Byte, b.F_String, b.F_Decimal, b.F_Int64 }).ToSqlString();
            return sql;
        }
        public override string testQueryMethodCondition()
        {
            var filter = GetMethodFilter();
            var query = getDb().Query<TestEntity>();
            var sql = query.Where(filter).ToSqlString();
            return sql;
        }

        public override void testQueryAnonymousResult()
        {
            var query = getDb().Query<TestEntity>();
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
            var db = getDb();
            var query = db.Query<TestEntity>().Take(100);
            var subQuery = query.Select(b => new { a1 = b.Id, a2 = b.F_String });
            var subQuery2 = db.Query(subQuery).InnerJoin<TestEntityItem>((a, b) => a.a1 == b.TestEntityId).Select((a, b) => new { a3 = a.a1, a4 = b.Name });
            var query2 = db.Query(subQuery2)
                .InnerJoin<TestEntity>((a, b) => a.a3 == b.Id).Select((a, b) => new
                {
                    a.a4,
                    b.Id
                });
            var sql = query2.ToSqlString();
            //Console.WriteLine($"{GetType().Name}: {sql}");
        }
        public override void testQueryLoop()
        {
            for (var i = 0; i < 20; i++)
            {
                var query = getDb().Query<TestEntity>();
                var item = query.Where(b => b.Id == i).ToList();
            }
        }
        public override void testInclude()
        {
            var query = getDb().Query<Blog>();
            query.Include(b => b.BlogUser);
            query.Include(b => b.Posts);
            //var result = query.ToList();
            var result2 = query.Select(b => new { url = b.Url, b.Id, post = b.Posts, user = b.BlogUser }).ToSqlString();//异常System.NotSupportedException:“underlyingType暂不支持转换.”
        }
        public override void testInsert()
        {
            for (int i = 0; i < 30; i++)
            {
                var db = getDb();
                db.Insert(new TestEntity() { F_Bool = true, F_Byte = 1, F_DateTime = DateTime.Now, F_Decimal = 100.23M, F_Double = 23.22, F_Float = 1.22F, F_Int16 = 22, F_Int32 = 333, F_Int64 = 333, F_String = "string" + i }).ExceuteReturnIdentity();
            }
        }
    }
}
