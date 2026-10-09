using FluentAssertions;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.data;
using System;
using TestMooSQL.src;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// 子查询含 top(n) 时的 SQL 生成验证。
    /// <para>
    /// 白盒要点：top(n) → skipTake(0,n)；HasSkipTakePaging() 对 skip=0 返回 false，
    /// 走 buildSelectNoPage，将 pageSize 写入 FragSQL.toped；子查询经 getBrotherBuilder().toSelect()
    /// 内联进 from/join/where/CTE/select 列，因此子查询内的 top 按当前方言独立生成。
    /// </para>
    /// </summary>
    public class SQLBuilderSubqueryTopTests
    {
        private static string Gen(DataBaseType dbType, Action<SQLBuilder> build)
        {
            using var kit = TestDatabaseHelper.CreateSQLBuilder(dbType);
            build(kit);
            return kit.toSelect().toRawSQL();
        }

        private static void BuildFromSubqueryWithTop(SQLBuilder kit)
        {
            kit.select("a.name")
                .from("a", t =>
                {
                    t.select("name")
                        .from("student")
                        .orderBy("id desc")
                        .top(1);
                });
        }

        private static void BuildJoinSubqueryWithTop(SQLBuilder kit)
        {
            kit.select("a.id")
                .from("tableA as a")
                .join("LEFT JOIN", "b on a.id=b.id", t =>
                {
                    t.select("id")
                        .from("student")
                        .orderBy("id desc")
                        .top(1);
                });
        }

        private static void BuildWhereInSubqueryWithTop(SQLBuilder kit)
        {
            kit.select("a.Name")
                .from("tableA as a")
                .whereIn("a.Name", t =>
                {
                    t.select("Name")
                        .from("student")
                        .orderBy("id desc")
                        .top(1);
                });
        }

        private static void BuildWhereExistSubqueryWithTop(SQLBuilder kit)
        {
            kit.select("a.id")
                .from("tableA as a")
                .whereExist(t =>
                {
                    t.select("1")
                        .from("student s")
                        .where("s.id=a.id")
                        .top(1);
                });
        }

        private static void BuildSelectColumnSubqueryWithTop(SQLBuilder kit)
        {
            kit.select("latest", t =>
                {
                    t.select("name")
                        .from("student")
                        .orderBy("id desc")
                        .top(1);
                })
                .from("dual_or_any");
        }

        private static void BuildCteSubqueryWithTop(SQLBuilder kit)
        {
            kit.withSelect("t1", t =>
                {
                    t.select("name")
                        .from("student")
                        .orderBy("id desc")
                        .top(1);
                })
                .select("*")
                .from("t1");
        }

        #region 维度1：方言差异 — from 子查询含 top

        [Fact]
        public void FromSubquery_Top_MSSQL_ShouldEmitTopInsideSubquery()
        {
            var sql = Gen(DataBaseType.MSSQL, BuildFromSubqueryWithTop);

            sql.Should().Contain("TOP 1");
            sql.Should().Contain("(SELECT");
            sql.Should().Contain(") as a");
            // 纯 top 不走 OFFSET/FETCH 分页分支
            sql.Should().NotContain("OFFSET");
            sql.Should().NotContain("FETCH");
            // TOP 出现在子查询内（SELECT 后、FROM student 前）
            var subStart = sql.IndexOf("(SELECT", StringComparison.OrdinalIgnoreCase);
            var subSql = sql.Substring(subStart);
            subSql.Should().MatchRegex(@"(?i)SELECT\s+TOP\s+1");
        }

        [Theory]
        [InlineData(DataBaseType.SQLite)]
        [InlineData(DataBaseType.MySQL)]
        [InlineData(DataBaseType.PostgreSQL)]
        public void FromSubquery_Top_LimitDialects_ShouldEmitLimitOffsetInsideSubquery(DataBaseType dbType)
        {
            var sql = Gen(dbType, BuildFromSubqueryWithTop);

            // top → skipTake(0,n) 使 skipNum=0，AppendLimitOffset 生成 LIMIT n OFFSET 0
            sql.Should().Contain("LIMIT 1 OFFSET 0");
            sql.Should().Contain("(SELECT");
            sql.Should().NotContain("TOP ");
            var subStart = sql.IndexOf("(SELECT", StringComparison.OrdinalIgnoreCase);
            sql.Substring(subStart).Should().Contain("LIMIT 1 OFFSET 0");
        }

        [Fact]
        public void FromSubquery_Top_Oracle_DefaultVersion_ShouldWrapRownum()
        {
            // versionNumber 默认 0 → Is12cOrHigher=false → ROWNUM 外包
            var sql = Gen(DataBaseType.Oracle, BuildFromSubqueryWithTop);

            sql.Should().Contain("ROWNUM <= 1");
            sql.Should().Contain("toptmp");
            sql.Should().NotContain("TOP ");
            sql.Should().NotContain("LIMIT ");
        }

        [Fact]
        public void FromSubquery_Top_Oracle12_ShouldEmitFetchFirst()
        {
            var db = DBTest.CreateDialectInstance(DataBaseType.Oracle);
            db.config.versionNumber = 12;
            db.config.version = "12.2";
            using var kit = TestDatabaseHelper.UseSQL(db);
            BuildFromSubqueryWithTop(kit);
            var sql = kit.toSelect().toRawSQL();

            sql.Should().Contain("FETCH FIRST 1 ROWS ONLY");
            sql.Should().NotContain("ROWNUM");
            sql.Should().NotContain("TOP ");
        }

        #endregion

        #region 维度1：子查询挂载位置

        [Fact]
        public void JoinSubquery_Top_MSSQL_ShouldKeepTopInJoinDerivedTable()
        {
            var sql = Gen(DataBaseType.MSSQL, BuildJoinSubqueryWithTop);

            sql.Should().Contain("LEFT JOIN");
            sql.Should().MatchRegex(@"(?i)LEFT JOIN\s+\(SELECT\s+TOP\s+1");
            sql.Should().Contain(") as b on a.id=b.id");
        }

        [Fact]
        public void JoinSubquery_Top_SQLite_ShouldKeepLimitInJoinDerivedTable()
        {
            var sql = Gen(DataBaseType.SQLite, BuildJoinSubqueryWithTop);

            sql.Should().Contain("LEFT JOIN");
            sql.Should().Contain("LIMIT 1 OFFSET 0");
            sql.Should().Contain(") as b on a.id=b.id");
        }

        [Fact]
        public void WhereInSubquery_Top_MSSQL_ShouldEmitTopInInClause()
        {
            var sql = Gen(DataBaseType.MSSQL, BuildWhereInSubqueryWithTop);

            // whereIn 子查询路径写入的 op 为 " in "（小写）
            sql.Should().MatchRegex(@"(?i)\bin\b\s+\(\s*SELECT\s+TOP\s+1");
        }

        [Fact]
        public void WhereInSubquery_Top_MySQL_ShouldEmitLimitInInClause()
        {
            var sql = Gen(DataBaseType.MySQL, BuildWhereInSubqueryWithTop);

            sql.Should().MatchRegex(@"(?i)\bin\b\s+\(");
            sql.Should().Contain("LIMIT 1 OFFSET 0");
            sql.Should().NotContain("TOP ");
        }

        [Fact]
        public void WhereExistSubquery_Top_MSSQL_ShouldEmitTopInExists()
        {
            var sql = Gen(DataBaseType.MSSQL, BuildWhereExistSubqueryWithTop);

            // whereExist 子查询路径写入的 op 为 " exists "（小写）
            sql.Should().MatchRegex(@"(?i)\bexists\b\s+\(\s*SELECT\s+TOP\s+1");
        }

        [Fact]
        public void WhereExistSubquery_Top_PostgreSQL_ShouldEmitLimitInExists()
        {
            var sql = Gen(DataBaseType.PostgreSQL, BuildWhereExistSubqueryWithTop);

            sql.Should().MatchRegex(@"(?i)\bexists\b");
            sql.Should().Contain("LIMIT 1 OFFSET 0");
        }

        [Fact]
        public void SelectColumnSubquery_Top_MSSQL_ShouldEmitTopInScalarSubquery()
        {
            var sql = Gen(DataBaseType.MSSQL, BuildSelectColumnSubqueryWithTop);

            sql.Should().MatchRegex(@"(?i)SELECT\s+\(SELECT\s+TOP\s+1");
            sql.Should().Contain(") as latest");
        }

        [Fact]
        public void SelectColumnSubquery_Top_SQLite_ShouldEmitLimitInScalarSubquery()
        {
            var sql = Gen(DataBaseType.SQLite, BuildSelectColumnSubqueryWithTop);

            sql.Should().Contain("LIMIT 1 OFFSET 0");
            sql.Should().Contain(") as latest");
            sql.Should().NotContain("TOP ");
        }

        [Fact]
        public void CteSubquery_Top_MSSQL_ShouldEmitTopInsideWithAs()
        {
            var sql = Gen(DataBaseType.MSSQL, BuildCteSubqueryWithTop);

            sql.Should().StartWith("WITH");
            sql.Should().MatchRegex(@"(?i)t1\s+AS\s+\(SELECT\s+TOP\s+1");
        }

        [Fact]
        public void CteSubquery_Top_SQLite_ShouldEmitLimitInsideWithAs()
        {
            var sql = Gen(DataBaseType.SQLite, BuildCteSubqueryWithTop);

            sql.Should().StartWith("WITH");
            sql.Should().Contain("LIMIT 1 OFFSET 0");
            sql.Should().Contain("t1 AS (");
        }

        #endregion

        #region 维度1：内外 top 互不干扰 + 分页路径区分

        [Fact]
        public void OuterAndInnerTop_MSSQL_ShouldEmitTopInBothLayers()
        {
            var sql = Gen(DataBaseType.MSSQL, kit =>
            {
                kit.select("a.name")
                    .from("a", t =>
                    {
                        t.select("name").from("student").top(3);
                    })
                    .top(1);
            });

            // 外层 TOP 1 + 子查询 TOP 3
            System.Text.RegularExpressions.Regex.Matches(sql, @"TOP\s+1", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                .Count.Should().Be(1);
            sql.Should().MatchRegex(@"(?i)TOP\s+3");
            sql.Should().MatchRegex(@"(?i)^SELECT\s+TOP\s+1");
        }

        [Fact]
        public void OuterAndInnerTop_SQLite_ShouldEmitLimitInBothLayers()
        {
            var sql = Gen(DataBaseType.SQLite, kit =>
            {
                kit.select("a.name")
                    .from("a", t =>
                    {
                        t.select("name").from("student").top(3);
                    })
                    .top(1);
            });

            System.Text.RegularExpressions.Regex.Matches(sql, @"LIMIT\s+3\s+OFFSET\s+0")
                .Count.Should().Be(1);
            System.Text.RegularExpressions.Regex.Matches(sql, @"LIMIT\s+1\s+OFFSET\s+0")
                .Count.Should().Be(1);
        }

        [Fact]
        public void TopOnly_MSSQL_ShouldNotUseOffsetFetch_EvenWhenVersionHigh()
        {
            // 白盒：HasSkipTakePaging 要求 skipNum>0 或 setPage，纯 top 走 TOP 而非 OFFSET/FETCH
            var db = DBTest.CreateDialectInstance(DataBaseType.MSSQL);
            db.config.versionNumber = 13;
            using var kit = TestDatabaseHelper.UseSQL(db);
            kit.select("id").from("users").top(5);
            var sql = kit.toSelect().toRawSQL();

            sql.Should().Contain("TOP 5");
            sql.Should().NotContain("OFFSET");
            sql.Should().NotContain("FETCH");
        }

        [Fact]
        public void SkipTake_MSSQL_HighVersion_ShouldUseOffsetFetch_NotTop()
        {
            var db = DBTest.CreateDialectInstance(DataBaseType.MSSQL);
            db.config.versionNumber = 13;
            using var kit = TestDatabaseHelper.UseSQL(db);
            kit.select("id").from("users").orderBy("id").skipTake(10, 5);
            var sql = kit.toSelect().toRawSQL();

            sql.Should().Contain("OFFSET 10 ROWS");
            sql.Should().Contain("FETCH NEXT 5 ROWS ONLY");
            sql.Should().NotContain("TOP ");
        }

        [Fact]
        public void SubqueryTop_ShouldPreserveOrderByInsideSubquery()
        {
            var sql = Gen(DataBaseType.MSSQL, BuildFromSubqueryWithTop);

            var sub = sql.Substring(sql.IndexOf("(SELECT", StringComparison.OrdinalIgnoreCase));
            sub.Should().Contain("ORDER BY id desc");
            // ORDER BY 应在子查询闭合前
            var close = sub.IndexOf(") as a", StringComparison.OrdinalIgnoreCase);
            close.Should().BeGreaterThan(0);
            sub.Substring(0, close).Should().Contain("ORDER BY");
        }

        #endregion

        #region 维度2：白盒 — HasSkipTakePaging / toped 赋值路径

        [Fact]
        public void WhiteBox_Top_SetsSkipTakeButHasSkipTakePagingIsFalse()
        {
            using var kit = TestDatabaseHelper.CreateSQLBuilder(DataBaseType.MSSQL);
            kit.select("id").from("users").top(7);
            kit.runBuild();

            kit.current.skipNum.Should().Be(0);
            kit.current.pageSize.Should().Be(7);
            kit.current.HasSkipTakePaging().Should().BeFalse(
                "skip=0 且未 setPage 时不应走 buildPagedSelect");
        }

        [Fact]
        public void WhiteBox_SkipTakePositiveSkip_HasSkipTakePagingIsTrue()
        {
            using var kit = TestDatabaseHelper.CreateSQLBuilder(DataBaseType.MSSQL);
            kit.select("id").from("users").skipTake(1, 7);
            kit.runBuild();

            kit.current.HasSkipTakePaging().Should().BeTrue();
        }

        [Fact]
        public void WhiteBox_BrotherBuilder_InheritsDialect_ForSubqueryTop()
        {
            using var kit = TestDatabaseHelper.CreateSQLBuilder(DataBaseType.MSSQL);
            string? innerSql = null;
            kit.select("*").from("outer_t", t =>
            {
                t.select("id").from("inner_t").top(2);
                // 子查询 toSelect 前可观察兄弟构造器方言与父相同
                t.Dialect.Should().BeSameAs(kit.Dialect);
                innerSql = t.toSelect().toRawSQL();
            });

            innerSql.Should().Contain("TOP 2");
            var outer = kit.toSelect().toRawSQL();
            outer.Should().Contain(innerSql!.Trim());
        }

        #endregion
    }
}
