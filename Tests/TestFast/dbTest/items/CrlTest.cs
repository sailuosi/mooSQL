using CRL.Data;
using CRL.Data.DBAccess;
using System;
using System.Collections.Generic;
using System.Linq;

namespace dbTest.items
{
    public class CrlTest : ITest
    {
        public static void Init()
        {
            var builder = DBConfigRegister.GetInstance();
            //builder.AutoRegisterDbProviderFactory();
            builder.RegisterDbProviderFactory(Microsoft.Data.Sqlite.SqliteFactory.Instance);//Microsoft驱动更稳定
            builder.RegisterDBAccessBuild(dbLocation =>
            {
                return new DBAccessBuild(DBType.SQLITE, ITest.sqlLiteDb);
            });
            //include
            builder.ConfigEntity<Blog>(p =>
            {
                p.Relation<Post>((a, b) => a.Id == b.BlogId);
                p.Relation<BlogUser>((a, b) => a.UserId == b.Id);
                p.Relation<BlogTag>((a, b) => a.Id == b.BlogId);
            });
            builder.ConfigEntity<Post>(p =>
            {
                p.Relation<BlogUser>((a, b) => a.UserId == b.Id);
            });
            SettingConfig.CheckModelTableMapping = false;
            SettingConfig.ConvertDbFieldValue = false;
            //InitData();
        }
        public static void InitData()
        {
            CrlTest.Init();
            var rep = RepositoryFactory.Get<TestEntity>();
            rep.CreateTable();
            rep.Delete(b => b.Id > 0);
            var c = rep.Count(b => b.Id > 0);
            if (c == 0)
            {
                // 1000 行：覆盖 BatchUpdate(1000) / SingleGet(200+) 写基准种子需求
                var list = new List<TestEntity>();
                for (int i = 0; i < 1000; i++)
                {
                    list.Add(new TestEntity() { F_Bool = true, F_Byte = 1, F_DateTime = DateTime.Now, F_Decimal = 100.23M, F_Double = 23.22, F_Float = 1.22F, F_Int16 = 22, F_Int32 = 333, F_Int64 = 333, F_String = "abcdefghijklmnopqrstuvwxyz" + i });
                }
                rep.BatchInsert(list);
                initRelation();
            }
            var rep4 = RepositoryFactory.Get<testJsonColumn>();
            rep4.CreateTable();
            rep4.Add(new testJsonColumn { EntityItem = new TestEntityItem { Name = DateTime.Now.ToString(), TestEntityId = DateTime.Now.Second } });
        }
        static void initRelation()
        {
            var rep = RepositoryFactory.Get<Blog>();
            rep.CreateTable();
            var c = rep.Count(b => !string.IsNullOrEmpty(b.Id));
            if (c > 0)
            {
                return;
            }
            var rep2 = RepositoryFactory.Get<Post>();
            var rep3 = RepositoryFactory.Get<BlogUser>();
            var rep4 = RepositoryFactory.Get<BlogTag>();
            rep2.CreateTable();
            rep3.CreateTable();
            rep4.CreateTable();

            var list1 = new List<Blog>();
            var list2 = new List<Post>();
            var list3 = new List<BlogUser>();
            var list4 = new List<BlogTag>();
            list1.Add(new Blog { Id = "b1", Url = "123", UserId = "u1" });
            list1.Add(new Blog { Id = "b2", Url = "123", UserId = "u2" });

            list2.Add(new Post { Id = "p1", BlogId = "b1", Title = "title", UserId = "u1" });
            list2.Add(new Post { Id = "p2", BlogId = "b2", Title = "title", UserId = "u1" });
            list2.Add(new Post { Id = "p3", BlogId = "b1", Title = "title", UserId = "u1" });
            list2.Add(new Post { Id = "p4", BlogId = "b2", Title = "title", UserId = "u1" });
            list2.Add(new Post { Id = "p5", BlogId = "b1", Title = "title", UserId = "u1" });
            list2.Add(new Post { Id = "p6", BlogId = "b2", Title = "title", UserId = "u1" });
            list2.Add(new Post { Id = "p7", BlogId = "b1", Title = "title", UserId = "u1" });
            list2.Add(new Post { Id = "p8", BlogId = "b2", Title = "title", UserId = "u1" });

            list3.Add(new BlogUser { Id = "u1", Name = "name" });
            list3.Add(new BlogUser { Id = "u2", Name = "name2" });

            list4.Add(new BlogTag { Id = "t1", BlogId = "b1", Tag = "t1" });
            list4.Add(new BlogTag { Id = "t2", BlogId = "b2", Tag = "t2" });
            rep.InsertOrUpdate(list1);
            rep2.InsertOrUpdate(list2);
            rep3.InsertOrUpdate(list3);
            rep4.InsertOrUpdate(list4);
        }
        public override void testQueryResult()
        {
            var rep = RepositoryFactory.Get<TestEntity>();
            var list = rep.GetLambdaQuery().Take(listTake).ToList();
        }
        public override void testQueryAnonymousResult()
        {
            var rep = RepositoryFactory.Get<TestEntity>();
            var list = rep.GetLambdaQuery().Take(listTake).Select(b => new
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
        public void testQueryAnonymousResult2()
        {
            var rep = RepositoryFactory.Get<TestEntity>();
            var list = rep.GetLambdaQuery().Take(listTake).Select(b =>b.Id).ToList();
        }
        public override string testQueryCondition()
        {
            var rep = RepositoryFactory.Get<TestEntity>();
            var filter = GetSelectFilter();
            var query = rep.GetLambdaQuery().Where(filter).Select(b => new { b.F_Float, b.F_Bool, b.F_Double, b.F_Byte, b.F_String, b.F_Decimal, b.F_Int64 });
            var sql = query.ToString();
            return sql;
        }
        public override string testQueryMethodCondition()
        {
            var rep = RepositoryFactory.Get<TestEntity>();
            var filter = GetMethodFilter();
            var query = rep.GetLambdaQuery().Where(filter);
            var sql = query.ToString();
            return sql;
        }
        public override void testQueryLoop()
        {
            for (var i = 0; i < 20; i++)
            {
                var rep = RepositoryFactory.Get<TestEntity>();
                var item = rep.GetLambdaQuery().Where(b => b.Id == i).ToList();
            }
        }
        public override void testQueryJoin()
        {
            var rep = RepositoryFactory.Get<TestEntity>();
            var query = rep.GetLambdaQuery().Take(100);
            var join = query.Select(b => new { a1 = b.Id, a2 = b.F_String }).Join<TestEntityItem>((a, b) => a.a1 == b.TestEntityId);
            var join2 = join.Select((a, b) => new { a3 = a.a1, a4 = b.Name })
                .Join<TestEntity>((a, b) => a.a3 == b.Id);
            join2.Select((a, b) => new
            {
                a.a4,
                b.Id
            });
            var sql = query.ToString();
            //Console.WriteLine($"{GetType().Name}: {sql}");
        }
        public void testQueryJoin2()
        {
            var rep = RepositoryFactory.Get<TestEntity>();
            var query = rep.GetLambdaQuery();
            var view = query.Take(10).Select(b => new { a1 = b.Id, a2 = b.F_String });
            var view2 = query.CreateQuery<TestEntityItem>().Select(b => new { b.TestEntityId, b.Name });
            var query2 = view.Join(view2, (a, b) => a.a1 == b.TestEntityId).Select((a, b) => new { a3 = a.a1, a4 = b.Name }).Join<TestEntity>((a, b) => a.a3 == b.Id).Select((a, b) => new
            {
                a.a4,
                b.Id
            });
            var sql = query.ToString();
            Console.WriteLine($"{GetType().Name}: {sql}");
        }
        public override void testInclude()
        {
            var rep = RepositoryFactory.Get<Blog>();
            var query = rep.GetLambdaQuery();
            query.Include(b => b.BlogUser);
            query.Include(b => b.Posts, q => q.Include(x => x.Blog));
            var result2 = query.Select(b => new { url = b.Url, b.Id, post = b.Posts, user = b.BlogUser });//.ToList();
        }
        public override void testBatchInsert(int count)
        {
            RepositoryFactory.Get<TestEntity>().BatchInsert(TestEntityFactory.NewList("Crl", count));
        }

        public override void testBatchUpdate(int count)
        {
            var rep = RepositoryFactory.Get<TestEntity>();
            var users = rep.GetLambdaQuery().Take(count).ToList();
            foreach (var u in users)
                TestEntityFactory.Mutate(u, "Crl", writeRandom);
            foreach (var u in users)
                rep.Update(u);
        }

        public override void testBatchUpsert(int count)
        {
            var rep = RepositoryFactory.Get<TestEntity>();
            var half = count / 2;
            var existing = rep.GetLambdaQuery().Take(half).ToList();
            foreach (var u in existing)
                TestEntityFactory.Mutate(u, "Crl_U", writeRandom);
            var neu = TestEntityFactory.NewList("Crl_I", count - half);
            rep.InsertOrUpdate(existing.Concat(neu).ToList());
        }

        public override void testSingleInsert(int loops)
        {
            var rep = RepositoryFactory.Get<TestEntity>();
            for (int i = 0; i < loops; i++)
                rep.Add(TestEntityFactory.New("Crl_SI", i));
        }

        public override void testSingleUpdate(int loops)
        {
            var rep = RepositoryFactory.Get<TestEntity>();
            var users = rep.GetLambdaQuery().Take(loops).ToList();
            if (users == null || users.Count == 0) return;
            for (int i = 0; i < loops; i++)
            {
                var u = users[i % users.Count];
                TestEntityFactory.Mutate(u, "Crl_SU", writeRandom);
                rep.Update(u);
            }
        }

        public override void testSingleUpsert(int loops)
        {
            var rep = RepositoryFactory.Get<TestEntity>();
            var existing = rep.GetLambdaQuery().Take(loops).ToList();
            for (int i = 0; i < loops; i++)
            {
                if (i % 2 == 0 && existing != null && existing.Count > 0)
                {
                    var u = existing[i % existing.Count];
                    TestEntityFactory.Mutate(u, "Crl_SX", writeRandom);
                    rep.InsertOrUpdate(u);
                }
                else
                    rep.InsertOrUpdate(TestEntityFactory.New("Crl_SX_I", i));
            }
        }

        public override void testSingleGet(int loops)
        {
            var rep = RepositoryFactory.Get<TestEntity>();
            for (int i = 0; i < loops; i++)
            {
                int id = (i % loops) + 1;
                _ = rep.QueryItem(id);
            }
        }
        public void testJson()
        {
            var rep = RepositoryFactory.Get<testJsonColumn>();
            var item = rep.GetLambdaQuery().Where(b => b.Id > 0).Take(3).ToSingle();
            Console.WriteLine($"EntityItem :{item.EntityItem}");
            var item2 = rep.GetLambdaQuery().Where(b => b.Id > 0).Take(3).Select(b => new
            {
                EntityItem = b.EntityItem,
                Id = b.Id
            }).ToSingle();
            Console.WriteLine($"EntityItem :{item2.EntityItem}");
            var query = rep.GetLambdaQuery().Where(b => b.Id > 0).Take(3).Select(b => new testJsonColumnDto
            {
                EntityItem = b.EntityItem
            });
            Console.WriteLine(query.ToString());
            var item3 = query.ToSingle();
            Console.WriteLine($"EntityItem :{item3.EntityItem}");
        }
    }
}
