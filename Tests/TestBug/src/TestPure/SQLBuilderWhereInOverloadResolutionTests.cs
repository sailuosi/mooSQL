using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using FluentAssertions;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// whereIn / whereNotIn / whereNotInOrNull / whereOR 重载决议与值类型覆盖测试。
    /// </summary>
    public sealed class SQLBuilderWhereInOverloadResolutionTests
    {
        private static readonly Guid Guid1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid Guid2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly DateTime Dt1 = new DateTime(2024, 6, 1, 8, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Dt2 = new DateTime(2024, 6, 2, 8, 0, 0, DateTimeKind.Utc);

        public sealed record OverloadCase(
            string OverloadPath,
            Action<SQLBuilder> Build,
            WhereSqlExpectation Expectation);

        public sealed record WhereSqlExpectation(
            bool ExpectIn = false,
            bool ExpectNotIn = false,
            bool ExpectOr = false,
            bool ExpectIsNull = false,
            int MinListElements = 0,
            int MinOrConditions = 0,
            params string[] ContainsFragments);

        #region 重载决议

        public static IEnumerable<object[]> WhereInOverloadCases()
        {
            foreach (var c in OverloadResolutionCases())
            {
                if (c.Expectation.ExpectIn && !c.Expectation.ExpectNotIn && !c.Expectation.ExpectOr)
                    yield return new object[] { c };
            }
        }

        public static IEnumerable<object[]> WhereNotInOverloadCases()
        {
            foreach (var c in OverloadResolutionCases())
            {
                if (c.Expectation.ExpectNotIn && !c.Expectation.ExpectIsNull && !c.Expectation.ExpectOr)
                    yield return new object[] { c };
            }
        }

        public static IEnumerable<object[]> WhereNotInOrNullOverloadCases()
        {
            foreach (var c in OverloadResolutionCases())
            {
                if (c.Expectation.ExpectNotIn && c.Expectation.ExpectIsNull)
                    yield return new object[] { c };
            }
        }

        public static IEnumerable<object[]> WhereOROverloadCases()
        {
            foreach (var c in OverloadResolutionCases())
            {
                if (c.Expectation.ExpectOr)
                    yield return new object[] { c };
            }
        }

        private static IEnumerable<OverloadCase> OverloadResolutionCases()
        {
            yield return Case(
                "whereIn.params.string[]",
                b => b.select("*").from("t").whereIn("code", "a", "b"),
                In(2, "a", "b"));

            yield return Case(
                "whereIn.params.T[] where T:struct (int)",
                b => b.select("*").from("t").whereIn("id", 1, 2, 3),
                In(3, "1", "2", "3"));

            yield return Case(
                "whereIn.params.T[] where T:struct (Guid)",
                b => b.select("*").from("t").whereIn("oid", Guid1, Guid2),
                In(2, Guid1.ToString(), Guid2.ToString()));

            yield return Case(
                "whereIn.params.T[] where T:struct (int[] identity)",
                b => b.select("*").from("t").whereIn("id", new[] { 1, 2 }),
                In(2, "1", "2"));

            yield return Case(
                "whereIn.List<T>",
                b => b.select("*").from("t").whereIn("id", new List<int> { 1, 2 }),
                In(2, "1", "2"));

            yield return Case(
                "whereIn.IReadOnlyList<T> (variable)",
                b =>
                {
                    IReadOnlyList<string> ids = new List<string> { "x", "y" };
                    b.select("*").from("t").whereIn("code", ids);
                },
                In(2, "x", "y"));

            yield return Case(
                "whereIn.IReadOnlyList<T> (ReadOnlyCollection)",
                b => b.select("*").from("t").whereIn("id", new ReadOnlyCollection<int>(new[] { 1, 2 })),
                In(2, "1", "2"));

            yield return Case(
                "whereIn.IEnumerable<T> (HashSet)",
                b => b.select("*").from("t").whereIn("id", new HashSet<int> { 1, 2 }),
                In(2, "1", "2"));

            yield return Case(
                "whereIn.IEnumerable<T> (explicit cast)",
                b => b.select("*").from("t").whereIn("id", (IEnumerable<int>)new[] { 1, 2 }),
                In(2, "1", "2"));

            yield return Case(
                "whereNotIn.params.string[]",
                b => b.select("*").from("t").whereNotIn("code", "a", "b"),
                NotIn(2, "a", "b"));

            yield return Case(
                "whereNotIn.params.T[] where T:struct",
                b => b.select("*").from("t").whereNotIn("id", 4, 5),
                NotIn(2, "4", "5"));

            yield return Case(
                "whereNotIn.List<T>",
                b => b.select("*").from("t").whereNotIn("id", new List<int> { 4, 5 }),
                NotIn(2, "4", "5"));

            yield return Case(
                "whereNotIn.IReadOnlyList<T>",
                b =>
                {
                    IReadOnlyList<int> ids = new List<int> { 4, 5 };
                    b.select("*").from("t").whereNotIn("id", ids);
                },
                NotIn(2, "4", "5"));

            yield return Case(
                "whereNotIn.IEnumerable<T> (HashSet)",
                b => b.select("*").from("t").whereNotIn("id", new HashSet<int> { 4, 5 }),
                NotIn(2, "4", "5"));

            yield return Case(
                "whereNotInOrNull.IEnumerable<T> (array)",
                b => b.select("*").from("t").whereNotInOrNull("id", new[] { 1, 2 }),
                NotInOrNull(2, "1", "2"));

            yield return Case(
                "whereNotInOrNull.List<T>",
                b => b.select("*").from("t").whereNotInOrNull("id", new List<int> { 1, 2 }),
                NotInOrNull(2, "1", "2"));

            yield return Case(
                "whereNotInOrNull.IReadOnlyList<T>",
                b =>
                {
                    IReadOnlyList<int> ids = new List<int> { 1, 2 };
                    b.select("*").from("t").whereNotInOrNull("id", ids);
                },
                NotInOrNull(2, "1", "2"));

            yield return Case(
                "whereOR.params.string[]",
                b => b.select("*").from("t").whereOR("code", "a", "b"),
                Or(2));

            yield return Case(
                "whereOR.params.T[] where T:struct",
                b => b.select("*").from("t").whereOR("id", 1, 2, 3),
                Or(3));
        }

        [Theory]
        [MemberData(nameof(WhereInOverloadCases))]
        public void WhereIn_OverloadResolution(OverloadCase overloadCase)
            => AssertExpectation(overloadCase);

        [Theory]
        [MemberData(nameof(WhereNotInOverloadCases))]
        public void WhereNotIn_OverloadResolution(OverloadCase overloadCase)
            => AssertExpectation(overloadCase);

        [Theory]
        [MemberData(nameof(WhereNotInOrNullOverloadCases))]
        public void WhereNotInOrNull_OverloadResolution(OverloadCase overloadCase)
            => AssertExpectation(overloadCase);

        [Theory]
        [MemberData(nameof(WhereOROverloadCases))]
        public void WhereOR_OverloadResolution(OverloadCase overloadCase)
            => AssertExpectation(overloadCase);

        #endregion

        #region 值类型覆盖（字符串 / 数值 / 时间 / Guid / bool / 可空值类型）

        public static IEnumerable<object[]> WhereInValueTypeCases()
        {
            foreach (var c in ValueTypeCases())
                yield return new object[] { c };
        }

        public static IEnumerable<object[]> WhereNotInValueTypeCases()
        {
            foreach (var c in ValueTypeNotInCases())
                yield return new object[] { c };
        }

        private static IEnumerable<OverloadCase> ValueTypeNotInCases()
        {
            yield return Case(
                "whereNotIn.value.string (params)",
                b => b.select("*").from("t").whereNotIn("code", "alpha", "beta"),
                NotIn(2, "alpha", "beta"));

            yield return Case(
                "whereNotIn.value.numeric.int (params)",
                b => b.select("*").from("t").whereNotIn("id", 10, 20),
                NotIn(2, "10", "20"));

            yield return Case(
                "whereNotIn.value.numeric.long (params)",
                b => b.select("*").from("t").whereNotIn("n", 100L, 200L),
                NotIn(2, "100", "200"));

            yield return Case(
                "whereNotIn.value.numeric.decimal (IEnumerable)",
                b => b.select("*").from("t").whereNotIn("amount", new List<decimal> { 1.5m, 2.5m }),
                NotIn(2, "1.5", "2.5"));

            yield return Case(
                "whereNotIn.value.DateTime (params)",
                b => b.select("*").from("t").whereNotIn("dt", Dt1, Dt2),
                NotIn(2));

            yield return Case(
                "whereNotIn.value.Guid (params)",
                b => b.select("*").from("t").whereNotIn("oid", Guid1, Guid2),
                NotIn(2, Guid1.ToString(), Guid2.ToString()));

            yield return Case(
                "whereNotIn.value.bool (params)",
                b => b.select("*").from("t").whereNotIn("flag", true, false),
                NotIn(2, "True", "False"));

            yield return Case(
                "whereNotIn.value.int? (params,含 null)",
                b => b.select("*").from("t").whereNotIn("id", (int?)1, null, (int?)2),
                NotIn(2, "1", "2"));

            yield return Case(
                "whereNotIn.value.long? (params,含 null)",
                b => b.select("*").from("t").whereNotIn("n", (long?)100L, null, (long?)200L),
                NotIn(2, "100", "200"));

            yield return Case(
                "whereNotIn.value.DateTime? (params,含 null)",
                b => b.select("*").from("t").whereNotIn("dt", (DateTime?)Dt1, null, (DateTime?)Dt2),
                NotIn(2));

            yield return Case(
                "whereNotIn.value.Guid? (params,含 null)",
                b => b.select("*").from("t").whereNotIn("oid", (Guid?)Guid1, null, (Guid?)Guid2),
                NotIn(2, Guid1.ToString(), Guid2.ToString()));

            yield return Case(
                "whereNotIn.value.bool? (params,含 null)",
                b => b.select("*").from("t").whereNotIn("flag", (bool?)true, null, (bool?)false),
                NotIn(2, "True", "False"));
        }

        private static IEnumerable<OverloadCase> ValueTypeCases()
        {
            yield return Case(
                "whereIn.value.string (params)",
                b => b.select("*").from("t").whereIn("code", "alpha", "beta"),
                In(2, "alpha", "beta"));

            yield return Case(
                "whereIn.value.numeric.int (params)",
                b => b.select("*").from("t").whereIn("id", 10, 20),
                In(2, "10", "20"));

            yield return Case(
                "whereIn.value.numeric.long (params)",
                b => b.select("*").from("t").whereIn("n", 100L, 200L),
                In(2, "100", "200"));

            yield return Case(
                "whereIn.value.numeric.decimal (IEnumerable)",
                b => b.select("*").from("t").whereIn("amount", new List<decimal> { 1.5m, 2.5m }),
                In(2, "1.5", "2.5"));

            yield return Case(
                "whereIn.value.DateTime (params)",
                b => b.select("*").from("t").whereIn("dt", Dt1, Dt2),
                In(2));

            yield return Case(
                "whereIn.value.Guid (params)",
                b => b.select("*").from("t").whereIn("oid", Guid1, Guid2),
                In(2, Guid1.ToString(), Guid2.ToString()));

            yield return Case(
                "whereIn.value.bool (params)",
                b => b.select("*").from("t").whereIn("flag", true, false),
                In(2, "True", "False"));

            yield return Case(
                "whereIn.value.int? (params,含 null)",
                b => b.select("*").from("t").whereIn("id", (int?)1, null, (int?)2),
                In(2, "1", "2"));

            yield return Case(
                "whereIn.value.long? (params,含 null)",
                b => b.select("*").from("t").whereIn("n", (long?)100L, null, (long?)200L),
                In(2, "100", "200"));

            yield return Case(
                "whereIn.value.DateTime? (params,含 null)",
                b => b.select("*").from("t").whereIn("dt", (DateTime?)Dt1, null, (DateTime?)Dt2),
                In(2));

            yield return Case(
                "whereIn.value.Guid? (params,含 null)",
                b => b.select("*").from("t").whereIn("oid", (Guid?)Guid1, null, (Guid?)Guid2),
                In(2, Guid1.ToString(), Guid2.ToString()));

            yield return Case(
                "whereIn.value.bool? (params,含 null)",
                b => b.select("*").from("t").whereIn("flag", (bool?)true, null, (bool?)false),
                In(2, "True", "False"));

            yield return Case(
                "whereIn.value.int? (IReadOnlyList)",
                b => b.select("*").from("t").whereIn("id", (IReadOnlyList<int?>)new List<int?> { 3, null, 4 }),
                In(2, "3", "4"));

            yield return Case(
                "whereIn.value.Guid? (IReadOnlyList)",
                b => b.select("*").from("t").whereIn("oid", (IReadOnlyList<Guid?>)new List<Guid?> { Guid1, null, Guid2 }),
                In(2, Guid1.ToString(), Guid2.ToString()));
        }

        [Theory]
        [MemberData(nameof(WhereInValueTypeCases))]
        public void WhereIn_ValueTypes(OverloadCase valueCase)
            => AssertExpectation(valueCase);

        [Theory]
        [MemberData(nameof(WhereNotInValueTypeCases))]
        public void WhereNotIn_ValueTypes(OverloadCase valueCase)
            => AssertExpectation(valueCase);

        #endregion

        private static void AssertExpectation(OverloadCase overloadCase)
        {
            using var builder = TestDatabaseHelper.CreateSQLBuilder();
            overloadCase.Build(builder);

            var sql = ResolveSql(builder);
            var exp = overloadCase.Expectation;

            if (exp.ExpectIn)
                sql.Should().Contain(" IN ", $"[{overloadCase.OverloadPath}] should expand to IN list");
            if (exp.ExpectNotIn)
                sql.Should().Contain(" NOT IN ", $"[{overloadCase.OverloadPath}] should expand to NOT IN list");
            if (exp.ExpectOr)
                sql.Should().Contain(" OR ", $"[{overloadCase.OverloadPath}] should expand to OR group");
            if (exp.ExpectIsNull)
                sql.Should().Contain(" IS NULL", $"[{overloadCase.OverloadPath}] should include IS NULL");

            foreach (var fragment in exp.ContainsFragments)
            {
                sql.Should().Contain(fragment, $"[{overloadCase.OverloadPath}] missing fragment '{fragment}'");
            }

            if (exp.MinListElements > 0 && (exp.ExpectIn || exp.ExpectNotIn))
            {
                CountInListElements(sql, exp.ExpectNotIn).Should().BeGreaterThanOrEqualTo(
                    exp.MinListElements,
                    $"[{overloadCase.OverloadPath}] should not treat collection as single IN value");
            }

            if (exp.MinOrConditions > 0)
            {
                CountOrConditions(sql).Should().BeGreaterThanOrEqualTo(
                    exp.MinOrConditions,
                    $"[{overloadCase.OverloadPath}] should expand to multiple OR conditions");
            }
        }

        private static string ResolveSql(SQLBuilder builder)
        {
            var cmd = builder.toSelect();
            cmd.Should().NotBeNull();
            return cmd.para != null ? cmd.para.ResolveDelayParas(cmd.sql) : (cmd.sql ?? "");
        }

        private static int CountInListElements(string sql, bool notIn)
        {
            var pattern = notIn ? @"NOT\s+IN\s*\((?<body>[^)]*)\)" : @"\bIN\s*\((?<body>[^)]*)\)";
            var match = Regex.Match(sql, pattern, RegexOptions.IgnoreCase);
            if (!match.Success)
                return 0;

            var body = match.Groups["body"].Value.Trim();
            if (body.Length == 0 || body == "1=2" || body == "1=1")
                return 0;

            return body.Split(',').Length;
        }

        private static int CountOrConditions(string sql)
        {
            var matches = Regex.Matches(sql, @"\sOR\s", RegexOptions.IgnoreCase);
            return matches.Count + 1;
        }

        private static OverloadCase Case(string path, Action<SQLBuilder> build, WhereSqlExpectation expectation)
            => new(path, build, expectation);

        private static WhereSqlExpectation In(int minElements, params string[] fragments)
            => new(ExpectIn: true, MinListElements: minElements, ContainsFragments: fragments);

        private static WhereSqlExpectation NotIn(int minElements, params string[] fragments)
            => new(ExpectNotIn: true, MinListElements: minElements, ContainsFragments: fragments);

        private static WhereSqlExpectation NotInOrNull(int minElements, params string[] fragments)
            => new(ExpectNotIn: true, ExpectIsNull: true, MinListElements: minElements, ContainsFragments: fragments);

        private static WhereSqlExpectation Or(int minConditions)
            => new(ExpectOr: true, MinOrConditions: minConditions);
    }
}
