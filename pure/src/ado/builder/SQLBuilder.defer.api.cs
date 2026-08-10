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
        public SQLBuilder ifs(bool isPass)
        {
            Opened = isPass;
            return Enqueue(new IfsboolStep(isPass));
        }


        // ---- select ----
        public SQLBuilder prefix(string SQLString) => Enqueue(new PrefixstringStep(SQLString));

        public SQLBuilder subfix(string SQLString) => Enqueue(new SubfixstringStep(SQLString));

        public SQLBuilder copyPreSelect() => Enqueue(CopyPreSelectStep.Instance);

        public SQLBuilder copyPreFrom() => Enqueue(CopyPreFromStep.Instance);

        public SQLBuilder copyPreWere() => Enqueue(CopyPreWereStep.Instance);

        public SQLBuilder selectWith(string queryOther) => Enqueue(new SelectWithstringStep(queryOther));

        public SQLBuilder selectSummary(string queryOther) => Enqueue(new SelectSummarystringStep(queryOther));

        public SQLBuilder selectFormat(string selectSQLPart, params object[] paras) => Enqueue(new SelectFormatstringobjectArrStep(selectSQLPart, paras));

        public SQLBuilder selectUnioned(string columns) => Enqueue(new SelectUnionedstringStep(columns));

        public SQLBuilder top(int num) => Enqueue(new TopintStep(num));

        public SQLBuilder skipTake(int skip, int take) => Enqueue(new SkipTakeintintStep(skip, take));

        public SQLBuilder skip(int skip) => Enqueue(new SkipintStep(skip));

        public SQLBuilder take(int skip) => Enqueue(new TakeintStep(skip));

        public SQLBuilder groupBy(string groupField) => Enqueue(new GroupBystringStep(groupField));

        public SQLBuilder having(string havingStr) => Enqueue(new HavingstringStep(havingStr));

        public SQLBuilder orderby(string orderByPart) => Enqueue(new OrderbystringStep(orderByPart));

        public SQLBuilder rowNumber() => Enqueue(RowNumberStep.Instance);

        public SQLBuilder rowNumberUse(string numFieldName) => Enqueue(new RowNumberUsestringStep(numFieldName));

        public SQLBuilder rowNumber(string orderPart) => Enqueue(new RowNumberstringStep(orderPart));

        public SQLBuilder rowNumber(string orderPart, string asName) => Enqueue(new RowNumberstringstringStep(orderPart, asName));


        // ---- set ----
        public SQLBuilder setTable(string tbName) => Enqueue(new SetTablestringStep(tbName));

        public SQLBuilder configSetNull(UpdateSetNullOption option) => Enqueue(new ConfigSetNullUpdateSetNullOptionStep(option));

        public SQLBuilder set(string key, string value, int maxLength)
        {
            var step = new SetstringstringintStep(key, value, maxLength);
            step.TryAssignStaticSlot(_paraRule, ref _opened, ref _nextStaticSlot, CurrentParaSeed, CurrentSetGroupKey);
            return Enqueue(step);
        }

        public SQLBuilder set(string key, object val, bool paramed = true, Type type = null, bool updatable = true, bool insertable = true)
        {
            var step = new SetstringobjectboolTypeboolboolStep(key, val, paramed, type, updatable, insertable);
            step.TryAssignStaticSlot(_paraRule, ref _opened, ref _nextStaticSlot, CurrentParaSeed, CurrentSetGroupKey);
            return Enqueue(step);
        }

        public SQLBuilder setToNull(string fieldName) => Enqueue(new SetToNullstringStep(fieldName));

        public SQLBuilder setI(string key, object val)
        {
            var step = new SetIstringobjectStep(key, val);
            step.TryAssignStaticSlot(_paraRule, ref _opened, ref _nextStaticSlot, CurrentParaSeed, CurrentSetGroupKey);
            return Enqueue(step);
        }

        public SQLBuilder setI(string key, object val, bool paramed)
        {
            var step = new SetIstringobjectboolStep(key, val, paramed);
            step.TryAssignStaticSlot(_paraRule, ref _opened, ref _nextStaticSlot, CurrentParaSeed, CurrentSetGroupKey);
            return Enqueue(step);
        }

        public SQLBuilder setU(string key, object val)
        {
            var step = new SetUstringobjectStep(key, val);
            step.TryAssignStaticSlot(_paraRule, ref _opened, ref _nextStaticSlot, CurrentParaSeed, CurrentSetGroupKey);
            return Enqueue(step);
        }

        public SQLBuilder setU(string key, object val, bool paramed)
        {
            var step = new SetUstringobjectboolStep(key, val, paramed);
            step.TryAssignStaticSlot(_paraRule, ref _opened, ref _nextStaticSlot, CurrentParaSeed, CurrentSetGroupKey);
            return Enqueue(step);
        }

        public SQLBuilder newRow() => Enqueue(NewRowStep.Instance);

        public SQLBuilder addRow() => Enqueue(AddRowStep.Instance);


        // ---- merge ----
        public SQLBuilder mergeAs(string asName) => Enqueue(new MergeAsstringStep(asName));

        public SQLBuilder mergeUsing(string asName, string tabname) => Enqueue(new MergeUsingstringstringStep(asName, tabname));

        public SQLBuilder mergeOn(string onPart) => Enqueue(new MergeOnstringStep(onPart));

        public SQLBuilder mergeDelete(bool thenDelete) => Enqueue(new MergeDeleteboolStep(thenDelete));


        // ---- union ----

        public SQLBuilder withRecur(string name, Action<RecurCTEBuilder> buildRecur) => Enqueue(new WithRecurstringAction_RecurCTEBuilderStep(name, buildRecur));

        public SQLBuilder withSelect(string name, string selectSQL) => Enqueue(new WithSelectstringstringStep(name, selectSQL));

        public SQLBuilder unionAll(bool wrapSelect = true, string wrapAsName = "tmpunioned") => Enqueue(new UnionAllboolstringStep(wrapSelect, wrapAsName));

        public SQLBuilder union(bool isUnionAll = false, bool wrapSelect = true, string wrapAsName = "tmpunioned") => Enqueue(new UnionboolboolstringStep(isUnionAll, wrapSelect, wrapAsName));

        public SQLBuilder unionAs(Action<SqlGoup> dogroup) => Enqueue(new UnionAsAction_SqlGoupStep(dogroup));

        public SQLBuilder toggleToUnionOutor() => Enqueue(ToggleToUnionOutorStep.Instance);

        public SQLBuilder union(Action<SQLBuilder> doUnion) => Enqueue(new UnionActStep(doUnion));


        // ---- from ----
        public SQLBuilder fromFormat(string fromSQLPart, params object[] paras) => Enqueue(new FromFormatstringobjectArrStep(fromSQLPart, paras));

        public SQLBuilder join(string joinSQLString) => Enqueue(new JoinstringStep(joinSQLString));

        public SQLBuilder join(string targetTable, string onLeft, string onRight) => Enqueue(new JoinstringstringstringStep(targetTable, onLeft, onRight));

        public SQLBuilder joinFormat(string JoinSQLPart, params object[] paras) => Enqueue(new JoinFormatstringobjectArrStep(JoinSQLPart, paras));

        public SQLBuilder leftJoin(string joinSQLString) => Enqueue(new LeftJoinstringStep(joinSQLString));

        public SQLBuilder innerJoin(string joinSQLString) => Enqueue(new InnerJoinstringStep(joinSQLString));

        public SQLBuilder pivot(PivotItem SQLString) => Enqueue(new PivotPivotItemStep(SQLString));

        public SQLBuilder unpivot(UnpivotItem SQLString) => Enqueue(new UnpivotUnpivotItemStep(SQLString));

        public SQLBuilder pivot(string aggregation, string field, List<string> values, string asName) => Enqueue(new PivotstringstringListstringstringStep(aggregation, field, values, asName));

        public SQLBuilder unpivot(string valueName, string fieldName, List<string> fields, string asName) => Enqueue(new UnpivotstringstringListstringstringStep(valueName, fieldName, fields, asName));


        // ---- misc ----
        public SQLBuilder orLeft() => Enqueue(OrLeftStep.Instance);

        public SQLBuilder orRight() => Enqueue(OrRightStep.Instance);

        public SQLBuilder andLeft() => Enqueue(AndLeftStep.Instance);

        public SQLBuilder andRight() => Enqueue(AndRightStep.Instance);

        public SQLBuilder pinLeft() => Enqueue(PinLeftStep.Instance);

        public SQLBuilder pinRight() => Enqueue(PinRightStep.Instance);


        // ---- where ----
        public SQLBuilder whereIsNull(string key) => Enqueue(new WhereIsNullstringStep(key));

        public SQLBuilder whereIsNotNull(string key) => Enqueue(new WhereIsNotNullstringStep(key));

        public SQLBuilder where(WhereFrag frag) => Enqueue(new WhereWhereFragStep(frag));

        public SQLBuilder pin(string SQL) => Enqueue(new PinstringStep(SQL));

        public SQLBuilder and() => Enqueue(AndStep.Instance);

        public SQLBuilder or() => Enqueue(OrStep.Instance);

        public SQLBuilder sink(string connector = "AND") => Enqueue(new SinkstringStep(connector));

        public SQLBuilder sinkNot(string connector = "AND") => Enqueue(new SinkNotstringStep(connector));

        public SQLBuilder sinkOR() => Enqueue(SinkORStep.Instance);

        public SQLBuilder sinkNotOR() => Enqueue(SinkNotORStep.Instance);

        public SQLBuilder rise() => Enqueue(RiseStep.Instance);

        public SQLBuilder not() => Enqueue(NotStep.Instance);

        public SQLBuilder whereLike(string key, object val) => Enqueue(new WhereLikestringobjectStep(key, val));

        public SQLBuilder whereLikes(IEnumerable<string> keys, string val) => Enqueue(new WhereLikesEnumstringstringStep(keys, val));

        public SQLBuilder whereLikes(string key, IEnumerable<string> vals, bool isOr = true) => Enqueue(new WhereLikesstringEnumstringboolStep(key, vals, isOr));

        public SQLBuilder whereLikesOr(string key, params string[] vals) => Enqueue(new WhereLikesOrstringstringArrStep(key, vals));

        public SQLBuilder whereLikesAnd(string key, params string[] vals) => Enqueue(new WhereLikesAndstringstringArrStep(key, vals));

        public SQLBuilder whereLikeLeft(string key, object val) => Enqueue(new WhereLikeLeftstringobjectStep(key, val));

        public SQLBuilder whereNotLikeLeft(string key, string val) => Enqueue(new WhereNotLikeLeftstringstringStep(key, val));

        public SQLBuilder whereLikeLefts(string key, IEnumerable<string> vals, bool isOr = true) => Enqueue(new WhereLikeLeftsstringEnumstringboolStep(key, vals, isOr));

        public SQLBuilder whereNotLikeLefts(string key, IEnumerable<string> vals) => Enqueue(new WhereNotLikeLeftsstringEnumstringStep(key, vals));

        public SQLBuilder whereLikeLefts(string key, params string[] likeCodes) => Enqueue(new WhereLikeLeftsstringstringArrStep(key, likeCodes));

        public SQLBuilder whereNotLike(string key, object val) => Enqueue(new WhereNotLikestringobjectStep(key, val));

        public SQLBuilder whereNotLikeOrNull(string key, string val) => Enqueue(new WhereNotLikeOrNullstringstringStep(key, val));

        public SQLBuilder whereNotLikeLeftOrNull(string key, string val) => Enqueue(new WhereNotLikeLeftOrNullstringstringStep(key, val));

        public SQLBuilder whereIn(string key, IEnumerable values) => Enqueue(new WhereInstringEnumStep(key, values));

        public SQLBuilder whereIn(string key, List<object> val) => Enqueue(new WhereInstringListobjectStep(key, val));

        public SQLBuilder whereInGuid(string key, IEnumerable<Guid> OIDs) => Enqueue(new WhereInGuidstringEnumGuidStep(key, OIDs));

        public SQLBuilder whereInGuid(string key, IEnumerable<Guid?> OIDs) => Enqueue(new WhereInGuidstringEnumGuidNStep(key, OIDs));

        public SQLBuilder whereInGuid(string key, IEnumerable<string> OIDs) => Enqueue(new WhereInGuidstringEnumstringStep(key, OIDs));

        public SQLBuilder whereNotIn(string key, IEnumerable values) => Enqueue(new WhereNotInstringEnumStep(key, values));

        public SQLBuilder whereFields(IEnumerable<string> fields, object value, int SinkMode = 0, string op = "=") => Enqueue(new WhereFieldsEnumstringobjectintstringStep(fields, value, SinkMode, op));

        public SQLBuilder whereAnyFieid(IEnumerable<string> fields, object value, string op = "=") => Enqueue(new WhereAnyFieidEnumstringobjectstringStep(fields, value, op));

        public SQLBuilder whereAnyFieldIs(object value, params string[] fields) => Enqueue(new WhereAnyFieldIsobjectstringArrStep(value, fields));

        public SQLBuilder whereAllFieid(IEnumerable<string> fields, object value, string op = "=") => Enqueue(new WhereAllFieidEnumstringobjectstringStep(fields, value, op));

        public SQLBuilder where(WhereListBag bag) => Enqueue(new WhereWhereListBagStep(bag));

        public SQLBuilder whereExist(string value) => Enqueue(new WhereExiststringStep(value));

        public SQLBuilder whereNotExist(string selectSQL) => Enqueue(new WhereNotExiststringStep(selectSQL));

        public SQLBuilder whereGreaterThan(string key, object val)
        {
            var step = new WhereGreaterThanstringobjectStep(key, val);
            step.TryAssignStaticSlot(_paraRule, ref _opened, ref _nextStaticSlot, CurrentParaSeed, CurrentWhereGroupSeed);
            return Enqueue(step);
        }

        public SQLBuilder whereLessThan(string key, object val)
        {
            var step = new WhereLessThanstringobjectStep(key, val);
            step.TryAssignStaticSlot(_paraRule, ref _opened, ref _nextStaticSlot, CurrentParaSeed, CurrentWhereGroupSeed);
            return Enqueue(step);
        }

        public SQLBuilder whereGreaterThanOrEqual(string key, object val)
        {
            var step = new WhereGreaterThanOrEqualstringobjectStep(key, val);
            step.TryAssignStaticSlot(_paraRule, ref _opened, ref _nextStaticSlot, CurrentParaSeed, CurrentWhereGroupSeed);
            return Enqueue(step);
        }

        public SQLBuilder whereLessThanOrEqual(string key, object val)
        {
            var step = new WhereLessThanOrEqualstringobjectStep(key, val);
            step.TryAssignStaticSlot(_paraRule, ref _opened, ref _nextStaticSlot, CurrentParaSeed, CurrentWhereGroupSeed);
            return Enqueue(step);
        }

        public SQLBuilder whereNotEqual(string key, object val)
        {
            var step = new WhereNotEqualstringobjectStep(key, val);
            step.TryAssignStaticSlot(_paraRule, ref _opened, ref _nextStaticSlot, CurrentParaSeed, CurrentWhereGroupSeed);
            return Enqueue(step);
        }

        public SQLBuilder whereIf(bool? isTrue, string key, object val, string op = "=") => Enqueue(new WhereIfboolNstringobjectstringStep(isTrue, key, val, op));

        public SQLBuilder whereIf(bool? isTrue, string key) => Enqueue(new WhereIfboolNstringStep(isTrue, key));

        public SQLBuilder whereGuid(string key, object val) => Enqueue(new WhereGuidstringobjectStep(key, val));

        public SQLBuilder whereIsOrNull(string key, object val) => Enqueue(new WhereIsOrNullstringobjectStep(key, val));

        public SQLBuilder whereIsNullOR(string key, object val, string op) => Enqueue(new WhereIsNullORstringobjectstringStep(key, val, op));

        public SQLBuilder whereVsOrNull(string key, object val, string op) => Enqueue(new WhereVsOrNullstringobjectstringStep(key, val, op));

        public SQLBuilder where(string key, object val, Type t) => Enqueue(new WherestringobjectTypeStep(key, val, t));

        public SQLBuilder where(string key, object val, string op, Type t) => Enqueue(new WherestringobjectstringTypeStep(key, val, op, t));

        public SQLBuilder where(string key, object val, string op, bool paramed, Type t) => Enqueue(new WherestringobjectstringboolTypeStep(key, val, op, paramed, t));

        public SQLBuilder whereFormat(string template, params object[] values) => Enqueue(new WhereFormatstringobjectArrStep(template, values));


    }
}
