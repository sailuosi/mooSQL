using System;
using System.Linq;
using mooSQL.data;

namespace dbTest.items
{
    /// <summary>
    /// mooSQL 三路径写基准共用：统一走 useRepo（与 LiteOrm EntityService 写栈对标）。
    /// </summary>
    internal static class MooSqlWriteOps
    {
        public static void BatchInsert(int count, string tag)
        {
            using var uow = MooSqlDb.Db.useWork();
            uow.InsertRange(TestEntityFactory.NewList(tag, count));
            uow.Commit();
        }

        public static void BatchUpdate(int count, string tag, Random random)
        {
            var users = MooSqlDb.Db.useRepo<TestEntity>().GetList(count);
            if (users == null || users.Count == 0) return;
            foreach (var u in users)
                TestEntityFactory.Mutate(u, tag, random);
            using var uow = MooSqlDb.Db.useWork();
            uow.UpdateRange(users);
            uow.Commit();
        }

        public static void BatchUpsert(int count, string tagUp, string tagIn, Random random)
        {
            var half = count / 2;
            var existing = MooSqlDb.Db.useRepo<TestEntity>().GetList(half)
                ?? new System.Collections.Generic.List<TestEntity>();
            foreach (var u in existing)
                TestEntityFactory.Mutate(u, tagUp, random);
            var neu = TestEntityFactory.NewList(tagIn, count - half);
            using var uow = MooSqlDb.Db.useWork();
            if (existing.Count > 0)
                uow.UpdateRange(existing);
            uow.InsertRange(neu);
            uow.Commit();
        }

        public static void SingleInsert(int loops, string tag)
        {
            var repo = MooSqlDb.Db.useRepo<TestEntity>();
            for (int i = 0; i < loops; i++)
                repo.Insert(TestEntityFactory.New(tag, i));
        }

        public static void SingleUpdate(int loops, string tag, Random random)
        {
            var repo = MooSqlDb.Db.useRepo<TestEntity>();
            var users = repo.GetList(loops);
            if (users == null || users.Count == 0) return;
            for (int i = 0; i < loops; i++)
            {
                var u = users[i % users.Count];
                TestEntityFactory.Mutate(u, tag, random);
                repo.Update(u);
            }
        }

        public static void SingleUpsert(int loops, string tag, Random random)
        {
            var repo = MooSqlDb.Db.useRepo<TestEntity>();
            var existing = repo.GetList(loops) ?? new System.Collections.Generic.List<TestEntity>();
            for (int i = 0; i < loops; i++)
            {
                if (i % 2 == 0 && existing.Count > 0)
                {
                    var u = existing[i % existing.Count];
                    TestEntityFactory.Mutate(u, tag + "_U", random);
                    repo.Save(u);
                }
                else
                    repo.Save(TestEntityFactory.New(tag + "_I", i));
            }
        }

        public static void SingleGet(int loops)
        {
            var repo = MooSqlDb.Db.useRepo<TestEntity>();
            for (int i = 0; i < loops; i++)
            {
                int id = (i % loops) + 1;
                _ = repo.GetById(id);
            }
        }
    }
}
