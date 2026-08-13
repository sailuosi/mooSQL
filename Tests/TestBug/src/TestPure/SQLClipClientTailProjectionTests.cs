using FluentAssertions;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.data;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// SQLClip 客户端尾投影 TDD（对标 Chloe MsSqlDemo.Method Select 段）。
    /// 见 doc/design/features/SQLClip-客户端尾投影.md §6.5
    /// </summary>
    public class SQLClipClientTailProjectionTests
    {
        private readonly DBInstance _db;
        private readonly char[] _space = { ' ' };

        public SQLClipClientTailProjectionTests()
        {
            _db = TestDatabaseHelper.CreateTestDBInstance();
            TestDatabaseHelper.EnsureTestUserSchema(_db);
            Seed();
        }

        private void Seed()
        {
            _db.ExeNonQuery(new SQLCmd("DELETE FROM test_users WHERE id IN (91001,91002,91003,91004)"));
            _db.ExeNonQuery(new SQLCmd(
                "INSERT INTO test_users (id, name, email, age, created_at, is_active) VALUES " +
                "(91001, ' Alice ', 'a@t.com', 20, '2020-01-01', 1)," +
                "(91002, 'bob', 'b@t.com', NULL, '2020-01-02', 1)," +
                "(91003, 'Sam', 's@t.com', 1, '2020-01-03', 1)," +
                "(91004, 'x', NULL, 5, '2020-01-04', 1)"));
        }

        private SQLClip Clip() => _db.useClip();

        private static void AssertNoSqlTailFunctions(string sql)
        {
            sql.Should().NotBeNullOrWhiteSpace();
            // 客户端尾投影不得把尾调用翻成 SQL 函数
            sql.Should().NotMatchRegex("(?i)\\bLEN\\s*\\(");
            sql.Should().NotMatchRegex("(?i)\\bLENGTH\\s*\\(");
            sql.Should().NotMatchRegex("(?i)\\bLOWER\\s*\\(");
            sql.Should().NotMatchRegex("(?i)\\bUPPER\\s*\\(");
            sql.Should().NotMatchRegex("(?i)\\bSUBSTRING\\s*\\(");
            sql.Should().NotMatchRegex("(?i)\\bSUBSTR\\s*\\(");
            sql.Should().NotMatchRegex("(?i)\\bDATEADD\\s*\\(");
            sql.Should().NotMatchRegex("(?i)\\bDATEDIFF\\s*\\(");
            sql.Should().NotMatchRegex("(?i)\\bTRIM\\s*\\(");
            sql.Should().NotMatchRegex("(?i)\\bLTRIM\\s*\\(");
            sql.Should().NotMatchRegex("(?i)\\bRTRIM\\s*\\(");
        }

        #region G4 纯列回归（实现前后均应绿）

        [Fact]
        public void G4_PureColumnAnonymous_ShouldWork_WithoutClientTailSlots()
        {
            var clip = Clip();
            clip.from<TestUser>(out var a);
            var q = clip
                .where(() => a.Id, 91001, ">=")
                .where(() => a.Id, 91003, "<=")
                .select(() => new { a.Id, a.Name, a.Age });

            var cmd = q.toSelect();
            cmd.sql.Should().Contain("SELECT");
            cmd.sql.Should().NotContain("__c");
            q.Context.ClientProjection.Should().BeNull("纯列不得进入尾投影管线");

            var list = q.queryList().ToList();
            list.Should().HaveCount(3);
            list.Select(x => x.Id).Should().BeEquivalentTo(new[] { 91001, 91002, 91003 });
        }

        #endregion

        #region G1 字符串尾方法

        [Fact]
        public void G1_StringTailMethods_ShouldMatchCsharp_AndSqlHasNoFunctions()
        {
            var clip = Clip();
            clip.from<TestUser>(out var a);
            var q = clip
                .where(() => a.Id, 91001)
                .select(() => new
                {
                    Id = a.Id,
                    String_Length = (int?)a.Name.Length,
                    Substring = a.Name.Substring(0),
                    Substring1 = a.Name.Substring(1),
                    Substring1_2 = a.Name.Substring(1, 2),
                    ToLower = a.Name.ToLower(),
                    ToUpper = a.Name.ToUpper(),
                    IsNullOrEmpty = string.IsNullOrEmpty(a.Name),
                    Contains = (bool?)a.Name.Contains("s"),
                    Trim = a.Name.Trim(),
                    TrimStart = a.Name.TrimStart(_space),
                    TrimEnd = a.Name.TrimEnd(_space),
                    StartsWith = (bool?)a.Name.StartsWith(" "),
                    EndsWith = (bool?)a.Name.EndsWith(" "),
                    Replace = a.Name.Replace("l", "L"),
                });

            var sql = q.toSelect().sql;
            AssertNoSqlTailFunctions(sql);
            // 列根去重：Id + Name
            Regex.Matches(sql, "(?i)\\bname\\b").Count.Should().BeGreaterThan(0);

            var row = q.queryList().Single();
            const string name = " Alice ";
            row.Id.Should().Be(91001);
            row.String_Length.Should().Be(name.Length);
            row.Substring.Should().Be(name.Substring(0));
            row.Substring1.Should().Be(name.Substring(1));
            row.Substring1_2.Should().Be(name.Substring(1, 2));
            row.ToLower.Should().Be(name.ToLower());
            row.ToUpper.Should().Be(name.ToUpper());
            row.IsNullOrEmpty.Should().Be(string.IsNullOrEmpty(name));
            row.Contains.Should().Be(name.Contains("s"));
            row.Trim.Should().Be(name.Trim());
            row.TrimStart.Should().Be(name.TrimStart(_space));
            row.TrimEnd.Should().Be(name.TrimEnd(_space));
            row.StartsWith.Should().Be(name.StartsWith(" "));
            row.EndsWith.Should().Be(name.EndsWith(" "));
            row.Replace.Should().Be(name.Replace("l", "L"));
        }

        [Fact]
        public void G1_SameColumnManyTails_ShouldSelectNameOnce()
        {
            var clip = Clip();
            clip.from<TestUser>(out var a);
            var sql = clip
                .where(() => a.Id, 91001)
                .select(() => new
                {
                    L = a.Name.Length,
                    U = a.Name.ToUpper(),
                    T = a.Name.Trim(),
                })
                .toSelect().sql;

            AssertNoSqlTailFunctions(sql);
            // 三投影共用 Name：SELECT 槽位只有 1 列（__c0 或 name 一次）
            var selectPart = Regex.Split(sql, "(?i)\\bFROM\\b")[0];
            var slotCount = Regex.Matches(selectPart, "(?i)__c\\d+").Count;
            var nameCount = Regex.Matches(selectPart, "(?i)\\bname\\b").Count;
            (slotCount == 1 || nameCount == 1).Should().BeTrue(
                $"expected single Name column in SELECT, sql={sql}");
        }

        #endregion

        #region G2 闭包常量 / Parse / DateTime

        [Fact]
        public void G2_ClientOnlyAndDateParse_ShouldMatchCsharp()
        {
            var startTime = new DateTime(2020, 1, 1, 12, 0, 0);
            var endTime = startTime.AddDays(1);
            var clip = Clip();
            clip.from<TestUser>(out var a);
            var q = clip
                .where(() => a.Id, 91001)
                .select(() => new
                {
                    Id = a.Id,
                    AddYears = startTime.AddYears(1),
                    AddDays = startTime.AddDays(1),
                    Int_Parse = int.Parse("1"),
                    Guid_Parse = Guid.Parse("D544BC4C-739E-4CD3-A3D3-7BF803FCE179"),
                    DateTime_Parse = DateTime.Parse("1992-1-16"),
                    DiffDays = (endTime - startTime).TotalDays,
                });

            var sql = q.toSelect().sql;
            AssertNoSqlTailFunctions(sql);

            var row = q.queryList().Single();
            row.Id.Should().Be(91001);
            row.AddYears.Should().Be(startTime.AddYears(1));
            row.AddDays.Should().Be(startTime.AddDays(1));
            row.Int_Parse.Should().Be(1);
            row.Guid_Parse.Should().Be(Guid.Parse("D544BC4C-739E-4CD3-A3D3-7BF803FCE179"));
            row.DateTime_Parse.Should().Be(DateTime.Parse("1992-1-16"));
            row.DiffDays.Should().Be(1);
        }

        #endregion

        #region G3 三元

        [Fact]
        public void G3_NullableTernary_ShouldMatchCsharp()
        {
            var clip = Clip();
            clip.from<TestUser>(out var a);
            var q = clip
                .where(() => a.Id, 91001, ">=")
                .where(() => a.Id, 91003, "<=")
                .select(() => new
                {
                    Id = a.Id,
                    B = a.Age == null ? false : a.Age > 1,
                });

            AssertNoSqlTailFunctions(q.toSelect().sql);

            var list = q.queryList().OrderBy(x => x.Id).ToList();
            list.Should().HaveCount(3);
            list[0].B.Should().Be(true);   // 20
            list[1].B.Should().Be(false);  // null
            list[2].B.Should().Be(false);  // 1
        }

        #endregion

        #region P1：可空传播 / 分页 / 缓存

        [Fact]
        public void P1_NullPropagateTail_ShouldReturnNull_WhenColumnNull()
        {
            var clip = Clip();
            clip.from<TestUser>(out var a);
            var row = clip
                .nullPropagateTail()
                .where(() => a.Id, 91004)
                .select(() => new
                {
                    Id = a.Id,
                    EmailLen = (int?)a.Email.Length,
                    EmailUpper = a.Email.ToUpper(),
                })
                .queryList()
                .Single();

            row.Id.Should().Be(91004);
            row.EmailLen.Should().BeNull();
            row.EmailUpper.Should().BeNull();
        }

        [Fact]
        public void P1_WithoutNullPropagate_NullColumnTail_ShouldThrow()
        {
            var clip = Clip();
            clip.from<TestUser>(out var a);
            var q = clip
                .where(() => a.Id, 91004)
                .select(() => new { Len = a.Email.Length });

            Action act = () => q.queryList().ToList();
            // Reader/表达式调用可能包装为 TargetInvocationException
            act.Should().Throw<Exception>()
                .Where(ex => ex is NullReferenceException
                             || ex.GetBaseException() is NullReferenceException);
        }

        [Fact]
        public void P1_QueryPage_WithTailProjection_ShouldPageAndTotal()
        {
            var clip = Clip();
            clip.from<TestUser>(out var a);
            var page = clip
                .where(() => a.Id, 91001, ">=")
                .where(() => a.Id, 91003, "<=")
                .select(() => new
                {
                    Id = a.Id,
                    Upper = a.Name.ToUpper(),
                })
                .setPage(2, 1)
                .queryPage();

            page.Total.Should().Be(3);
            page.PageSize.Should().Be(2);
            page.Items.Should().HaveCount(2);
            page.Items.Select(x => x.Id).Should().BeSubsetOf(new[] { 91001, 91002, 91003 });
            page.Items.First().Upper.Should().Be(page.Items.First().Upper.ToUpper());
        }

        [Fact]
        public void P1_ProjectorCache_SecondCall_ShouldMatchFirst()
        {
            List<object> RunOnce()
            {
                var clip = Clip();
                clip.from<TestUser>(out var a);
                return clip
                    .where(() => a.Id, 91001)
                    .select(() => new
                    {
                        Id = a.Id,
                        Lower = a.Name.ToLower(),
                        Len = a.Name.Length,
                    })
                    .queryList()
                    .Cast<object>()
                    .ToList();
            }

            var first = RunOnce();
            var second = RunOnce();
            first.Should().HaveCount(1);
            second.Should().HaveCount(1);
            // 匿名类型 ToString 含属性值，足以核对缓存复用后语义
            second[0].ToString().Should().Be(first[0].ToString());
        }

        #endregion

        #region 性能烟雾（非 BDN；基线见 baseline 文档）

        [Fact]
        public void PerfSmoke_PureAndTail_RecordsTiming()
        {
            for (int i = 0; i < 30; i++)
            {
                RunPureAnonymous();
                RunPureEntity();
                RunTailG1();
            }

            const int n = 300;
            var swAnon = Stopwatch.StartNew();
            for (int i = 0; i < n; i++)
                RunPureAnonymous();
            swAnon.Stop();

            var swEntity = Stopwatch.StartNew();
            for (int i = 0; i < n; i++)
                RunPureEntity();
            swEntity.Stop();

            var swTail = Stopwatch.StartNew();
            for (int i = 0; i < n; i++)
                RunTailG1();
            swTail.Stop();

            var meanAnonUs = swAnon.Elapsed.TotalMilliseconds * 1000.0 / n;
            var meanEntityUs = swEntity.Elapsed.TotalMilliseconds * 1000.0 / n;
            var meanTailUs = swTail.Elapsed.TotalMilliseconds * 1000.0 / n;
            var ratio = meanTailUs / Math.Max(meanAnonUs, 0.001);
            Console.WriteLine(
                $"[SQLClipClientTail baseline] TFM=net8.0 n={n} " +
                $"Clip.Anonymous.meanUs={meanAnonUs:F1} Clip.Result.meanUs={meanEntityUs:F1} " +
                $"Clip.TailG1.meanUs={meanTailUs:F1} Tail/Anon={ratio:F2}x");

            meanAnonUs.Should().BeLessThan(50_000);
            meanEntityUs.Should().BeLessThan(50_000);
            meanTailUs.Should().BeLessThan(50_000);
        }

        private void RunPureAnonymous()
        {
            var clip = Clip();
            clip.from<TestUser>(out var a);
            _ = clip
                .where(() => a.Id, 91001, ">=")
                .where(() => a.Id, 91003, "<=")
                .select(() => new { a.Id, a.Name, a.Age, a.Email })
                .queryList()
                .ToList();
        }

        private void RunPureEntity()
        {
            var clip = Clip();
            clip.from<TestUser>(out var a);
            _ = clip
                .where(() => a.Id, 91001, ">=")
                .where(() => a.Id, 91003, "<=")
                .select(a)
                .queryList()
                .ToList();
        }

        private void RunTailG1()
        {
            var clip = Clip();
            clip.from<TestUser>(out var a);
            _ = clip
                .where(() => a.Id, 91001)
                .select(() => new
                {
                    Id = a.Id,
                    Len = a.Name.Length,
                    Lower = a.Name.ToLower(),
                    Trim = a.Name.Trim(),
                })
                .queryList()
                .ToList();
        }

        #endregion
    }
}
