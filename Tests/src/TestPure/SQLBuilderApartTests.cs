using FluentAssertions;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.data;
using System.Linq;
using Xunit;

namespace mooSQL.Pure.Tests
{
    public class SQLBuilderApartTests
    {
        private static void AssertSqlCmdEqual(SQLCmd expected, SQLCmd actual)
        {
            actual.sql.Should().Be(expected.sql);
            actual.para.Count.Should().Be(expected.para.Count);
            foreach (var kv in expected.para.value)
            {
                actual.para.value.Should().ContainKey(kv.Key);
                actual.para.GetParameter(kv.Key).val.Should().Be(kv.Value.val);
            }
        }

        [Fact]
        public void toApart_useApart_ShouldMatchManualBuild_BasicSelect()
        {
            var kit = TestDatabaseHelper.CreateSQLBuilder();
            const int tid = 42;

            var apart = kit.select("u.*")
                .from("users u")
                .where("u.tenant_id", tid)
                .toApart();

            var manual = kit.clear()
                .select("u.*")
                .from("users u")
                .where("u.tenant_id", tid)
                .where("u.status", 1)
                .toSelect();

            var replay = kit.clear()
                .useApart(apart)
                .where("u.status", 1)
                .toSelect();

            AssertSqlCmdEqual(manual, replay);
        }

        [Fact]
        public void toApart_useApart_ShouldMatchManualBuild_SinkOrWhere()
        {
            var kit = TestDatabaseHelper.CreateSQLBuilder();

            var apart = kit.select("id")
                .from("users")
                .sinkOR()
                .where("a", 1)
                .where("b", 2)
                .rise()
                .where("c", 3)
                .toApart();

            var manual = kit.clear()
                .select("id")
                .from("users")
                .sinkOR()
                .where("a", 1)
                .where("b", 2)
                .rise()
                .where("c", 3)
                .toSelect();

            var replay = kit.clear()
                .useApart(apart)
                .toSelect();

            AssertSqlCmdEqual(manual, replay);
        }

        [Fact]
        public void useApart_OnDifferentDbType_ShouldThrow()
        {
            var sqliteKit = TestDatabaseHelper.CreateSQLBuilder(DataBaseType.SQLite);
            var apart = sqliteKit.select("1").from("t").toApart();

            var mssqlDb = TestDatabaseHelper.CreateTestDBInstance(DataBaseType.MSSQL);
            var mssqlKit = mssqlDb.useSQL();

            var act = () => mssqlKit.useApart(apart);
            act.Should().Throw<ApartIncompatibleException>();
        }

        [Fact]
        public void apart_clear_ShouldRemoveSteps()
        {
            var kit = TestDatabaseHelper.CreateSQLBuilder();
            var apart = kit.select("id").from("users").where("x", 1).toApart();

            apart.clear();

            var cmd = kit.clear().useApart(apart).toSelect();
            cmd.sql.Should().NotContain("users");
        }

        [Fact]
        public void toApart_WithCte_ShouldReplay()
        {
            var kit = TestDatabaseHelper.CreateSQLBuilder();

            var apart = kit
                .withSelect("cte1", b => b.select("id").from("t"))
                .select("u.*")
                .from("users u")
                .where("u.id", 1)
                .toApart();

            var manual = kit.clear()
                .withSelect("cte1", b => b.select("id").from("t"))
                .select("u.*")
                .from("users u")
                .where("u.id", 1)
                .toSelect();

            var replay = kit.clear().useApart(apart).toSelect();

            AssertSqlCmdEqual(manual, replay);
        }

        [Fact]
        public void useApart_ParameterKeys_ShouldMatchManualChain()
        {
            var kit = TestDatabaseHelper.CreateSQLBuilder();
            var apart = kit.from("users").where("age", 18).toApart();

            kit.clear().useApart(apart).where("status", 1).toSelect();
            var replayKeys = kit.ps.value.Keys.OrderBy(k => k).ToList();

            kit.clear().from("users").where("age", 18).where("status", 1).toSelect();
            var manualKeys = kit.ps.value.Keys.OrderBy(k => k).ToList();

            replayKeys.Should().Equal(manualKeys);
        }
    }
}
