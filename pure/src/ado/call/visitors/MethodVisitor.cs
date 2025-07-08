
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{
    /// <summary>
    /// 抽象的方法访问者，主要负责分发，具体逻辑由子类实现。
    /// </summary>
    public abstract class MethodVisitor
    {
        protected MethodVisitor()
        {

        }



        protected internal virtual MethodCall VisitExtension(MethodCall node)
        {
            return node.VisitChildren(this);
        }

        #region 抽象访问者，禁止表达式调用
        public virtual MethodCall Visit(MethodCall node)
        {
            if (node == null) { 
                return null;
            }
            return node.Accept(this);
        }
        #endregion

        #region 具体访问者，被表达式调用

        public virtual MethodCall VisitAlias(AliasCall method) {
            return method;
        }

        public virtual MethodCall VisitAll(AllCall method)
        {
            return method;
        }

        public virtual MethodCall VisitAny(AnyCall method)
        {
            return method;
        }

        public virtual MethodCall VisitAsCte(AsCteCall method)
        {
            return method;
        }
        public virtual MethodCall VisitAsQueryable(AsQueryableCall method)
        {
            return method;
        }
        public virtual MethodCall VisitAsSubQuery(AsSubQueryCall method)
        {
            return method;
        }

        public virtual MethodCall VisitAsUpdatable(AsUpdatableCall method)
        {
            return method;
        }

        public virtual MethodCall VisitAsValueInsertable(AsValueInsertableCall method)
        {
            return method;
        }
        public virtual MethodCall VisitAverage(AverageCall method)
        {
            return method;
        }
        public virtual MethodCall VisitCast(CastCall method)
        {
            return method;
        }
        public virtual MethodCall VisitConcat(ConcatCall method)
        {
            return method;
        }
        public virtual MethodCall VisitContains(ContainsCall method)
        {
            return method;
        }
        public virtual MethodCall VisitCount(CountCall method)
        {
            return method;
        }
        public virtual MethodCall VisitCrossJoin(CrossJoinCall method)
        {
            return method;
        }
        public virtual MethodCall VisitDatabaseName(DatabaseNameCall method)
        {
            return method;
        }
        public virtual MethodCall VisitDefaultIfEmpty(DefaultIfEmptyCall method)
        {
            return method;
        }
        public virtual MethodCall VisitDelete(DeleteCall method)
        {
            return method;
        }
        public virtual MethodCall VisitDoDelete(DoDeleteCall method)
        {
            return method;
        }

        public virtual MethodCall VisitDeleteWhenMatchedAnd(DeleteWhenMatchedAndCall method)
        {
            return method;
        }
        public virtual MethodCall VisitDeleteWhenNotMatchedBySourceAnd(DeleteWhenNotMatchedBySourceAndCall method)
        {
            return method;
        }
        public virtual MethodCall VisitDeleteWithOutput(DeleteWithOutputCall method)
        {
            return method;
        }
        public virtual MethodCall VisitDeleteWithOutputInto(DeleteWithOutputIntoCall method)
        {
            return method;
        }
        public virtual MethodCall VisitDisableGuard(DisableGuardCall method)
        {
            return method;
        }
        public virtual MethodCall VisitDistinct(DistinctCall method)
        {
            return method;
        }
        public virtual MethodCall VisitDrop(DropCall method)
        {
            return method;
        }
        public virtual MethodCall VisitElementAt(ElementAtCall method)
        {
            return method;
        }
        public virtual MethodCall VisitElementAtOrDefault(ElementAtOrDefaultCall method)
        {
            return method;
        }
        public virtual MethodCall VisitElse(ElseCall method)
        {
            return method;
        }
        public virtual MethodCall VisitExcept(ExceptCall method)
        {
            return method;
        }
        public virtual MethodCall VisitExceptAll(ExceptAllCall method)
        {
            return method;
        }

        public virtual MethodCall VisitExpression(ExpressionCall mehod) { 
            return mehod;
        }

        public virtual MethodCall VisitFirst(FirstCall method)
        {
            return method;
        }
        public virtual MethodCall VisitFirstOrDefault(FirstOrDefaultCall method)
        {
            return method;
        }
        public virtual MethodCall VisitFromSql(FromSqlCall method)
        {
            return method;
        }
        public virtual MethodCall VisitFromSqlScalar(FromSqlScalarCall method)
        {
            return method;
        }
        public virtual MethodCall VisitFullJoin(FullJoinCall method)
        {
            return method;
        }
        public virtual MethodCall VisitGetCte(GetCteCall method)
        {
            return method;
        }

        public virtual MethodCall VisitGetTable(GetTableCall method)
        {
            return method;
        }
        public virtual MethodCall VisitGroupBy(GroupByCall method)
        {
            return method;
        }
        public virtual MethodCall VisitGroupJoin(GroupJoinCall method)
        {
            return method;
        }
        public virtual MethodCall VisitHasUniqueKey(HasUniqueKeyCall method)
        {
            return method;
        }
        public virtual MethodCall VisitHaving(HavingCall method)
        {
            return method;
        }
        public virtual MethodCall VisitIgnoreFilters(IgnoreFiltersCall method)
        {
            return method;
        }
        /// <summary>
        /// 导航包含
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public virtual MethodCall VisitIncludes(IncludesCall method)
        {
            return method;
        }
        
        /// <summary>
        /// SQL注入表达式
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public virtual MethodCall VisitInjectSQL(InjectSQLCall method)
        {
            return method;
        }
        
        public virtual MethodCall VisitInlineParameters(InlineParametersCall method)
        {
            return method;
        }
        public virtual MethodCall VisitInList(InListCall method)
        {
            return method;
        }
        public virtual MethodCall VisitInnerJoin(InnerJoinCall method)
        {
            return method;
        }
        public virtual MethodCall VisitInsert(InsertCall method)
        {
            return method;
        }
        public virtual MethodCall VisitInsertAll(InsertAllCall method)
        {
            return method;

        }
        public virtual MethodCall VisitInsertFirst(InsertFirstCall method)
        {
            return method;
        }

        public virtual MethodCall VisitInsertOrUpdate(InsertOrUpdateCall method)
        {
            return method;
        }


        public virtual MethodCall VisitInsertWhenNotMatchedAnd(InsertWhenNotMatchedAndCall method)
        {
            return method;
        }

        public virtual MethodCall VisitInsertWithIdentity(InsertWithIdentityCall method)
        {
            return method;
        }
        public virtual MethodCall VisitInsertWithOutput(InsertWithOutputCall method)
        {
            return method;
        }
        public virtual MethodCall VisitInsertWithOutputInto(InsertWithOutputIntoCall method)
        {
            return method;
        }
        public virtual MethodCall VisitIntersect(IntersectCall method)
        {
            return method;
        }
        public virtual MethodCall VisitInto(IntoCall method)
        {
            return method;
        }
        public virtual MethodCall VisitIsNull(IsNullCall method)
        {
            return method;
        }

        public virtual MethodCall VisitIsNotNull(IsNotNullCall method)
        {
            return method;
        }

        public virtual MethodCall VisitIsTemporary(IsTemporaryCall method)
        {
            return method;
        }
        public virtual MethodCall VisitJoin(JoinCall method)
        {
            return method;
        }
        public virtual MethodCall VisitLeftJoin(LeftJoinCall method)
        {
            return method;
        }

        public virtual MethodCall VisitLoadWith(LoadWithCall method)
        {
            return method;
        }
        public virtual MethodCall VisitLike(LikeCall method)
        {
            return method;
        }

        public virtual MethodCall VisitLikeLeft(LikeLeftCall method)
        {
            return method;
        }

        public virtual MethodCall VisitLoadWithAsTable(LoadWithAsTableCall method)
        {
            return method;
        }
        public virtual MethodCall VisitLoadWithInternal(LoadWithInternalCall method)
        {
            return method;
        }
        public virtual MethodCall VisitLongCount(LongCountCall method)
        {
            return method;
        }
        public virtual MethodCall VisitMax(MaxCall method)
        {
            return method;
        }
        public virtual MethodCall VisitMerge(MergeCall method)
        {
            return method;
        }
        public virtual MethodCall VisitMergeInto(MergeIntoCall method)
        {
            return method;
        }
        public virtual MethodCall VisitMergeWithOutput(MergeWithOutputCall method)
        {
            return method;
        }
        public virtual MethodCall VisitMergeWithOutputInto(MergeWithOutputIntoCall method)
        {
            return method;
        }
        public virtual MethodCall VisitMin(MinCall method)
        {
            return method;
        }
        public virtual MethodCall VisitMultiInsert(MultiInsertCall method)
        {
            return method;
        }
        public virtual MethodCall VisitOfType(OfTypeCall method)
        {
            return method;
        }
        public virtual MethodCall VisitOn(OnCall method)
        {
            return method;
        }
        public virtual MethodCall VisitOnTargetKey(OnTargetKeyCall method)
        {
            return method;
        }
        public virtual MethodCall VisitOrderBy(OrderByCall method)
        {
            return method;
        }
        public virtual MethodCall VisitOrderByDescending(OrderByDescendingCall method)
        {
            return method;
        }
        public virtual MethodCall VisitQueryName(QueryNameCall method)
        {
            return method;
        }
        public virtual MethodCall VisitRemoveOrderBy(RemoveOrderByCall method)
        {
            return method;
        }
        public virtual MethodCall VisitRightJoin(RightJoinCall method)
        {
            return method;
        }
        public virtual MethodCall VisitSchemaName(SchemaNameCall method)
        {
            return method;
        }
        public virtual MethodCall VisitSelect(SelectCall method)
        {
            return method;
        }
        public virtual MethodCall VisitSelectDistinct(SelectDistinctCall method)
        {
            return method;
        }
        public virtual MethodCall VisitSelectMany(SelectManyCall method)
        {
            return method;
        }
        public virtual MethodCall VisitSelectQuery(SelectQueryCall method)
        {
            return method;
        }
        public virtual MethodCall VisitServerName(ServerNameCall method)
        {
            return method;
        }
        public virtual MethodCall VisitSet(SetCall method)
        {
            return method;
        }
        public virtual MethodCall VisitSetPage(SetPageCall method)
        {
            return method;
        }
        
        public virtual MethodCall VisitSingle(SingleCall method)
        {
            return method;
        }
        public virtual MethodCall VisitSingleOrDefault(SingleOrDefaultCall method)
        {
            return method;
        }

        public virtual MethodCall VisitSink(SinkCall method)
        {
            return method;
        }
        public virtual MethodCall VisitSinkOR(SinkORCall method)
        {
            return method;
        }
        public virtual MethodCall VisitRise(RiseCall method)
        {
            return method;
        }


        public virtual MethodCall VisitSkip(SkipCall method)
        {
            return method;
        }
        public virtual MethodCall VisitStartsWith(StartsWithCall method)
        {
            return method;
        }
        

        public virtual MethodCall VisitSum(SumCall method)
        {
            return method;
        }
        public virtual MethodCall VisitTableFromExpression(TableFromExpressionCall method)
        {
            return method;
        }
        public virtual MethodCall VisitTableID(TableIDCall method)
        {
            return method;
        }
        public virtual MethodCall VisitTableName(TableNameCall method)
        {
            return method;
        }
        public virtual MethodCall VisitTableOptions(TableOptionsCall method)
        {
            return method;
        }
        public virtual MethodCall VisitTagQuery(TagQueryCall method)
        {
            return method;
        }
        public virtual MethodCall VisitTake(TakeCall method)
        {
            return method;
        }
        public virtual MethodCall VisitThenBy(ThenByCall method)
        {
            return method;
        }
        public virtual MethodCall VisitThenByDescending(ThenByDescendingCall method)
        {
            return method;
        }
        public virtual MethodCall VisitThenLoad(ThenLoadCall method)
        {
            return method;

        }
        public virtual MethodCall VisitThenOrBy(ThenOrByCall method)
        {
            return method;
        }

        public virtual MethodCall VisitThenOrByDescending(ThenOrByDescendingCall method)
        {
            return method;
        }
        public virtual MethodCall VisitTop(TopCall method)
        {
            return method;
        }
        public virtual MethodCall VisitToPageList(ToPageListCall method)
        {
            return method;
        }
        

        public virtual MethodCall VisitTruncate(TruncateCall method)
        {
            return method;
        }

        public virtual MethodCall VisitUnion(UnionCall method)
        {
            return method;
        }
        public virtual MethodCall VisitUnionAll(UnionAllCall method)
        {
            return method;
        }
        public virtual MethodCall VisitUpdate(UpdateCall method)
        {
            return method;
        }
        public virtual MethodCall VisitDoUpdate(DoUpdateCall method)
        {
            return method;
        }
        public virtual MethodCall VisitUpdateWhenMatchedAnd(UpdateWhenMatchedAndCall method)
        {
            return method;
        }
        public virtual MethodCall VisitUpdateWhenMatchedAndThenDelete(UpdateWhenMatchedAndThenDeleteCall method)
        {
            return method;
        }
        public virtual MethodCall VisitUpdateWhenNotMatchedBySourceAnd(UpdateWhenNotMatchedBySourceAndCall method)
        {
            return method;
        }

        public virtual MethodCall VisitUpdateWithOutput(UpdateWithOutputCall method)
        {
            return method;
        }
        public virtual MethodCall VisitUpdateWithOutputInto(UpdateWithOutputIntoCall method)
        {
            return method;
        }
        public virtual MethodCall VisitUsing(UsingCall method)
        {
            return method;
        }
        public virtual MethodCall VisitUsingTarget(UsingTargetCall method)
        {
            return method;
        }
        public virtual MethodCall VisitValue(ValueCall method)
        {
            return method;
        }
        public virtual MethodCall VisitWhen(WhenCall method)
        {
            return method;
        }
        public virtual MethodCall VisitWhere(WhereCall method)
        {
            return method;
        }
        public virtual MethodCall VisitWith(WithCall method)
        {
            return method;
        }
        public virtual MethodCall VisitWithTableExpression(WithTableExpressionCall method)
        {
            return method;
        }

        #endregion


    }
}
