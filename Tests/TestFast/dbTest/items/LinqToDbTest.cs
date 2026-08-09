using LinqToDB;
using System.Linq;

namespace dbTest.items
{
    public class AppDataConnection : DataContext
    {
        public ITable<TestEntity> Items => this.GetTable<TestEntity>();
        public AppDataConnection() : base(new DataOptions().UseConnectionString(ProviderName.SQLite, ITest.sqlLiteDb)) { }
    }
    public class LinqToDbTest : ITest
    {
        AppDataConnection getDb()
        {
            return new AppDataConnection();
        }
        public override void testQueryResult()
        {
            var query = getDb().Items;
            var list = query.Take(listTake).ToList();
        }

        public override string testQueryCondition()
        {
            var filter = GetSelectFilter();
            var query = getDb().Items;
            var sql = query.Where(filter).Select(b => new { b.F_Float, b.F_Bool, b.F_Double, b.F_Byte, b.F_String, b.F_Decimal, b.F_Int64 }).ToSqlQuery().Sql;
            return sql;
        }
        public override string testQueryMethodCondition()
        {
            var filter = GetMethodFilter();
            var query = getDb().Items;
            var sql = query.Where(filter).ToSqlQuery().Sql;
            return sql;
        }

        public override void testQueryAnonymousResult()
        {
            var query = getDb().Items;
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
        public override void testQueryLoop()
        {
            for (var i = 0; i < 20; i++)
            {
                var query = getDb().Items;
                var item = query.Where(b => b.Id == i).ToList();
            }
        }
    }
}
