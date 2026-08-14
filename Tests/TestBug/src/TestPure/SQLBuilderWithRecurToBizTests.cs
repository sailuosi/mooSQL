using System;
using FluentAssertions;
using mooSQL.data;
using mooSQL.Pure.Tests.SqlSnapshot;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// 固化业务侧 withRecurTo 用法。
    /// apply / whereRoot 走门面 SQLBuilder；CTE 经 withSelect 入队，产物以 SqlSnapshot 完整 SQL 相等为准。
    /// </summary>
    public class SQLBuilderWithRecurToBizTests : IDisposable
    {
        public const string CommFields =
            "OrgName,ClassCode,Varchar1,ORG_FLG,Varchar7,Varchar3,Int2,ACTIVE_FLAG,Boolean1,OrgNO";

        public const string SampleRootId = "00000000-0000-0000-0000-000000000001";
        public const int SampleDeep = 3;

        private readonly SQLBuilder _kit;

        public SQLBuilderWithRecurToBizTests()
        {
            _kit = TestDatabaseHelper.CreateSQLBuilder(DataBaseType.MSSQL);
            _kit.setSeed(SqlSnap.Seed);
        }

        public void Dispose()
        {
            _kit?.Dispose();
            SqlSnap.FlushIfDirty();
        }

        /// <summary>
        /// 用例1：根节点空时查父级（向上递归）+ 外层 ROW_NUMBER 去重。
        /// </summary>
        private static void BuildOrgParentRootQuery(SQLBuilder kit, string rootIdForWhere = SampleRootId)
        {
            kit.withRecurTo("O")
                .fromRoot("UCML_Organize")
                .joinOn("ParentOID", "UCML_OrganizeOID")
                .selectDeep("tDeepNum")
                .select("CAST( 'root' as varchar(50))", "CAST( 'parent' as varchar(50))", "lvType")
                .select(CommFields)
                .whereRoot((r, t) =>
                {
                    // 业务：RegxUntils.isGUID / r.useDuty(...)；此处只锁 withRecurTo 编排
                    r.where("src.UCML_OrganizeOID", rootIdForWhere);
                })
                .apply()
                .from("p", p =>
                {
                    p.select(
                            "* ,ROW_NUMBER()over (partition by UCML_OrganizeOID  order by Varchar1) n,(select COUNT(*) from UCML_Organize n where n.ParentOID=o.UCML_OrganizeOID) as childcc")
                        .from("o");
                })
                .where("p.n=1");
        }

        /// <summary>
        /// 用例2：按 rootID 向下递归子树 + whereNext 深度限制 + apply 后 select/from/where/query。
        /// </summary>
        private static void BuildOrgChildrenQuery(SQLBuilder kit, string rootId = SampleRootId, int deep = SampleDeep)
        {
            kit.withRecurTo("o")
                .select(CommFields)
                .selectDeep("tDeepNum")
                .fromRoot("UCML_Organize")
                .joinOn("UCML_OrganizeOID", "ParentOID")
                .whereRoot((r, t) =>
                {
                    r.where("tar.UCML_OrganizeOID", rootId);
                })
                .whereNext((n, t) =>
                {
                    n.where("np.tDeepNum<" + deep);
                })
                .apply()
                .select("*,(select COUNT(*) from UCML_Organize n where n.ParentOID=o.UCML_OrganizeOID) as childcc")
                .from("o")
                .where("o.UCML_OrganizeOI", rootId, "<>");
        }

        [Fact]
        public void Case1_OrgParentRoot_FullSql_ShouldMatchSnapshot()
        {
            SqlSnap.AssertSql(
                "cte_recur_to_org_parent_root",
                k => BuildOrgParentRootQuery(k),
                dbType: DataBaseType.MSSQL);
        }

        [Fact]
        public void Case2_OrgChildren_FullSql_ShouldMatchSnapshot()
        {
            SqlSnap.AssertSql(
                "cte_recur_to_org_children",
                k => BuildOrgChildrenQuery(k),
                dbType: DataBaseType.MSSQL);
        }

        /// <summary>
        /// 文档约定：apply 返回 SQLBuilder。当前返回 StepBuilder → 红灯，作为修复目标契约。
        /// </summary>
        [Fact]
        public void WithRecurTo_Apply_ShouldReturnFacadeSqlBuilder()
        {
            var afterApply = _kit.withRecurTo("O")
                .fromRoot("UCML_Organize")
                .joinOn("ParentOID", "UCML_OrganizeOID")
                .selectDeep("tDeepNum")
                .select("OrgName")
                .whereRoot((r, _) => r.where("src.UCML_OrganizeOID", "r1"))
                .apply();

            afterApply.Should().BeAssignableTo<SQLBuilder>(
                "apply() 应回到编排门面 SQLBuilder，而不是内核 StepBuilder；否则后续 from/where 无法入队，且与 kit.query 的 runBuild 脱节");
        }

        /// <summary>
        /// whereRoot 闭包应收到门面，以便业务扩展（useDuty 等）挂在 SQLBuilder 上。
        /// </summary>
        [Fact]
        public void WithRecurTo_WhereRoot_ShouldReceiveFacadeSqlBuilder()
        {
            object received = null;
            _kit.withRecurTo("O")
                .fromRoot("UCML_Organize")
                .joinOn("ParentOID", "UCML_OrganizeOID")
                .select("OrgName")
                .whereRoot((r, _) =>
                {
                    received = r;
                    r.where("src.UCML_OrganizeOID", "r1");
                })
                .apply();

            received.Should().BeAssignableTo<SQLBuilder>(
                "whereRoot 的 builder 参数应为 SQLBuilder，业务侧 r.useDuty(...) 才能编译/运行");
        }

        /// <summary>
        /// 若门面已有延迟步骤，eager withRecurTo 写入 _inner 会在 runBuild 时被 reset 冲掉。
        /// </summary>
        [Fact]
        public void WithRecurTo_AfterDeferredSelect_RunBuild_ShouldNotLoseCte()
        {
            _kit.select("keep"); // 入队 → dirty
            BuildOrgParentRootQuery(_kit);

            var sql = _kit.toSelect()?.sql ?? "";
            sql.Should().ContainEquivalentOf("with",
                "先 select 入队再 withRecurTo 时，物化不得冲掉递归 CTE（当前 eager 写内核 + reset 会丢）");
        }
    }
}
