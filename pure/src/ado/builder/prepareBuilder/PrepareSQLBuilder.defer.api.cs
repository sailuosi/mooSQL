using System;
using System.Collections;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// 自动生成：简单构造 API 的门面入队（由 tools/gen_sqlbuilder_steps.py 生成）。
    /// 语法糖 override 已迁入 <see cref="SQLBuilder.sugar.cs"/>，经 multi_enqueue 调用下列内核 Step。
    /// </summary>
    public partial class PrepareSQLBuilder
    {
        // ---- control ----
        public override SQLBuilder ifs(bool isPass)
        {
            Opened = isPass;
            return Enqueue(new IfsboolStep(isPass));
        }


        // ---- select ----
        public override SQLBuilder prefix(string SQLString) => Enqueue(new PrefixstringStep(SQLString));

        public override SQLBuilder subfix(string SQLString) => Enqueue(new SubfixstringStep(SQLString));

        public override SQLBuilder copyPreSelect() => Enqueue(CopyPreSelectStep.Instance);

        public override SQLBuilder copyPreFrom() => Enqueue(CopyPreFromStep.Instance);

        public override SQLBuilder copyPreWere() => Enqueue(CopyPreWereStep.Instance);

        public override SQLBuilder selectWith(string queryOther) => Enqueue(new SelectWithstringStep(queryOther));

        public override SQLBuilder selectSummary(string queryOther) => Enqueue(new SelectSummarystringStep(queryOther));

        public override SQLBuilder selectFormat(string selectSQLPart, params object[] paras) => Enqueue(new SelectFormatstringobjectArrStep(selectSQLPart, paras));

        public override SQLBuilder selectUnioned(string columns) => Enqueue(new SelectUnionedstringStep(columns));

        public override SQLBuilder skipTake(int skip, int take) => Enqueue(new SkipTakeintintStep(skip, take));

        public override SQLBuilder skip(int skip) => Enqueue(new SkipintStep(skip));

        public override SQLBuilder take(int skip) => Enqueue(new TakeintStep(skip));

        public override SQLBuilder groupBy(string groupField) => Enqueue(new GroupBystringStep(groupField));

        public override SQLBuilder having(string havingStr) => Enqueue(new HavingstringStep(havingStr));

        public override SQLBuilder rowNumber() => Enqueue(RowNumberStep.Instance);

        public override SQLBuilder rowNumberUse(string numFieldName) => Enqueue(new RowNumberUsestringStep(numFieldName));

        public override SQLBuilder rowNumber(string orderPart) => Enqueue(new RowNumberstringStep(orderPart));

        public override SQLBuilder rowNumber(string orderPart, string asName) => Enqueue(new RowNumberstringstringStep(orderPart, asName));


        // ---- set ----
        public override SQLBuilder setTable(string tbName) => Enqueue(new SetTablestringStep(tbName));

        public override SQLBuilder configSetNull(UpdateSetNullOption option) => Enqueue(new ConfigSetNullUpdateSetNullOptionStep(option));

        public override SQLBuilder set(string key, object val, bool paramed = true, Type type = null, bool updatable = true, bool insertable = true)
        {
            var step = new SetstringobjectboolTypeboolboolStep(key, val, paramed, type, updatable, insertable);
            step.TryAssignStaticSlot(_paraRule, ref _opened, ref _nextStaticSlot, CurrentParaSeed, CurrentSetGroupKey);
            return Enqueue(step);
        }

        public override SQLBuilder newRow() => Enqueue(NewRowStep.Instance);

        public override SQLBuilder addRow() => Enqueue(AddRowStep.Instance);


        // ---- merge ----
        public override SQLBuilder mergeAs(string asName) => Enqueue(new MergeAsstringStep(asName));

        public override SQLBuilder mergeOn(string onPart) => Enqueue(new MergeOnstringStep(onPart));

        public override SQLBuilder mergeDelete(bool thenDelete) => Enqueue(new MergeDeleteboolStep(thenDelete));


        // ---- union ----

        // withRecur：见 PrepareSQLBuilder.defer.b.cs（编排期展开）

        public override SQLBuilder withSelect(string name, string selectSQL) => Enqueue(new WithSelectstringstringStep(name, selectSQL));

        public override SQLBuilder union(bool isUnionAll = false, bool wrapSelect = true, string wrapAsName = "tmpunioned") => Enqueue(new UnionboolboolstringStep(isUnionAll, wrapSelect, wrapAsName));

        public override SQLBuilder unionAs(Action<SqlGoup> dogroup) => Enqueue(new UnionAsAction_SqlGoupStep(dogroup));

        public override SQLBuilder toggleToUnionOutor() => Enqueue(ToggleToUnionOutorStep.Instance);

        public override SQLBuilder union(Action<SQLBuilder> doUnion) => Enqueue(new UnionActStep(doUnion));


        // ---- from ----
        public override SQLBuilder fromFormat(string fromSQLPart, params object[] paras) => Enqueue(new FromFormatstringobjectArrStep(fromSQLPart, paras));

        public override SQLBuilder join(string joinSQLString) => Enqueue(new JoinstringStep(joinSQLString));

        public override SQLBuilder join(string targetTable, string onLeft, string onRight) => Enqueue(new JoinstringstringstringStep(targetTable, onLeft, onRight));

        public override SQLBuilder joinFormat(string JoinSQLPart, params object[] paras) => Enqueue(new JoinFormatstringobjectArrStep(JoinSQLPart, paras));

        public override SQLBuilder pivot(PivotItem SQLString) => Enqueue(new PivotPivotItemStep(SQLString));

        public override SQLBuilder unpivot(UnpivotItem SQLString) => Enqueue(new UnpivotUnpivotItemStep(SQLString));

        public override SQLBuilder pivot(string aggregation, string field, List<string> values, string asName) => Enqueue(new PivotstringstringListstringstringStep(aggregation, field, values, asName));

        public override SQLBuilder unpivot(string valueName, string fieldName, List<string> fields, string asName) => Enqueue(new UnpivotstringstringListstringstringStep(valueName, fieldName, fields, asName));


        // ---- misc ----
        public override SQLBuilder pinLeft() => Enqueue(PinLeftStep.Instance);

        public override SQLBuilder pinRight() => Enqueue(PinRightStep.Instance);


        // ---- where ----
        public override SQLBuilder where(WhereFrag frag) => Enqueue(new WhereWhereFragStep(frag));

        public override SQLBuilder pin(string SQL) => Enqueue(new PinstringStep(SQL));

        public override SQLBuilder and() => Enqueue(AndStep.Instance);

        public override SQLBuilder or() => Enqueue(OrStep.Instance);

        public override SQLBuilder sink(string connector = "AND") => Enqueue(new SinkstringStep(connector));

        public override SQLBuilder sinkNot(string connector = "AND") => Enqueue(new SinkNotstringStep(connector));

        public override SQLBuilder sinkOR() => Enqueue(SinkORStep.Instance);

        public override SQLBuilder sinkNotOR() => Enqueue(SinkNotORStep.Instance);

        public override SQLBuilder rise() => Enqueue(RiseStep.Instance);

        public override SQLBuilder not() => Enqueue(NotStep.Instance);

        public override SQLBuilder whereLike(string key, object val) => Enqueue(new WhereLikestringobjectStep(key, val));

        public override SQLBuilder whereLikes(IEnumerable<string> keys, string val) => Enqueue(new WhereLikesEnumstringstringStep(keys, val));

        public override SQLBuilder whereLikes(string key, IEnumerable<string> vals, bool isOr = true) => Enqueue(new WhereLikesstringEnumstringboolStep(key, vals, isOr));

        public override SQLBuilder whereLikesOr(string key, params string[] vals) => Enqueue(new WhereLikesOrstringstringArrStep(key, vals));

        public override SQLBuilder whereLikesAnd(string key, params string[] vals) => Enqueue(new WhereLikesAndstringstringArrStep(key, vals));

        public override SQLBuilder whereLikeLeft(string key, object val) => Enqueue(new WhereLikeLeftstringobjectStep(key, val));

        public override SQLBuilder whereNotLikeLeft(string key, string val) => Enqueue(new WhereNotLikeLeftstringstringStep(key, val));

        public override SQLBuilder whereNotLikeLefts(string key, IEnumerable<string> vals) => Enqueue(new WhereNotLikeLeftsstringEnumstringStep(key, vals));

        public override SQLBuilder whereNotLike(string key, object val) => Enqueue(new WhereNotLikestringobjectStep(key, val));

        protected override SQLBuilder whereInCore(string key, IEnumerable values) => Enqueue(new WhereInstringEnumStep(key, values));

        public override SQLBuilder whereInGuid(string key, IEnumerable<Guid> OIDs) => Enqueue(new WhereInGuidstringEnumGuidStep(key, OIDs));

        public override SQLBuilder whereInGuid(string key, IEnumerable<Guid?> OIDs) => Enqueue(new WhereInGuidstringEnumGuidNStep(key, OIDs));

        public override SQLBuilder whereInGuid(string key, IEnumerable<string> OIDs) => Enqueue(new WhereInGuidstringEnumstringStep(key, OIDs));

        protected override SQLBuilder whereNotInCore(string key, IEnumerable values) => Enqueue(new WhereNotInstringEnumStep(key, values));

        public override SQLBuilder whereFields(IEnumerable<string> fields, object value, int SinkMode = 0, string op = "=") => Enqueue(new WhereFieldsEnumstringobjectintstringStep(fields, value, SinkMode, op));

        public override SQLBuilder where(WhereListBag bag) => Enqueue(new WhereWhereListBagStep(bag));

        public override SQLBuilder whereExist(string value) => Enqueue(new WhereExiststringStep(value));

        public override SQLBuilder whereIf(bool? isTrue, string key, object val, string op = "=") => Enqueue(new WhereIfboolNstringobjectstringStep(isTrue, key, val, op));

        public override SQLBuilder whereGuid(string key, object val) => Enqueue(new WhereGuidstringobjectStep(key, val));

        public override SQLBuilder where(string key, object val, string op, bool paramed, Type t) => Enqueue(new WherestringobjectstringboolTypeStep(key, val, op, paramed, t));

        public override SQLBuilder whereFormat(string template, params object[] values) => Enqueue(new WhereFormatstringobjectArrStep(template, values));


    }
}
