using System.Data;
using System.Linq;
using FluentAssertions;
using mooSQL.data;
using mooSQL.data.model;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>SQLBuilder SELECT 结果缓存（setCache / 自动指纹 / useCachePrefix）。</summary>
    public class SQLBuilderResultCacheTests
    {
        [Fact]
        public void SqlCmd_GetCacheKey_StableForSameSqlAndParams()
        {
            var a = new SQLCmd("select 1 where id=@id");
            a.para.Add("@id", 1);
            a.type = QueryType.Select;

            var b = new SQLCmd("select 1 where id=@id");
            b.para.Add("@id", 1);
            b.type = QueryType.Select;

            a.GetCacheKey().Should().Be(b.GetCacheKey());
            a.GetCacheKey().Should().StartWith("RC:");
        }

        [Fact]
        public void SqlCmd_GetCacheKey_ChangesWhenParamChanges()
        {
            var a = new SQLCmd("select 1 where id=@id");
            a.para.Add("@id", 1);
            var b = new SQLCmd("select 1 where id=@id");
            b.para.Add("@id", 2);

            a.GetCacheKey().Should().NotBe(b.GetCacheKey());
        }

        [Fact]
        public void UseCachePrefix_ComposesRcNamespace()
        {
            StepBuilder.ComposeAutoCacheKeyPrefix("Shop").Should().Be("RC:Shop:");
            StepBuilder.ComposeAutoCacheKeyPrefix("RC:report").Should().Be("RC:report:");
            StepBuilder.ComposeAutoCacheKeyPrefix("").Should().Be("RC:");
        }

        [Fact]
        public void ResultCacheKey_ForUser_Normalizes()
        {
            ResultCacheKey.ForUser("0", "k1").Should().Be("RC:USER:0:k1");
            ResultCacheKey.ForUser("0", "RC:already").Should().Be("RC:already");
        }

        [Fact]
        public void SetCache_Int_EnablesResultCacheWithoutUserKey()
        {
            var db = TestDatabaseHelper.CreateTestDBInstance();
            using var kit = db.useSQL();
            kit.setCache(60);
            kit.Inner.resultCacheEnabled.Should().BeTrue();
            kit.Inner.HasUserResultCacheKey.Should().BeFalse();
            kit.Inner.IsResultCacheArmed.Should().BeTrue();
        }

        [Fact]
        public void SetCache_UserKey_RoundTrip_HitsCache()
        {
            using var fx = new SQLiteTestFixture();
            fx.CreateAllTables();
            fx.SeedStandardData();

            var shared = new HashCache();
            DataTable first;
            DataTable second;

            using (var kit = fx.Db.useSQL())
            {
                kit.setCacheHolder(shared)
                    .configClear(CleanWay.Never)
                    .setCache("users:all", 300)
                    .select("id, name")
                    .from(SQLiteTestFixture.UserTable);
                first = kit.query();
            }

            shared.GetKeys().Should().Contain(k => k != null && k.Contains("users:all"));

            using (var kit = fx.Db.useSQL())
            {
                kit.setCacheHolder(shared)
                    .configClear(CleanWay.Never)
                    .setCache("users:all", 300)
                    .select("id, name")
                    .from(SQLiteTestFixture.UserTable);
                second = kit.query();
            }

            second.Should().NotBeNull();
            first.Rows.Count.Should().Be(second.Rows.Count);
        }

        [Fact]
        public void SetCache_AutoFingerprint_SecondQueryHits()
        {
            using var fx = new SQLiteTestFixture();
            fx.CreateAllTables();
            fx.SeedStandardData();

            var shared = new HashCache();
            string keyAfterFirst = null;

            using (var kit = fx.Db.useSQL())
            {
                kit.setCacheHolder(shared)
                    .useCachePrefix("ut")
                    .configClear(CleanWay.Never)
                    .setCache(120)
                    .select("id, name")
                    .from(SQLiteTestFixture.UserTable)
                    .where("is_active", 1);
                var dt = kit.query();
                dt.Should().NotBeNull();
                keyAfterFirst = shared.GetKeys().FirstOrDefault(k => k != null && k.StartsWith("RC:ut:"));
            }

            keyAfterFirst.Should().NotBeNullOrEmpty();

            using (var kit = fx.Db.useSQL())
            {
                kit.setCacheHolder(shared)
                    .useCachePrefix("ut")
                    .configClear(CleanWay.Never)
                    .setCache(120)
                    .select("id, name")
                    .from(SQLiteTestFixture.UserTable)
                    .where("is_active", 1);
                var dt = kit.query();
                dt.Should().NotBeNull();
                shared.ContainsKey(keyAfterFirst).Should().BeTrue();
            }
        }

        [Fact]
        public void SetCache_AutoFingerprint_DifferentWhere_Misses()
        {
            using var fx = new SQLiteTestFixture();
            fx.CreateAllTables();
            fx.SeedStandardData();

            var shared = new HashCache();

            using (var kit = fx.Db.useSQL())
            {
                kit.setCacheHolder(shared).configClear(CleanWay.Never).setCache(60)
                    .select("id").from(SQLiteTestFixture.UserTable).where("is_active", 1);
                kit.query();
            }

            var keys1 = shared.GetKeys().Where(k => k != null && k.StartsWith("RC:")).ToList();
            keys1.Should().NotBeEmpty();

            using (var kit = fx.Db.useSQL())
            {
                kit.setCacheHolder(shared).configClear(CleanWay.Never).setCache(60)
                    .select("id").from(SQLiteTestFixture.UserTable).where("is_active", 0);
                kit.query();
            }

            shared.GetKeys().Count(k => k != null && k.StartsWith("RC:") && k.Contains(":dt"))
                .Should().BeGreaterThan(keys1.Count);
        }
    }
}
