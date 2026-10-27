using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dbTest.items
{
    public class efContext : Microsoft.EntityFrameworkCore.DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(ITest.sqlLiteDb);
                //.LogTo(Console.WriteLine, LogLevel.Information)
                //  .EnableSensitiveDataLogging();
            base.OnConfiguring(optionsBuilder);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var e = modelBuilder.Entity<TestEntity>();
            e.ToTable("TestEntity", "dbo");
            e.HasKey(b => b.Id);
            //e.ConfigEntityTypeBuilder();
            modelBuilder.Entity<Blog>()
                .HasOne(b => b.BlogUser)
                .WithMany()
                .HasForeignKey(b => b.UserId);

            modelBuilder.Entity<Blog>()
                .HasMany(b => b.Posts)
                .WithOne(p => p.Blog)
                .HasForeignKey(p => p.BlogId);

            modelBuilder.Entity<Post>()
                .HasOne(p => p.BlogUser)
                .WithMany()
                .HasForeignKey(p => p.UserId);
            base.OnModelCreating(modelBuilder);
        }
    }
    public class EfSqlliteTest : ITest
    {
        public override void testQueryResult()
        {
            using (var context = new efContext())
            {
                var query = context.Set<TestEntity>().AsQueryable();
                var list = query.Take(listTake).ToList();
            }
        }

        public override string testQueryCondition()
        {
            using (var context = new efContext())
            {
                var filter = GetSelectFilter();
                var query = context.Set<TestEntity>().AsQueryable();
                var sql = query.Where(filter).Select(b => new { b.F_Float, b.F_Bool, b.F_Double, b.F_Byte, b.F_String, b.F_Decimal, b.F_Int64 }).ToQueryString();
                return sql;
            }
        }
        public override string testQueryMethodCondition()
        {
            using (var context = new efContext())
            {
                var filter = GetMethodFilter();
                var query = context.Set<TestEntity>().AsQueryable();
                var sql = query.Where(filter).ToQueryString();
                return sql;
            }
        }
        public override void testQueryAnonymousResult()
        {
            using (var context = new efContext())
            {
                var query = context.Set<TestEntity>().AsQueryable();
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
        }
        public override void testQueryJoin()
        {

        }
        public override void testQueryLoop()
        {
            for (var i = 0; i < 20; i++)
            {
                using (var context = new efContext())
                {
                    var query = context.Set<TestEntity>().AsQueryable();
                    var list = query.Where(b => b.Id == i).ToList();
                }
            }
        }
        public override void testInclude()
        {
            using (var context = new efContext())
            {
                var query = context.Set<Blog>().AsQueryable().Include(b => b.BlogUser).Include(b => b.Posts).ThenInclude(b => b.Blog);
                //var result = query.ToList();
                var result2 = query.Select(b => new { url = b.Url, b.Id, post = b.Posts, user = b.BlogUser }).ToQueryString();
            }
        }
        public override void testBatchInsert(int count)
        {
            using var context = new efContext();
            context.ChangeTracker.AutoDetectChangesEnabled = false;
            context.AddRange(TestEntityFactory.NewList("EF", count));
            context.ChangeTracker.DetectChanges();
            context.SaveChanges();
        }

        public override void testBatchUpdate(int count)
        {
            using var context = new efContext();
            context.ChangeTracker.AutoDetectChangesEnabled = false;
            var users = context.Set<TestEntity>().Take(count).ToList();
            foreach (var u in users)
                TestEntityFactory.Mutate(u, "EF", writeRandom);
            context.ChangeTracker.DetectChanges();
            context.SaveChanges();
        }

        public override void testBatchUpsert(int count)
        {
            using var context = new efContext();
            context.ChangeTracker.AutoDetectChangesEnabled = false;
            var half = count / 2;
            var existing = context.Set<TestEntity>().Take(half).ToList();
            foreach (var u in existing)
                TestEntityFactory.Mutate(u, "EF_U", writeRandom);
            context.AddRange(TestEntityFactory.NewList("EF_I", count - half));
            context.ChangeTracker.DetectChanges();
            context.SaveChanges();
        }

        public override void testSingleInsert(int loops)
        {
            using var context = new efContext();
            context.ChangeTracker.AutoDetectChangesEnabled = false;
            for (int i = 0; i < loops; i++)
            {
                var e = TestEntityFactory.New("EF_SI", i);
                context.Add(e);
                context.ChangeTracker.DetectChanges();
                context.SaveChanges();
                context.Entry(e).State = EntityState.Detached;
            }
        }

        public override void testSingleUpdate(int loops)
        {
            using var context = new efContext();
            context.ChangeTracker.AutoDetectChangesEnabled = false;
            var users = context.Set<TestEntity>().AsNoTracking().Take(loops).ToList();
            if (users.Count == 0) return;
            for (int i = 0; i < loops; i++)
            {
                var u = users[i % users.Count];
                TestEntityFactory.Mutate(u, "EF_SU", writeRandom);
                context.Update(u);
                context.SaveChanges();
                context.Entry(u).State = EntityState.Detached;
            }
        }

        public override void testSingleUpsert(int loops)
        {
            using var context = new efContext();
            context.ChangeTracker.AutoDetectChangesEnabled = false;
            var existing = context.Set<TestEntity>().AsNoTracking().Take(loops).ToList();
            for (int i = 0; i < loops; i++)
            {
                if (i % 2 == 0 && existing.Count > 0)
                {
                    var u = existing[i % existing.Count];
                    TestEntityFactory.Mutate(u, "EF_SX", writeRandom);
                    context.Update(u);
                    context.SaveChanges();
                    context.Entry(u).State = EntityState.Detached;
                }
                else
                {
                    var e = TestEntityFactory.New("EF_SX_I", i);
                    context.Add(e);
                    context.ChangeTracker.DetectChanges();
                    context.SaveChanges();
                    context.Entry(e).State = EntityState.Detached;
                }
            }
        }

        public override void testSingleGet(int loops)
        {
            using var context = new efContext();
            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            for (int i = 0; i < loops; i++)
            {
                int id = (i % loops) + 1;
                _ = context.Set<TestEntity>().AsNoTracking().FirstOrDefault(x => x.Id == id);
            }
        }
    }
}
