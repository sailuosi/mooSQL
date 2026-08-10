using System.Linq;
using SmartSql;
using SmartSql.DataSource;

namespace dbTest.items
{
    /// <summary>
    /// SmartSql（MyBatis 风格）。本基准用 RealSql 走执行/映射路径；无 SqlMap XML 时 Condition/Join 为空实现。
    /// </summary>
    public class SmartSqlTest : ITest
    {
        static readonly object Gate = new object();
        static ISqlMapper _mapper;

        ISqlMapper getMapper()
        {
            if (_mapper != null) return _mapper;
            lock (Gate)
            {
                if (_mapper != null) return _mapper;
                var builder = new SmartSqlBuilder()
                    .UseDataSource(DbProvider.SQLITE, sqlLiteDb)
                    .UseAlias("SmartSql")
                    .Build();
                _mapper = builder.SqlMapper;
                return _mapper;
            }
        }

        public override void testQueryResult()
        {
            var list = getMapper().Query<TestEntity>(new RequestContext
            {
                RealSql = $"select * from TestEntity limit {listTake}"
            }).ToList();
        }

        public override void testQueryAnonymousResult()
        {
            var list = getMapper().Query<dynamic>(new RequestContext
            {
                RealSql = $"select Id, F_Float, F_Bool, F_DateTime, F_Decimal, F_Double, F_Int64 from TestEntity limit {listTake}"
            }).ToList();
        }

        public override string testQueryCondition()
        {
            return "";
        }

        public override string testQueryMethodCondition()
        {
            return "";
        }

        public override void testQueryJoin()
        {
        }

        public override void testQueryLoop()
        {
            for (var i = 0; i < 20; i++)
            {
                var item = getMapper().Query<TestEntity>(new RequestContext
                {
                    RealSql = "select * from TestEntity where Id = @Id",
                    Request = new { Id = i }
                }).ToList();
            }
        }
    }
}
