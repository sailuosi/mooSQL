using System;
using System.Linq;
using LiteOrm;
using LiteOrm.CodeGen;
using LiteOrm.Common;
using Microsoft.Data.Sqlite;

namespace dbTest.items
{
    /// <summary>
    /// LiteOrm（NuGet 8.1.13）适配器。使用 LiteOrmContext（非 DI）+ ObjectViewDAO/ObjectDAO。
    /// Condition/Join：QuerySource 链须以 Select 收尾（ToPreparedSql 根须为 SelectExpr），再 SqlGen.ToSql。
    /// </summary>
    public class LiteOrmTest : ITest
    {
        static readonly LiteOrmContext Context = new LiteOrmContext()
            .AddDataSource<SqliteConnection>("SQLite", sqlLiteDb, @default: true, syncTable: false);

        static readonly SessionManager Session = Context.CreateSession();

        static readonly SqlGen Sql = new SqlGen(typeof(LiteOrmTestEntity));

        static ObjectViewDAO<LiteOrmTestEntity> View()
            => new ObjectViewDAO<LiteOrmTestEntity>(Session);

        static ObjectDAO<LiteOrmTestEntity> Write()
            => new ObjectDAO<LiteOrmTestEntity>(Session);

        static string ToSql<T>(IQueryable<T> query)
            => Sql.ToSql(query.GetQueryExpr()).Sql;

        public override void testQueryResult()
        {
            var list = View().Search(q => q.Take(listTake)).ToList();
            _ = list;
        }

        public override void testQueryAnonymousResult()
        {
            // 匿名类型无法稳定 materialize，改用同形状命名 DTO（与 CoreOrm 口径一致）。
            var list = View().SearchAs(q => q.Take(listTake).Select(b => new CoreOrmAnonymousDto
            {
                Id = b.Id,
                F_Float = b.F_Float,
                F_Bool = b.F_Bool,
                F_DateTime = b.F_DateTime,
                F_Decimal = b.F_Decimal,
                F_Double = b.F_Double,
                F_Int64 = b.F_Int64
            })).ToList();
            _ = list;
        }

        public override string testQueryCondition()
        {
            var query = Expr.From<LiteOrmTestEntity>()
                .Where(b => b.F_String == "111" && b.F_Decimal > 0 && b.F_Bool == true && b.F_String.StartsWith("abc"))
                .Select(b => new
                {
                    b.F_Float,
                    b.F_Bool,
                    b.F_Double,
                    b.F_Byte,
                    b.F_String,
                    b.F_Decimal,
                    b.F_Int64
                });
            return ToSql(query);
        }

        public override string testQueryMethodCondition()
        {
            var query = Expr.From<LiteOrmTestEntity>()
                .Where(b => b.F_String.StartsWith("abc") && b.F_String.EndsWith("ddd") && b.F_String.Contains("333"))
                .Select(b => b);
            return ToSql(query);
        }

        public override void testQueryJoin()
        {
            // Take 须在 Select 之前，保证根为 SelectExpr（SectionExpr 不能单独 ToSql）
            var query = Expr.From<LiteOrmTestEntity>()
                .Join<LiteOrmTestEntityItem>((a, b) => a.Id == b.TestEntityId, TableJoinType.Inner)
                .Take(100)
                .Select(t => new { a4 = t.Item2.Name, t.Item1.Id });
            var sql = ToSql(query);
            _ = sql;
        }

        public override void testQueryLoop()
        {
            var view = View();
            for (var i = 0; i < 20; i++)
            {
                var id = i;
                var item = view.Search(b => b.Id == id).ToList();
                _ = item;
            }
        }

        public override void testInsert()
        {
            var write = Write();
            for (int i = 0; i < 30; i++)
            {
                write.Insert(new LiteOrmTestEntity
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
                });
            }
        }
    }
}
