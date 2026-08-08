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
        public override void testInsert()
        {
            for (int i = 0; i < 30; i++)
            {
                using (var context = new efContext())
                {
                    context.Add(new TestEntity() { F_Bool = true, F_Byte = 1, F_DateTime = DateTime.Now, F_Decimal = 100.23M, F_Double = 23.22, F_Float = 1.22F, F_Int16 = 22, F_Int32 = 333, F_Int64 = 333, F_String = "string" + i });
                    context.SaveChanges();
                }
            }
        }
    }
}
