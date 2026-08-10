using System.Linq;
using System.Linq.Expressions;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using Environment = NHibernate.Cfg.Environment;

namespace dbTest.items
{
    public class NHibernateTest : ITest
    {
        static readonly object Gate = new object();
        static ISessionFactory _factory;

        ISessionFactory getFactory()
        {
            if (_factory != null) return _factory;
            lock (Gate)
            {
                if (_factory != null) return _factory;
                var cfg = new Configuration();
                cfg.SetProperty(Environment.ConnectionDriver, typeof(SQLite20Driver).AssemblyQualifiedName);
                cfg.SetProperty(Environment.Dialect, typeof(SQLiteDialect).AssemblyQualifiedName);
                cfg.SetProperty(Environment.ConnectionProvider, typeof(NHibernate.Connection.DriverConnectionProvider).AssemblyQualifiedName);
                var cs = sqlLiteDb.TrimEnd(';');
                if (cs.IndexOf("Version=", System.StringComparison.OrdinalIgnoreCase) < 0)
                    cs += ";Version=3";
                cfg.SetProperty(Environment.ConnectionString, cs);
                cfg.SetProperty(Environment.ShowSql, "false");
                var mapper = new ModelMapper();
                mapper.AddMapping<NhTestEntityMap>();
                cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());
                _factory = cfg.BuildSessionFactory();
                return _factory;
            }
        }

        static Expression<System.Func<NhTestEntity, bool>> SelectFilter()
        {
            return b => b.F_String == "111" && b.F_Decimal > 0 && b.F_Bool == true && b.F_String.StartsWith("abc");
        }

        static Expression<System.Func<NhTestEntity, bool>> MethodFilter()
        {
            return b => b.F_String.StartsWith("abc") && b.F_String.EndsWith("ddd") && b.F_String.Contains("333");
        }

        public override void testQueryResult()
        {
            using var s = getFactory().OpenSession();
            var list = s.Query<NhTestEntity>().Take(listTake).ToList();
        }

        public override void testQueryAnonymousResult()
        {
            using var s = getFactory().OpenSession();
            var list = s.Query<NhTestEntity>().Take(listTake).Select(b => new
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

        public override string testQueryCondition()
        {
            using var s = getFactory().OpenSession();
            var q = s.Query<NhTestEntity>().Where(SelectFilter())
                .Select(b => new { b.F_Float, b.F_Bool, b.F_Double, b.F_Byte, b.F_String, b.F_Decimal, b.F_Int64 });
            return q.ToString() ?? "";
        }

        public override string testQueryMethodCondition()
        {
            using var s = getFactory().OpenSession();
            return s.Query<NhTestEntity>().Where(MethodFilter()).ToString() ?? "";
        }

        public override void testQueryJoin()
        {
            // Item 表无自然主键映射；Join 空实现。
        }

        public override void testQueryLoop()
        {
            for (var i = 0; i < 20; i++)
            {
                using var s = getFactory().OpenSession();
                var item = s.Query<NhTestEntity>().Where(b => b.Id == i).ToList();
            }
        }
    }

    class NhTestEntityMap : ClassMapping<NhTestEntity>
    {
        public NhTestEntityMap()
        {
            Table("TestEntity");
            Id(x => x.Id, m => m.Generator(Generators.Native));
            Property(x => x.F_Byte);
            Property(x => x.F_Int16);
            Property(x => x.F_Int32);
            Property(x => x.F_Int64);
            Property(x => x.F_Double);
            Property(x => x.F_Float);
            Property(x => x.F_Decimal);
            Property(x => x.F_Bool);
            Property(x => x.F_DateTime);
            Property(x => x.F_String);
        }
    }
}
