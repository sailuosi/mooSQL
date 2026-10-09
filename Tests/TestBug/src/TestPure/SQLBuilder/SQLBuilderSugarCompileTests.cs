using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// <see cref="SQLBuilder.sugar.cs"/> 语法糖编译覆盖：按类别调用全部重载，签名/重载决议错误在编译期暴露。
    /// 运行时可切换 <see cref="TestDatabaseHelper.Kind"/>（Step / Prepare）验证两套实现均可编译调用。
    /// </summary>
    public sealed class SQLBuilderSugarCompileTests
    {
        private static SQLBuilder NewBuilder() => TestDatabaseHelper.CreateSQLBuilder();

        #region where 重载链

        [Fact]
        public void WhereOverloadChain_Sugar_Compiles()
        {
            using var b = NewBuilder();
            b.from("t")
                .where("a", 1)
                .where("a", 1, ">")
                .where("a", 1, ">", false)
                .where("a", 1, typeof(int))
                .where("a", 1, ">", typeof(int))
                .where("a", w => w.select("1").from("x"));
        }

        #endregion

        #region 比较 / NULL / Exist 简写

        [Fact]
        public void WhereComparisonNullExist_Sugar_Compiles()
        {
            using var b = NewBuilder();
            b.from("t")
                .whereGreaterThan("a", 1)
                .whereLessThan("a", 2)
                .whereGreaterThanOrEqual("a", 3)
                .whereLessThanOrEqual("a", 4)
                .whereNotEqual("a", 5)
                .whereIsNull("a")
                .whereIsNotNull("a")
                .whereNotExist("select 1 from x")
                .whereIf(true, "flag")
                .whereIf(false, "skip");
        }

        #endregion

        #region OrNull 组合

        [Fact]
        public void WhereOrNull_Sugar_Compiles()
        {
            using var b = NewBuilder();
            b.from("t")
                .whereIsOrNull("a", 1)
                .whereIsNullOR("a", 2, ">")
                .whereVsOrNull("a", 3, "<>")
                .whereNotLikeOrNull("name", "%x%")
                .whereNotLikeLeftOrNull("code", "pre")
                .whereNotInOrNull("id", new List<int> { 1, 2 })
                .whereNotInOrNull("code", (IReadOnlyList<string>)new ReadOnlyCollection<string>(new[] { "a", "b" }));
        }

        #endregion

        #region whereIn / whereNotIn / whereOR

        [Fact]
        public void WhereInWhereNotInWhereOR_Sugar_Compiles()
        {
            using var b = NewBuilder();
            var nonGeneric = new ArrayList { 1, 2, 3 };
            var intList = new List<int> { 1, 2 };
            var objList = new List<object> { "a", 1 };
            IReadOnlyList<int> roInt = new ReadOnlyCollection<int>(new[] { 1, 2 });
            IReadOnlyList<string> roStr = new ReadOnlyCollection<string>(new[] { "x", "y" });

            b.from("t")
                .whereIn("id", nonGeneric)
                .whereIn("id", (IEnumerable<int>)intList)
                .whereIn("code", "a", "b")
                .whereIn("id", 1, 2, 3)
                .whereIn("flag", true, false)
                .whereIn("oid", Guid.NewGuid(), Guid.NewGuid())
                .whereIn("id", (int?)1, (int?)2, null)
                .whereIn("id", intList)
                .whereIn("mix", objList)
                .whereIn("id", roInt)
                .whereNotIn("code", "x", "y")
                .whereNotIn("id", 4, 5)
                .whereNotIn("id", (int?)10, null)
                .whereNotIn("id", intList)
                .whereNotIn("code", roStr)
                .whereNotIn("id", (IEnumerable<int>)new HashSet<int> { 1, 2 })
                .whereNotInOrNull("id", intList)
                .whereNotInOrNull("code", roStr)
                .whereIn("id", w => w.select("id").from("src"))
                .whereNotIn("id", w => w.select("id").from("blk"))
                .whereExist(w => w.select("1").from("e"))
                .whereNotExist(w => w.select("1").from("ne"))
                .whereOR("code", "a", "b")
                .whereOR("id", 1, 2, 3)
                .whereOR("id", (int?)1, (int?)2, null);
        }

        #endregion

        #region 多字段 / Like 简写

        [Fact]
        public void WhereMultiFieldLike_Sugar_Compiles()
        {
            using var b = NewBuilder();
            var fields = new[] { "a", "b", "c" };
            b.from("t")
                .whereAnyFieid(fields, 1)
                .whereAnyFieid(fields, 2, ">")
                .whereAnyFieldIs(3, "x", "y")
                .whereAllFieid(fields, 4)
                .whereAllFieid(fields, 5, "<>")
                .whereLikeLefts("code", "pre", "mid")
                .whereLikeLefts("code", new[] { "a", "b" }, false);
        }

        #endregion

        #region JOIN / UNION / CTE

        [Fact]
        public void JoinUnionCte_Sugar_Compiles()
        {
            using var b = NewBuilder();
            b.select("*").from("u")
                .leftJoin("orders o on o.uid = u.id")
                .innerJoin("items i on i.oid = o.id")
                .leftJoin("o", t => t.select("uid").from("orders"))
                .innerJoin("i", t => t.select("oid").from("items"))
                .rightJoin("r", t => t.select("id").from("refs"))
                .withAs("tmp", w => w.select("id").from("users"))
                .unionAll()
                .unionAll(false, "u2");
        }

        #endregion

        #region SELECT / SET / Merge

        [Fact]
        public void SelectSetMerge_Sugar_Compiles()
        {
            using var b = NewBuilder();
            b.select("*").from("t")
                .top(10);

#pragma warning disable CS0618
            b.orderby("id desc");
#pragma warning restore CS0618

            b.setTable("t")
                .set("name", "value")
                .set("name", "longname", 4)
                .setToNull("remark")
                .setI("ins", 1)
                .setI("ins2", 2, false)
                .setU("upd", 3)
                .setU("upd2", 4, true);

            b.mergeAs("s")
                .mergeUsing("u", "src")
                .mergeUsing("u2", u => u.select("id").from("src2"));
        }

        #endregion

        #region Window / ifs / 分组括号

        [Fact]
        public void WindowIfsGrouping_Sugar_Compiles()
        {
            using var b = NewBuilder();
            b.from("t")
                .ifs(true, () => b.where("a", 1))
                .ifs(false, () => b.where("b", 2), () => b.where("c", 3))
                .or(w => w.where("x", 1).where("y", 2))
                .and(w => w.where("p", 1).where("q", 2))
                .orLeft()
                .where("m", 1)
                .orRight()
                .andLeft()
                .where("n", 2)
                .andRight();

            WindowBuilder w1 = b.over("SUM(amt)");
            WindowBuilder w2 = b.windowRowNumber();
            WindowBuilder w3 = b.windowRank();
            WindowBuilder w4 = b.windowDenseRank();
            Assert.NotNull(w1);
            Assert.NotNull(w2);
            Assert.NotNull(w3);
            Assert.NotNull(w4);
        }

        #endregion
    }
}
