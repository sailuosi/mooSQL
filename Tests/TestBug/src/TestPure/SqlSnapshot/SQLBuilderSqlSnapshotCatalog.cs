using System;
using System.Collections.Generic;
using mooSQL.data;

namespace mooSQL.Pure.Tests.SqlSnapshot
{
    /// <summary>
    /// SQLBuilder toXxx 出口快照用例目录（不执行，只生成 SQL）。
    /// </summary>
    public static class SQLBuilderSqlSnapshotCatalog
    {
        public sealed record Case(string Name, Action<SQLBuilder> Build, string ToXxx = "toSelect",
            DataBaseType DbType = DataBaseType.SQLite);

        public sealed record MergeCase(string Name, Func<SQLBuilder, SQLCmd> Build,
            DataBaseType DbType = DataBaseType.MSSQL);

        public static IEnumerable<Case> All()
        {
            foreach (var c in SelectBasics()) yield return c;
            foreach (var c in FormatJoins()) yield return c;
            foreach (var c in WhereBasics()) yield return c;
            foreach (var c in WhereLike()) yield return c;
            foreach (var c in WhereInBetween()) yield return c;
            foreach (var c in WhereNullOrExist()) yield return c;
            foreach (var c in WhereCompose()) yield return c;
            foreach (var c in WhereSubquery()) yield return c;
            foreach (var c in SubqueryFromSelect()) yield return c;
            foreach (var c in CteUnion()) yield return c;
            foreach (var c in Dml()) yield return c;
            foreach (var c in OtherExports()) yield return c;
            foreach (var c in FluentMerge()) yield return c;
            foreach (var c in WindowAndMisc()) yield return c;
        }

        public static IEnumerable<MergeCase> Merges()
        {
            yield return new MergeCase("merge_table_set", kit => kit
                .mergeInto("tgt", "t")
                .from("src", "s")
                .on("t.id=s.id")
                .set("name", "s.name", false)
                .setI("id", "s.id", false)
                .toMergeInto());

            yield return new MergeCase("merge_from_subquery", kit => kit
                .mergeInto("bill", "b")
                .from("r", r => r
                    .select("*")
                    .from("src a")
                    .whereIn("a.status", "A", "B"))
                .on("r.oid=b.oid")
                .set("amt", "r.amt", false)
                .setI("oid", "r.oid", false)
                .toMergeInto());

            yield return new MergeCase("merge_when_match_update_insert", kit => kit
                .mergeInto("tgt")
                .from("src", "s")
                .on("tgt.id=s.id")
                .whenMatchThenUpdate(u => u.set("name", "s.name", false))
                .whenNotMatchThenInsert(i => i.set("id", "s.id", false).set("name", "s.name", false))
                .toMergeInto());

            yield return new MergeCase("merge_when_match_delete", kit => kit
                .mergeInto("tgt")
                .from("src", "s")
                .on("tgt.id=s.id")
                .whenMatchThenDelete()
                .toMergeInto());

            yield return new MergeCase("merge_setU_setI_split", kit => kit
                .mergeInto("tgt", "t")
                .from("src", "s")
                .on("t.id=s.id")
                .setI("id", "s.id", false)
                .setU("name", "s.name", false)
                .set("flag", "s.flag", false)
                .toMergeInto());
        }

        private static IEnumerable<Case> FluentMerge()
        {
            // SQLBuilder 链式 mergeUsing / mergeOn / toMergeInto（MSSQL）
            yield return C("merge_fluent_using_table", k => k
                .mergeAs("t")
                .mergeUsing("s", "src")
                .mergeOn("t.id=s.id")
                .setTable("tgt")
                .set("name", "s.name", false)
                .setI("id", "s.id", false),
                "toMergeInto", DataBaseType.MSSQL);

            yield return C("merge_fluent_using_select", k => k
                .mergeAs("t")
                .mergeUsing("s", s => s.select("id, name").from("src").where("flag", 1))
                .mergeOn("t.id=s.id")
                .setTable("tgt")
                .set("name", "s.name", false),
                "toMergeInto", DataBaseType.MSSQL);
        }

        private static IEnumerable<Case> WindowAndMisc()
        {
            yield return C("row_number", k => k.select("id").from("users").rowNumber("id").orderBy("id"));
            yield return C("row_number_as", k => k.select("id").from("users").rowNumber("id", "rn"));
            yield return C("join_on_columns", k => k.select("*").from("a").join("b", "a.id", "b.aid"));
            yield return C("where_frag", k =>
            {
                var frag = new WhereFrag { key = "id", op = "=", value = 1, paramed = true };
                k.select("*").from("t").where(frag);
            });
            yield return C("set_to_null_update", k => k.setTable("users").setToNull("name").where("id", 1), "toUpdate");
            yield return C("ifs_skip_where", k => k.select("*").from("t").ifs(false).where("id", 1).ifs(true).where("x", 2));
            yield return C("skip_take_parts", k => k.select("id").from("users").orderBy("id").skip(5).take(3));
        }

        private static IEnumerable<Case> SelectBasics()
        {
            yield return C("select_from_basic", k => k.select("id, name").from("users"));
            yield return C("select_distinct_order_top", k => k.distinct().select("name").from("users").orderBy("id desc").top(5));
            yield return C("select_groupby_having", k => k.select("age").select("COUNT(*) c").from("users").groupBy("age").having("COUNT(*) > 1"));
            yield return C("select_setpage", k => k.select("id").from("users").orderBy("id").setPage(10, 2));
            yield return C("select_skiptake", k => k.select("id").from("users").orderBy("id").skipTake(10, 5));
            yield return C("select_with_from_append", k => k.from("a").selectWith(s => s.from("b")).select("*"));
            yield return C("select_prefix", k => k.prefix("/*h*/").select("id").from("users"));
            yield return C("select_multi_from", k => k.select("*").from("users u").from("roles r"));
        }

        private static IEnumerable<Case> FormatJoins()
        {
            yield return C("format_select_from_join", k => k
                .selectFormat("u.id, u.{0}", "name")
                .fromFormat("users_{0} u", "2024")
                .joinFormat("LEFT JOIN orders_{0} o ON o.uid=u.id", "2024"));

            yield return C("join_raw_left_inner", k => k
                .select("u.*")
                .from("users u")
                .leftJoin("orders o on o.uid=u.id")
                .innerJoin("roles r on r.id=u.role_id"));

            yield return C("join_left_subquery", k => k
                .select("u.id")
                .from("users u")
                .leftJoin("o", t => t.select("uid, COUNT(*) c").from("orders").groupBy("uid")));

            yield return C("join_inner_subquery", k => k
                .select("u.id")
                .from("users u")
                .innerJoin("v", t => t.select("uid").from("vip")));

            yield return C("join_right_subquery", k => k
                .select("u.id")
                .from("users u")
                .rightJoin("o", t => t.select("uid").from("orders")));

            yield return C("join_key_subquery", k => k
                .select("u.*")
                .from("users u")
                .join("LEFT JOIN", "o", t => t.select("*").from("orders").where("amt", 0, ">")));
        }

        private static IEnumerable<Case> WhereBasics()
        {
            yield return C("where_raw", k => k.select("*").from("t").where("1=1"));
            yield return C("where_eq", k => k.select("*").from("t").where("id", 1));
            yield return C("where_op", k => k.select("*").from("t").where("age", 18, ">="));
            yield return C("where_op_noparam", k => k.select("*").from("t").where("flag", "1", "=", false));
            yield return C("where_gt_lt_ge_le_ne", k => k.select("*").from("t")
                .whereGreaterThan("a", 1)
                .whereLessThan("b", 2)
                .whereGreaterThanOrEqual("c", 3)
                .whereLessThanOrEqual("d", 4)
                .whereNotEqual("e", 5));
            yield return C("where_if_true", k => k.select("*").from("t").whereIf(true, "id", 1));
            yield return C("where_if_false", k => k.select("*").from("t").whereIf(false, "id", 1).where("x", 1));
            yield return C("where_if_raw", k => k.select("*").from("t").whereIf(true, "status=1"));
            yield return C("where_guid", k => k.select("*").from("t").whereGuid("oid", Guid.Parse("11111111-1111-1111-1111-111111111111")));
            yield return C("where_format", k => k.select("*").from("t").whereFormat("id={0} and name={1}", 1, "n"));
            yield return C("where_typed", k => k.select("*").from("t").where("age", 18, typeof(int)));
            yield return C("where_typed_op", k => k.select("*").from("t").where("age", 18, ">", typeof(int)));
        }

        private static IEnumerable<Case> WhereLike()
        {
            yield return C("where_like", k => k.select("*").from("t").whereLike("name", "ab"));
            yield return C("where_like_left", k => k.select("*").from("t").whereLikeLeft("name", "ab"));
            yield return C("where_not_like", k => k.select("*").from("t").whereNotLike("name", "ab"));
            yield return C("where_not_like_left", k => k.select("*").from("t").whereNotLikeLeft("name", "ab"));
            yield return C("where_likes_keys", k => k.select("*").from("t").whereLikes(new[] { "a", "b" }, "x"));
            yield return C("where_likes_vals_or", k => k.select("*").from("t").whereLikes("name", new[] { "a", "b" }, true));
            yield return C("where_likes_vals_and", k => k.select("*").from("t").whereLikes("name", new[] { "a", "b" }, false));
            yield return C("where_likes_or_params", k => k.select("*").from("t").whereLikesOr("name", "a", "b"));
            yield return C("where_likes_and_params", k => k.select("*").from("t").whereLikesAnd("name", "a", "b"));
            yield return C("where_like_lefts_enum", k => k.select("*").from("t").whereLikeLefts("code", new[] { "A", "B" }));
            yield return C("where_like_lefts_params", k => k.select("*").from("t").whereLikeLefts("code", "A", "B"));
            yield return C("where_not_like_lefts", k => k.select("*").from("t").whereNotLikeLefts("code", new[] { "A", "B" }));
            yield return C("where_not_like_or_null", k => k.select("*").from("t").whereNotLikeOrNull("name", "ab"));
            yield return C("where_not_like_left_or_null", k => k.select("*").from("t").whereNotLikeLeftOrNull("name", "ab"));
        }

        private static IEnumerable<Case> WhereInBetween()
        {
            yield return C("where_in_params", k => k.select("*").from("t").whereIn("id", 1, 2, 3));
            yield return C("where_in_list", k => k.select("*").from("t").whereIn("id", new List<int> { 1, 2 }));
            yield return C("where_in_ienum", k => k.select("*").from("t").whereIn("id", (IEnumerable<int>)new[] { 1, 2 }));
            yield return C("where_in_objects", k => k.select("*").from("t").whereIn("id", new List<object> { 1, 2 }));
            yield return C("where_notin_params", k => k.select("*").from("t").whereNotIn("id", 1, 2));
            yield return C("where_notin_list", k => k.select("*").from("t").whereNotIn("id", new List<int> { 1, 2 }));
            yield return C("where_notin_or_null", k => k.select("*").from("t").whereNotInOrNull("id", new[] { 1, 2 }));
            yield return C("where_or_params", k => k.select("*").from("t").whereOR("id", 1, 2, 3));
            yield return C("where_between", k => k.select("*").from("t").whereBetween("age", 10, 20));
            yield return C("where_not_between", k => k.select("*").from("t").whereNotBetween("age", 10, 20));
            yield return C("where_in_guid", k => k.select("*").from("t").whereInGuid("oid",
                new[] { Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.Parse("22222222-2222-2222-2222-222222222222") }));
            yield return C("where_in_guid_nullable", k => k.select("*").from("t").whereInGuid("oid",
                new Guid?[] { Guid.Parse("11111111-1111-1111-1111-111111111111"), null }));
            yield return C("where_in_guid_string", k => k.select("*").from("t").whereInGuid("oid",
                new[] { "11111111-1111-1111-1111-111111111111" }));
            yield return C("where_fields_and", k => k.select("*").from("t").whereFields(new[] { "a", "b" }, 1, 0, "="));
            yield return C("where_fields_or", k => k.select("*").from("t").whereFields(new[] { "a", "b" }, 1, 1, "="));
            yield return C("where_any_field", k => k.select("*").from("t").whereAnyFieid(new[] { "a", "b" }, 1));
            yield return C("where_any_field_is", k => k.select("*").from("t").whereAnyFieldIs(1, "a", "b"));
            yield return C("where_all_field", k => k.select("*").from("t").whereAllFieid(new[] { "a", "b" }, 1));
            yield return C("where_list_op", k => k.select("*").from("t").whereList("id", " IN ", new[] { 1, 2 }));
        }

        private static IEnumerable<Case> WhereNullOrExist()
        {
            yield return C("where_is_null", k => k.select("*").from("t").whereIsNull("name"));
            yield return C("where_is_not_null", k => k.select("*").from("t").whereIsNotNull("name"));
            yield return C("where_is_or_null", k => k.select("*").from("t").whereIsOrNull("id", 1));
            yield return C("where_is_null_or", k => k.select("*").from("t").whereIsNullOR("id", 1, "="));
            yield return C("where_vs_or_null", k => k.select("*").from("t").whereVsOrNull("age", 18, ">="));
            yield return C("where_exist_raw", k => k.select("*").from("t").whereExist("select 1 from dual"));
            yield return C("where_not_exist_raw", k => k.select("*").from("t").whereNotExist("select 1 from dual"));
            yield return C("where_exist_action", k => k.select("id").from("users u")
                .whereExist(t => t.select("1").from("orders o").where("o.uid=u.id")));
            yield return C("where_not_exist_action", k => k.select("id").from("users u")
                .whereNotExist(t => t.select("1").from("banned b").where("b.uid=u.id")));
        }

        private static IEnumerable<Case> WhereCompose()
        {
            yield return C("where_and_or_chain", k => k.select("*").from("t")
                .where("a", 1).or().where("b", 2).and().where("c", 3));
            yield return C("where_sink_or_rise", k => k.select("*").from("t")
                .sinkOR().where("a", 1).where("b", 2).rise().where("c", 3));
            yield return C("where_sink_not", k => k.select("*").from("t")
                .sinkNot().where("a", 1).where("b", 2).rise());
            yield return C("where_sink_not_or", k => k.select("*").from("t")
                .sinkNotOR().where("a", 1).where("b", 2).rise());
            yield return C("where_not_flag", k => k.select("*").from("t").not().where("a", 1));
            yield return C("where_or_action", k => k.select("*").from("t")
                .where("x", 1).or(w => w.where("a", 1).where("b", 2)));
            yield return C("where_and_action", k => k.select("*").from("t")
                .where("x", 1).and(w => w.where("a", 1).or().where("b", 2)));
            yield return C("where_or_group_action", k => k.select("*").from("t")
                .whereOR(w => w.where("a", 1).where("b", 2)));
            yield return C("where_action_fragment", k => k.select("*").from("t")
                .where(w => w.where("id", 1).or().where("id", 2)));
            yield return C("where_pin", k => k.select("*").from("t").where("a", 1).pin("OR b=2"));
            yield return C("where_or_left_right", k => k.select("*").from("t").orLeft().where("a", 1).where("b", 2).orRight());
            yield return C("where_and_left_right", k => k.select("*").from("t").andLeft().where("a", 1).where("b", 2).andRight());
        }

        private static IEnumerable<Case> WhereSubquery()
        {
            yield return C("where_in_subquery", k => k.select("id").from("users u")
                .whereIn("u.id", t => t.select("user_id").from("orders").where("amt", 0, ">")));
            yield return C("where_notin_subquery", k => k.select("id").from("users u")
                .whereNotIn("u.id", t => t.select("uid").from("banned")));
            yield return C("where_key_subquery", k => k.select("*").from("t")
                .where("id", s => s.select("MAX(id)").from("t2")));
            yield return C("where_key_op_subquery", k => k.select("*").from("t")
                .where("id", ">", s => s.select("MIN(id)").from("t2")));
        }

        private static IEnumerable<Case> SubqueryFromSelect()
        {
            yield return C("from_subquery", k => k.select("a.*")
                .from("a", t => t.select("*").from("users").where("id", 1)));
            yield return C("from_subquery_nested_where_action", k => k.select("a.*")
                .from("a", t => t.select("*").from("users")
                    .where(w => w.where("id", 1).or().where("id", 2))));
            yield return C("from_subquery_where_in", k => k.select("x.id")
                .from("x", outer => outer.select("id").from("users")
                    .whereIn("id", inner => inner.select("uid").from("vip"))));
            yield return C("select_column_subquery", k => k
                .select("u.id")
                .select("cnt", t => t.select("COUNT(*)").from("orders o").where("o.uid=u.id"))
                .from("users u"));
            yield return C("from_subquery_top", k => k.select("a.name")
                .from("a", t => t.select("name").from("student").orderBy("id desc").top(1)),
                dbType: DataBaseType.MSSQL);
        }

        private static IEnumerable<Case> CteUnion()
        {
            yield return C("cte_with_select_action", k => k
                .withSelect("cte1", b => b.select("id").from("t").where("id", 1))
                .select("c.*")
                .from("cte1 c"));

            yield return C("cte_with_select_sql", k => k
                .withSelect("cte1", "select id from t where id=1")
                .select("*")
                .from("cte1"));

            yield return C("cte_with_as", k => k
                .withAs("tmp", b => b.select("id").from("users"))
                .select("*")
                .from("tmp"));

            yield return C("union_two_queries", k => k
                .select("id").from("a")
                .union(u => u.select("id").from("b")));

            yield return C("union_all_wrap", k => k
                .select("id").from("a")
                .unionAll()
                .select("id").from("b"));

            yield return C("cte_recur_basic", k => k
                .withRecur("tree", r => r
                    .fromRoot("org", "o")
                    .fromNext("org", "tar")
                    .joinOn("pid", "id")
                    .select("id")
                    .select("name")
                    .select("pid")
                    .whereRoot((w, _) => w.where("o.pid", 0)))
                .select("*")
                .from("tree"),
                dbType: DataBaseType.MSSQL);

            // 业务：组织树向上递归（ParentOID→OID）+ lvType 差异列 + apply 后外层去重子查询
            // 权限 useDuty 分支简化为固定 where；修 withRecurTo 门面衔接时以此 SQL 为回归锚
            yield return C("cte_recur_to_org_parent_root", k =>
            {
                var commFields =
                    "OrgName,ClassCode,Varchar1,ORG_FLG,Varchar7,Varchar3,Int2,ACTIVE_FLAG,Boolean1,OrgNO";
                k.withRecurTo("O")
                    .fromRoot("UCML_Organize")
                    .joinOn("ParentOID", "UCML_OrganizeOID")
                    .selectDeep("tDeepNum")
                    .select("CAST( 'root' as varchar(50))", "CAST( 'parent' as varchar(50))", "lvType")
                    .select(commFields)
                    .whereRoot((r, _) => r.where("src.UCML_OrganizeOID", "00000000-0000-0000-0000-000000000001"))
                    .apply()
                    .from("p", p =>
                    {
                        p.select(
                                "* ,ROW_NUMBER()over (partition by UCML_OrganizeOID  order by Varchar1) n,(select COUNT(*) from UCML_Organize n where n.ParentOID=o.UCML_OrganizeOID) as childcc")
                            .from("o");
                    })
                    .where("p.n=1");
            }, dbType: DataBaseType.MSSQL);

            // 业务用例2：向下递归（OID→ParentOID）+ whereNext 深度限制 + apply 后直接 select/from/where
            // 条件字面量按业务原文固化（含 o.UCML_OrganizeOI）
            yield return C("cte_recur_to_org_children", k =>
            {
                var commFields =
                    "OrgName,ClassCode,Varchar1,ORG_FLG,Varchar7,Varchar3,Int2,ACTIVE_FLAG,Boolean1,OrgNO";
                const string rootId = "00000000-0000-0000-0000-000000000001";
                const int deep = 3;
                k.withRecurTo("o")
                    .select(commFields)
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
            }, dbType: DataBaseType.MSSQL);
        }

        private static IEnumerable<Case> Dml()
        {
            yield return C("insert_set", k => k.setTable("users").set("name", "n").set("age", 1), "toInsert");
            yield return C("insert_multirow", k => k.setTable("users")
                .set("name", "a").newRow().set("name", "b"), "toInsert");
            yield return C("insert_from", k => k.setTable("users_bak")
                .select("id, name").from("users").where("id", 1, ">"), "toInsertFrom");
            yield return C("update_set", k => k.setTable("users")
                .set("name", "n2").where("id", 1), "toUpdate");
            yield return C("update_from", k => k.setTable("users")
                .set("name", "s.name", false)
                .from("src s")
                .where("users.id=s.id"), "toUpdateFrom");
            yield return C("delete_where", k => k.from("users").where("id", 1), "toDelete");
            yield return C("delete_where_guid", k => k.setTable("users")
                .whereGuid("oid", Guid.Parse("11111111-1111-1111-1111-111111111111")), "toDelete");
            yield return C("delete_where_in_guid", k => k.setTable("users").whereInGuid("oid",
                new[] { Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.Parse("22222222-2222-2222-2222-222222222222") }), "toDelete");
            yield return C("delete_where_guid_invalid", k => k.setTable("users").whereGuid("oid", "not-a-guid"), "toDelete");
            yield return C("delete_where_in_guid_empty", k => k.setTable("users").whereInGuid("oid", Array.Empty<Guid>()), "toDelete");
            yield return C("insert_setI_setU", k => k.setTable("users")
                .setI("id", 1).setU("name", "n").set("age", 2), "toInsert");
            yield return C("update_setU_only", k => k.setTable("users")
                .setI("id", 1).setU("name", "n").set("age", 2).where("id", 1), "toUpdate");
        }

        private static IEnumerable<Case> OtherExports()
        {
            yield return C("to_select_count", k => k.select("id").from("users").where("id", 1), "toSelectCount");
            yield return C("to_select_exist", k => k.select("id").from("users").where("id", 1), "toSelectExist");
            yield return C("to_insert_with_duplicate", k => k
                    .setTable("users").set("id", 1).set("name", "n").setU("name", "n2"),
                "toInsertWithDuplicateUpdate", DataBaseType.MySQL);
        }

        private static Case C(string name, Action<SQLBuilder> build, string toXxx = "toSelect",
            DataBaseType dbType = DataBaseType.SQLite)
            => new(name, build, toXxx, dbType);
    }
}
