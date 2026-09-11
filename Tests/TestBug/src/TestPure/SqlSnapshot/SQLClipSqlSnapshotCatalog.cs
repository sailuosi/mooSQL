using System;
using System.Collections.Generic;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;

namespace mooSQL.Pure.Tests.SqlSnapshot
{
    /// <summary>
    /// SQLClip 对齐补齐 API 的 toSelect 快照目录（不执行）。
    /// </summary>
    public static class SQLClipSqlSnapshotCatalog
    {
        public sealed record Case(string Name, Action<SQLClip> Build);

        public static IEnumerable<Case> All()
        {
            foreach (var c in P0Fixes()) yield return c;
            foreach (var c in WhereParity()) yield return c;
            foreach (var c in JoinPageParity()) yield return c;
            foreach (var c in SubqueryParity()) yield return c;
        }

        private static Case C(string name, Action<SQLClip> build) => new(name, build);

        private static IEnumerable<Case> P0Fixes()
        {
            yield return C("whereif_true", clip =>
            {
                clip.from<TestUser>(out var u);
                clip.whereIf(true, () => u.Id, 1);
                clip.select(u);
            });
            yield return C("whereif_false", clip =>
            {
                clip.from<TestUser>(out var u);
                clip.whereIf(false, () => u.Id, 1);
                clip.select(u);
            });
            yield return C("whereif_op", clip =>
            {
                clip.from<TestUser>(out var u);
                clip.whereIf(true, () => u.Age, 18, ">=");
                clip.select(u);
            });
            yield return C("groupby_field", clip =>
            {
                clip.from<TestUser>(out var u);
                clip.select(() => u.Age).groupBy(() => u.Age);
            });
        }

        private static IEnumerable<Case> WhereParity()
        {
            yield return C("where_is_null_or", clip =>
            {
                clip.from<TestUser>(out var u);
                clip.whereIsNullOR(() => u.Age, 18, ">=");
                clip.select(u);
            });
            yield return C("where_vs_or_null", clip =>
            {
                clip.from<TestUser>(out var u);
                clip.whereVsOrNull(() => u.Age, 30, "<");
                clip.select(u);
            });
            yield return C("where_not_like_left", clip =>
            {
                clip.from<TestUser>(out var u);
                clip.whereNotLikeLeft(() => u.Name, "A");
                clip.select(u);
            });
            yield return C("where_not_like_or_null", clip =>
            {
                clip.from<TestUser>(out var u);
                clip.whereNotLikeOrNull(() => u.Name, "x");
                clip.select(u);
            });
            yield return C("where_not_like_left_or_null", clip =>
            {
                clip.from<TestUser>(out var u);
                clip.whereNotLikeLeftOrNull(() => u.Name, "A");
                clip.select(u);
            });
            yield return C("where_not_in_or_null", clip =>
            {
                clip.from<TestUser>(out var u);
                clip.whereNotInOrNull(() => u.Id, new[] { 1, 2 });
                clip.select(u);
            });
            yield return C("where_likes_multi", clip =>
            {
                clip.from<TestUser>(out var u);
                clip.whereLikes(() => u.Name, new[] { "Al", "Bo" });
                clip.select(u);
            });
            yield return C("where_like_lefts", clip =>
            {
                clip.from<TestUser>(out var u);
                clip.whereLikeLefts(() => u.Name, "A", "B");
                clip.select(u);
            });
            yield return C("where_exist_clip", clip =>
            {
                clip.from<TestUser>(out var u);
                clip.whereExist(sub =>
                {
                    sub.from<TestOrder>(out var o);
                    sub.where(() => o.UserId, 1);
                    return sub.select(() => o.Id);
                });
                clip.select(u);
            });
            yield return C("where_not_exist_clip", clip =>
            {
                clip.from<TestUser>(out var u);
                clip.whereNotExist(sub =>
                {
                    sub.from<TestOrder>(out var o);
                    sub.where(() => o.UserId, 99);
                    return sub.select(() => o.Id);
                });
                clip.select(u);
            });
            yield return C("where_not_in_builder_sub", clip =>
            {
                clip.from<TestUser>(out var u);
                clip.whereNotIn(() => u.Id, b => b.select("id").from("test_orders"));
                clip.select(u);
            });
            yield return C("sink_not", clip =>
            {
                clip.from<TestUser>(out var u);
                clip.sinkNot().where(() => u.Id, 1).where(() => u.Id, 2).rise();
                clip.select(u);
            });
            yield return C("sink_not_or", clip =>
            {
                clip.from<TestUser>(out var u);
                clip.sinkNotOR().where(() => u.Id, 1).where(() => u.Id, 2).rise();
                clip.select(u);
            });
            yield return C("clear_where", clip =>
            {
                clip.from<TestUser>(out var u);
                clip.where(() => u.Id, 1).clearWhere().where(() => u.Id, 2);
                clip.select(u);
            });
        }

        private static IEnumerable<Case> JoinPageParity()
        {
            yield return C("inner_join", clip =>
            {
                clip.from<TestUser>(out var u);
                clip.InnerJoin<TestOrder>(out var o).on(() => o.UserId == u.Id);
                clip.select(() => new { u.Id, o.OrderNo });
            });
            yield return C("right_join_sub", clip =>
            {
                clip.from<TestUser>(out var u);
                clip.RightJoin<TestOrder>(out var o, sub =>
                {
                    sub.from<TestOrder>(out var x);
                    sub.where(() => x.UserId, 1);
                    return sub.select(x);
                }).on(() => o.UserId == u.Id);
                clip.select(() => new { UserId = u.Id, OrderId = o.Id });
            });
            yield return C("skip_take", clip =>
            {
                clip.from<TestUser>(out var u);
                clip.orderBy(() => u.Id);
                clip.select(u).skipTake(1, 2);
            });
            yield return C("set_page", clip =>
            {
                clip.from<TestUser>(out var u);
                clip.orderBy(() => u.Id);
                clip.select(u).setPage(2, 1);
            });
        }

        private static IEnumerable<Case> SubqueryParity()
        {
            yield return C("from_subquery", clip =>
            {
                clip.from<TestUser>(out var u, sub =>
                {
                    sub.from<TestUser>(out var x);
                    sub.where(() => x.IsActive, true);
                    return sub.select(x);
                });
                clip.where(() => u.Id, 1, ">=");
                clip.select(() => u.Id);
            });
            yield return C("select_col_subquery", clip =>
            {
                clip.from<TestUser>(out var u);
                clip.select(() => u.Id);
                clip.select<int>("ord_cnt", sub =>
                {
                    sub.from<TestOrder>(out var o);
                    sub.where(() => o.UserId, 1);
                    return sub.select(() => o.Id);
                });
            });
        }
    }
}
