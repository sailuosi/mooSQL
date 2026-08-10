using System;
using System.Linq.Expressions;
using TORM;

namespace dbTest.items
{
    /// <summary>
    /// Core.ORM（产品名 TORM）适配器。查询 API 仅提供 Async（ToListAsync），基准内同步阻塞等待。
    /// 公开 API 未见多表 Join，testQueryJoin 保持空实现。
    /// 使用独立实体 <see cref="CoreOrmTestEntity"/>，避免 Orm 源码生成器污染共享 TestEntity。
    /// </summary>
    public class CoreOrmTest : ITest
    {
        OrmClient getDb()
        {
            return new OrmClient(new ConnectionConfig
            {
                DatabaseType = DatabaseType.Sqlite,
                ConnectionString = sqlLiteDb,
            }, null);
        }

        static Expression<Func<CoreOrmTestEntity, bool>> SelectFilter()
        {
            return b => b.F_String == "111" && b.F_Decimal > 0 && b.F_Bool == true && b.F_String.StartsWith("abc");
        }

        static Expression<Func<CoreOrmTestEntity, bool>> MethodFilter()
        {
            return b => b.F_String.StartsWith("abc") && b.F_String.EndsWith("ddd") && b.F_String.Contains("333");
        }

        public override void testQueryResult()
        {
            using var db = getDb();
            var list = db.Queryable<CoreOrmTestEntity>().Take(listTake).ToListAsync().GetAwaiter().GetResult();
        }

        public override void testQueryAnonymousResult()
        {
            using var db = getDb();
            // Core.ORM 无法 Activator.CreateInstance 匿名类型，改用同形状命名 DTO（列投影+映射口径仍可比）。
            var list = db.Queryable<CoreOrmTestEntity>().Take(listTake).Select(b => new CoreOrmAnonymousDto
            {
                Id = b.Id,
                F_Float = b.F_Float,
                F_Bool = b.F_Bool,
                F_DateTime = b.F_DateTime,
                F_Decimal = b.F_Decimal,
                F_Double = b.F_Double,
                F_Int64 = b.F_Int64
            }).ToListAsync().GetAwaiter().GetResult();
        }

        public override string testQueryCondition()
        {
            using var db = getDb();
            var (sql, _) = db.Queryable<CoreOrmTestEntity>().Where(SelectFilter())
                .Select(b => new { b.F_Float, b.F_Bool, b.F_Double, b.F_Byte, b.F_String, b.F_Decimal, b.F_Int64 })
                .ToSql();
            return sql;
        }

        public override string testQueryMethodCondition()
        {
            using var db = getDb();
            var (sql, _) = db.Queryable<CoreOrmTestEntity>().Where(MethodFilter()).ToSql();
            return sql;
        }

        public override void testQueryJoin()
        {
            // Core.ORM 公开 API 无多表 Join（无 InnerJoin / LeftJoin 等），空实现，BDN 成绩无业务意义。
        }

        public override void testQueryLoop()
        {
            for (var i = 0; i < 20; i++)
            {
                using var db = getDb();
                var item = db.Queryable<CoreOrmTestEntity>().Where(b => b.Id == i).ToListAsync().GetAwaiter().GetResult();
            }
        }

        public override void testInsert()
        {
            for (int i = 0; i < 30; i++)
            {
                using var db = getDb();
                db.Insertable(new CoreOrmTestEntity()
                {
                    F_Bool = true,
                    F_Byte = 1,
                    F_DateTime = DateTime.Now,
                    F_Decimal = 100.23M,
                    F_Double = 23.22,
                    F_Float = 1.22F,
                    F_Int16 = 22,
                    F_Int32 = 333,
                    F_Int64 = 333,
                    F_String = "string" + i
                }).ExecuteAsync().GetAwaiter().GetResult();
            }
        }
    }
}
