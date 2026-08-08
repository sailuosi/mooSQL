using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// 自动生成：简单构造 API 的门面入队（由 tools/gen_sqlbuilder_steps.py 生成）。
    /// </summary>
    public partial class SQLBuilder
    {
        // ---- control ----
        public new SQLBuilder ifs(bool isPass) => Enqueue(new IfsboolStep(isPass));


        // ---- select ----
        public new SQLBuilder prefix(string SQLString) => Enqueue(new PrefixstringStep(SQLString));

        public new SQLBuilder subfix(string SQLString) => Enqueue(new SubfixstringStep(SQLString));

        public new SQLBuilder copyPreSelect() => Enqueue(CopyPreSelectStep.Instance);

        public new SQLBuilder copyPreFrom() => Enqueue(CopyPreFromStep.Instance);

        public new SQLBuilder copyPreWere() => Enqueue(CopyPreWereStep.Instance);

        public new SQLBuilder selectWith(Action<SQLBuilder> queryOther) => Enqueue(new SelectWithActStep(queryOther));

        public new SQLBuilder selectWith(string queryOther) => Enqueue(new SelectWithstringStep(queryOther));

        public new SQLBuilder selectSummary(string queryOther) => Enqueue(new SelectSummarystringStep(queryOther));

        public new SQLBuilder selectFormat(string selectSQLPart, params object[] paras) => Enqueue(new SelectFormatstringobjectArrStep(selectSQLPart, paras));

        public new SQLBuilder select(string asName, Action<SQLBuilder> doColSelect) => Enqueue(new SelectstringActStep(asName, doColSelect));

        public new SQLBuilder selectUnioned(string columns) => Enqueue(new SelectUnionedstringStep(columns));

        public new SQLBuilder top(int num) => Enqueue(new TopintStep(num));

        public new SQLBuilder skipTake(int skip, int take) => Enqueue(new SkipTakeintintStep(skip, take));

        public new SQLBuilder skip(int skip) => Enqueue(new SkipintStep(skip));

        public new SQLBuilder take(int skip) => Enqueue(new TakeintStep(skip));

        public new SQLBuilder groupBy(string groupField) => Enqueue(new GroupBystringStep(groupField));

        public new SQLBuilder having(string havingStr) => Enqueue(new HavingstringStep(havingStr));

        public new SQLBuilder orderby(string orderByPart) => Enqueue(new OrderbystringStep(orderByPart));

        public new SQLBuilder rowNumber() => Enqueue(RowNumberStep.Instance);

        public new SQLBuilder rowNumberUse(string numFieldName) => Enqueue(new RowNumberUsestringStep(numFieldName));

        public new SQLBuilder rowNumber(string orderPart) => Enqueue(new RowNumberstringStep(orderPart));

        public new SQLBuilder rowNumber(string orderPart, string asName) => Enqueue(new RowNumberstringstringStep(orderPart, asName));


        // ---- set ----
        public new SQLBuilder setTable(string tbName) => Enqueue(new SetTablestringStep(tbName));

        public new SQLBuilder configSetNull(UpdateSetNullOption option) => Enqueue(new ConfigSetNullUpdateSetNullOptionStep(option));

        public new SQLBuilder set(string key, string value, int maxLength) => Enqueue(new SetstringstringintStep(key, value, maxLength));

        public new SQLBuilder set(string key, object val, bool paramed = true, Type type = null, bool updatable = true, bool insertable = true) => Enqueue(new SetstringobjectboolTypeboolboolStep(key, val, paramed, type, updatable, insertable));

        public new SQLBuilder setToNull(string fieldName) => Enqueue(new SetToNullstringStep(fieldName));

        public new SQLBuilder setI(string key, object val) => Enqueue(new SetIstringobjectStep(key, val));

        public new SQLBuilder setI(string key, object val, bool paramed) => Enqueue(new SetIstringobjectboolStep(key, val, paramed));

        public new SQLBuilder setU(string key, object val) => Enqueue(new SetUstringobjectStep(key, val));

        public new SQLBuilder setU(string key, object val, bool paramed) => Enqueue(new SetUstringobjectboolStep(key, val, paramed));

        public new SQLBuilder newRow() => Enqueue(NewRowStep.Instance);

        public new SQLBuilder addRow() => Enqueue(AddRowStep.Instance);


        // ---- merge ----
        public new SQLBuilder mergeAs(string asName) => Enqueue(new MergeAsstringStep(asName));

        public new SQLBuilder mergeUsing(string asName, Action<SQLBuilder> buildSelect) => Enqueue(new MergeUsingstringActStep(asName, buildSelect));

        public new SQLBuilder mergeUsing(string asName, string tabname) => Enqueue(new MergeUsingstringstringStep(asName, tabname));

        public new SQLBuilder mergeOn(string onPart) => Enqueue(new MergeOnstringStep(onPart));

        public new SQLBuilder mergeDelete(bool thenDelete) => Enqueue(new MergeDeleteboolStep(thenDelete));


        // ---- union ----
        public new SQLBuilder withSelect(string name, Action<SQLBuilder> doselect) => Enqueue(new WithSelectstringActStep(name, doselect));

        public new SQLBuilder withAs(string name, Action<SQLBuilder> selectBuilder) => Enqueue(new WithAsstringActStep(name, selectBuilder));

        public new SQLBuilder withRecur(string name, Action<RecurCTEBuilder> buildRecur) => Enqueue(new WithRecurstringAction_RecurCTEBuilderStep(name, buildRecur));

        public new SQLBuilder withSelect(string name, string selectSQL) => Enqueue(new WithSelectstringstringStep(name, selectSQL));

        public new SQLBuilder unionAll(bool wrapSelect = true, string wrapAsName = "tmpunioned") => Enqueue(new UnionAllboolstringStep(wrapSelect, wrapAsName));

        public new SQLBuilder union(bool isUnionAll = false, bool wrapSelect = true, string wrapAsName = "tmpunioned") => Enqueue(new UnionboolboolstringStep(isUnionAll, wrapSelect, wrapAsName));

        public new SQLBuilder unionAs(Action<SqlGoup> dogroup) => Enqueue(new UnionAsAction_SqlGoupStep(dogroup));

        public new SQLBuilder toggleToUnionOutor() => Enqueue(ToggleToUnionOutorStep.Instance);

        public new SQLBuilder union(Action<SQLBuilder> doUnion) => Enqueue(new UnionActStep(doUnion));


        // ---- from ----
        public new SQLBuilder fromFormat(string fromSQLPart, params object[] paras) => Enqueue(new FromFormatstringobjectArrStep(fromSQLPart, paras));

        public new SQLBuilder join(string joinSQLString) => Enqueue(new JoinstringStep(joinSQLString));

        public new SQLBuilder join(string targetTable, string onLeft, string onRight) => Enqueue(new JoinstringstringstringStep(targetTable, onLeft, onRight));

        public new SQLBuilder joinFormat(string JoinSQLPart, params object[] paras) => Enqueue(new JoinFormatstringobjectArrStep(JoinSQLPart, paras));

        public new SQLBuilder join(string joinKey, string joinSQLString, Action<SQLBuilder> childFromPart) => Enqueue(new JoinstringstringActStep(joinKey, joinSQLString, childFromPart));

        public new SQLBuilder leftJoin(string joinSQLString, Action<SQLBuilder> childFromPart) => Enqueue(new LeftJoinstringActStep(joinSQLString, childFromPart));

        public new SQLBuilder leftJoin(string joinSQLString) => Enqueue(new LeftJoinstringStep(joinSQLString));

        public new SQLBuilder innerJoin(string joinSQLString, Action<SQLBuilder> childFromPart) => Enqueue(new InnerJoinstringActStep(joinSQLString, childFromPart));

        public new SQLBuilder innerJoin(string joinSQLString) => Enqueue(new InnerJoinstringStep(joinSQLString));

        public new SQLBuilder rightJoin(string joinSQLString, Action<SQLBuilder> childFromPart) => Enqueue(new RightJoinstringActStep(joinSQLString, childFromPart));

        public new SQLBuilder from(string asName, Action<SQLBuilder> childFromPart) => Enqueue(new FromstringActStep(asName, childFromPart));

        public new SQLBuilder pivot(PivotItem SQLString) => Enqueue(new PivotPivotItemStep(SQLString));

        public new SQLBuilder unpivot(UnpivotItem SQLString) => Enqueue(new UnpivotUnpivotItemStep(SQLString));

        public new SQLBuilder pivot(string aggregation, string field, List<string> values, string asName) => Enqueue(new PivotstringstringListstringstringStep(aggregation, field, values, asName));

        public new SQLBuilder unpivot(string valueName, string fieldName, List<string> fields, string asName) => Enqueue(new UnpivotstringstringListstringstringStep(valueName, fieldName, fields, asName));


        // ---- misc ----
        public new SQLBuilder orLeft() => Enqueue(OrLeftStep.Instance);

        public new SQLBuilder orRight() => Enqueue(OrRightStep.Instance);

        public new SQLBuilder andLeft() => Enqueue(AndLeftStep.Instance);

        public new SQLBuilder andRight() => Enqueue(AndRightStep.Instance);

        public new SQLBuilder pinLeft() => Enqueue(PinLeftStep.Instance);

        public new SQLBuilder pinRight() => Enqueue(PinRightStep.Instance);


        // ---- where ----
        public new SQLBuilder whereIsNull(string key) => Enqueue(new WhereIsNullstringStep(key));

        public new SQLBuilder whereIsNotNull(string key) => Enqueue(new WhereIsNotNullstringStep(key));

        public new SQLBuilder where(WhereFrag frag) => Enqueue(new WhereWhereFragStep(frag));

        public new SQLBuilder pin(string SQL) => Enqueue(new PinstringStep(SQL));

        public new SQLBuilder whereOR(Action<SQLBuilder> whereBuilder) => Enqueue(new WhereORActStep(whereBuilder));

        public new SQLBuilder and() => Enqueue(AndStep.Instance);

        public new SQLBuilder or() => Enqueue(OrStep.Instance);

        public new SQLBuilder or(Action<SQLBuilder> doSomeWhere) => Enqueue(new OrActStep(doSomeWhere));

        public new SQLBuilder and(Action<SQLBuilder> doSomeWhere) => Enqueue(new AndActStep(doSomeWhere));

        public new SQLBuilder sink(string connector = "AND") => Enqueue(new SinkstringStep(connector));

        public new SQLBuilder sinkNot(string connector = "AND") => Enqueue(new SinkNotstringStep(connector));

        public new SQLBuilder sinkOR() => Enqueue(SinkORStep.Instance);

        public new SQLBuilder sinkNotOR() => Enqueue(SinkNotORStep.Instance);

        public new SQLBuilder rise() => Enqueue(RiseStep.Instance);

        public new SQLBuilder not() => Enqueue(NotStep.Instance);

        public new SQLBuilder whereLike(string key, object val) => Enqueue(new WhereLikestringobjectStep(key, val));

        public new SQLBuilder whereLikes(IEnumerable<string> keys, string val) => Enqueue(new WhereLikesEnumstringstringStep(keys, val));

        public new SQLBuilder whereLikes(string key, IEnumerable<string> vals, bool isOr = true) => Enqueue(new WhereLikesstringEnumstringboolStep(key, vals, isOr));

        public new SQLBuilder whereLikesOr(string key, params string[] vals) => Enqueue(new WhereLikesOrstringstringArrStep(key, vals));

        public new SQLBuilder whereLikesAnd(string key, params string[] vals) => Enqueue(new WhereLikesAndstringstringArrStep(key, vals));

        public new SQLBuilder whereLikeLeft(string key, object val) => Enqueue(new WhereLikeLeftstringobjectStep(key, val));

        public new SQLBuilder whereNotLikeLeft(string key, string val) => Enqueue(new WhereNotLikeLeftstringstringStep(key, val));

        public new SQLBuilder whereLikeLefts(string key, IEnumerable<string> vals, bool isOr = true) => Enqueue(new WhereLikeLeftsstringEnumstringboolStep(key, vals, isOr));

        public new SQLBuilder whereNotLikeLefts(string key, IEnumerable<string> vals) => Enqueue(new WhereNotLikeLeftsstringEnumstringStep(key, vals));

        public new SQLBuilder whereLikeLefts(string key, params string[] likeCodes) => Enqueue(new WhereLikeLeftsstringstringArrStep(key, likeCodes));

        public new SQLBuilder whereNotLike(string key, object val) => Enqueue(new WhereNotLikestringobjectStep(key, val));

        public new SQLBuilder whereNotLikeOrNull(string key, string val) => Enqueue(new WhereNotLikeOrNullstringstringStep(key, val));

        public new SQLBuilder whereNotLikeLeftOrNull(string key, string val) => Enqueue(new WhereNotLikeLeftOrNullstringstringStep(key, val));

        public new SQLBuilder whereIn(string key, IEnumerable values) => Enqueue(new WhereInstringEnumStep(key, values));

        public new SQLBuilder whereIn(string key, List<object> val) => Enqueue(new WhereInstringListobjectStep(key, val));

        public new SQLBuilder whereIn(string key, Action<SQLBuilder> doselect) => Enqueue(new WhereInstringActStep(key, doselect));

        public new SQLBuilder whereInGuid(string key, IEnumerable<Guid> OIDs) => Enqueue(new WhereInGuidstringEnumGuidStep(key, OIDs));

        public new SQLBuilder whereInGuid(string key, IEnumerable<Guid?> OIDs) => Enqueue(new WhereInGuidstringEnumGuidNStep(key, OIDs));

        public new SQLBuilder whereInGuid(string key, IEnumerable<string> OIDs) => Enqueue(new WhereInGuidstringEnumstringStep(key, OIDs));

        public new SQLBuilder whereNotIn(string key, IEnumerable values) => Enqueue(new WhereNotInstringEnumStep(key, values));

        public new SQLBuilder whereFields(IEnumerable<string> fields, object value, int SinkMode = 0, string op = "=") => Enqueue(new WhereFieldsEnumstringobjectintstringStep(fields, value, SinkMode, op));

        public new SQLBuilder whereAnyFieid(IEnumerable<string> fields, object value, string op = "=") => Enqueue(new WhereAnyFieidEnumstringobjectstringStep(fields, value, op));

        public new SQLBuilder whereAnyFieldIs(object value, params string[] fields) => Enqueue(new WhereAnyFieldIsobjectstringArrStep(value, fields));

        public new SQLBuilder whereAllFieid(IEnumerable<string> fields, object value, string op = "=") => Enqueue(new WhereAllFieidEnumstringobjectstringStep(fields, value, op));

        public new SQLBuilder where(WhereListBag bag) => Enqueue(new WhereWhereListBagStep(bag));

        public new SQLBuilder whereNotIn(string key, Action<SQLBuilder> doselect) => Enqueue(new WhereNotInstringActStep(key, doselect));

        public new SQLBuilder whereExist(string value) => Enqueue(new WhereExiststringStep(value));

        public new SQLBuilder whereExist(Action<SQLBuilder> doselect) => Enqueue(new WhereExistActStep(doselect));

        public new SQLBuilder whereNotExist(string selectSQL) => Enqueue(new WhereNotExiststringStep(selectSQL));

        public new SQLBuilder whereNotExist(Action<SQLBuilder> doselect) => Enqueue(new WhereNotExistActStep(doselect));

        public new SQLBuilder where(string key, string op, Action<SQLBuilder> doselect) => Enqueue(new WherestringstringActStep(key, op, doselect));

        public new SQLBuilder where(string key, Action<SQLBuilder> doselect) => Enqueue(new WherestringActStep(key, doselect));

        public new SQLBuilder whereGreaterThan(string key, object val) => Enqueue(new WhereGreaterThanstringobjectStep(key, val));

        public new SQLBuilder whereLessThan(string key, object val) => Enqueue(new WhereLessThanstringobjectStep(key, val));

        public new SQLBuilder whereGreaterThanOrEqual(string key, object val) => Enqueue(new WhereGreaterThanOrEqualstringobjectStep(key, val));

        public new SQLBuilder whereLessThanOrEqual(string key, object val) => Enqueue(new WhereLessThanOrEqualstringobjectStep(key, val));

        public new SQLBuilder whereNotEqual(string key, object val) => Enqueue(new WhereNotEqualstringobjectStep(key, val));

        public new SQLBuilder whereIf(bool? isTrue, string key, object val, string op = "=") => Enqueue(new WhereIfboolNstringobjectstringStep(isTrue, key, val, op));

        public new SQLBuilder whereIf(bool? isTrue, string key) => Enqueue(new WhereIfboolNstringStep(isTrue, key));

        public new SQLBuilder whereGuid(string key, object val) => Enqueue(new WhereGuidstringobjectStep(key, val));

        public new SQLBuilder whereIsOrNull(string key, object val) => Enqueue(new WhereIsOrNullstringobjectStep(key, val));

        public new SQLBuilder whereIsNullOR(string key, object val, string op) => Enqueue(new WhereIsNullORstringobjectstringStep(key, val, op));

        public new SQLBuilder whereVsOrNull(string key, object val, string op) => Enqueue(new WhereVsOrNullstringobjectstringStep(key, val, op));

        public new SQLBuilder where(string key, object val, Type t) => Enqueue(new WherestringobjectTypeStep(key, val, t));

        public new SQLBuilder where(string key, object val, string op, Type t) => Enqueue(new WherestringobjectstringTypeStep(key, val, op, t));

        public new SQLBuilder where(string key, object val, string op, bool paramed, Type t) => Enqueue(new WherestringobjectstringboolTypeStep(key, val, op, paramed, t));

        public new SQLBuilder whereFormat(string template, params object[] values) => Enqueue(new WhereFormatstringobjectArrStep(template, values));

        public new SQLBuilder where(Action<SQLBuilder> whereBuilder) => Enqueue(new WhereActStep(whereBuilder));


    }
}
