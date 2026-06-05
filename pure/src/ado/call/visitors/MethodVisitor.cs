
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
        /// <summary>
        /// 初始化 MethodVisitor。
        /// </summary>
        protected MethodVisitor()
        {

        }



        /// <summary>
        /// 访问 Extension 调用节点（默认返回原节点）。
        /// </summary>
        protected internal virtual MethodCall VisitExtension(MethodCall node)
        {
            return node.VisitChildren(this);
        }

        #region 抽象访问者，禁止表达式调用
        /// <summary>
        /// Visit 方法（返回 MethodCall）。
        /// </summary>
        public virtual MethodCall Visit(MethodCall node)
        {
            if (node == null) { 
                return null;
            }
            return node.Accept(this);
        }
        #endregion

        #region 具体访问者，被表达式调用

        /// <summary>
        /// 访问 Alias 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitAlias(AliasCall method) {
            return method;
        }

        /// <summary>
        /// 访问 All 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitAll(AllCall method)
        {
            return method;
        }

        /// <summary>
        /// 访问 Any 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitAny(AnyCall method)
        {
            return method;
        }

        /// <summary>
        /// 访问 CTE 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitAsCte(AsCteCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 AsQueryable 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitAsQueryable(AsQueryableCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 AsSubQuery 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitAsSubQuery(AsSubQueryCall method)
        {
            return method;
        }

        /// <summary>
        /// 访问 AsUpdatable 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitAsUpdatable(AsUpdatableCall method)
        {
            return method;
        }

        /// <summary>
        /// 访问 AsValueInsertable 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitAsValueInsertable(AsValueInsertableCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 AVG 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitAverage(AverageCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 Cast 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitCast(CastCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 Concat 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitConcat(ConcatCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 CONTAINS 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitContains(ContainsCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 COUNT 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitCount(CountCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 CROSS JOIN 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitCrossJoin(CrossJoinCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 DatabaseName 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitDatabaseName(DatabaseNameCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 DefaultIfEmpty 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitDefaultIfEmpty(DefaultIfEmptyCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 DELETE 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitDelete(DeleteCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 DoDelete 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitDoDelete(DoDeleteCall method)
        {
            return method;
        }

        /// <summary>
        /// 访问 DeleteWhenMatchedAnd 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitDeleteWhenMatchedAnd(DeleteWhenMatchedAndCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 DeleteWhenNotMatchedBySourceAnd 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitDeleteWhenNotMatchedBySourceAnd(DeleteWhenNotMatchedBySourceAndCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 DeleteWithOutput 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitDeleteWithOutput(DeleteWithOutputCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 DeleteWithOutputInto 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitDeleteWithOutputInto(DeleteWithOutputIntoCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 DisableGuard 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitDisableGuard(DisableGuardCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 DISTINCT 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitDistinct(DistinctCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 Drop 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitDrop(DropCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 ElementAt 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitElementAt(ElementAtCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 ElementAtOrDefault 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitElementAtOrDefault(ElementAtOrDefaultCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 Else 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitElse(ElseCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 Equals 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitEquals(EqualsCall method)
        {
            return method;
        }
        
        /// <summary>
        /// 访问 EXCEPT 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitExcept(ExceptCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 ExceptAll 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitExceptAll(ExceptAllCall method)
        {
            return method;
        }

        /// <summary>
        /// 访问 Expression 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitExpression(ExpressionCall mehod) { 
            return mehod;
        }

        /// <summary>
        /// 访问 Statement 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitStatement(StatementCall method)
            => method;

        /// <summary>
        /// 访问 FIRST 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitFirst(FirstCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 FirstOrDefault 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitFirstOrDefault(FirstOrDefaultCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 FROM SQL 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitFromSql(FromSqlCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 FromSqlScalar 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitFromSqlScalar(FromSqlScalarCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 FULL JOIN 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitFullJoin(FullJoinCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 获取 CTE 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitGetCte(GetCteCall method)
        {
            return method;
        }

        /// <summary>
        /// 访问 GetTable 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitGetTable(GetTableCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 GROUP BY 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitGroupBy(GroupByCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 GROUP JOIN 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitGroupJoin(GroupJoinCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 HasUniqueKey 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitHasUniqueKey(HasUniqueKeyCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 HAVING 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitHaving(HavingCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 IgnoreFilters 调用节点（默认返回原节点）。
        /// </summary>
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
        
        /// <summary>
        /// 访问 InlineParameters 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitInlineParameters(InlineParametersCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 InList 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitInList(InListCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 INNER JOIN 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitInnerJoin(InnerJoinCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 INSERT 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitInsert(InsertCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 InsertAll 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitInsertAll(InsertAllCall method)
        {
            return method;

        }
        /// <summary>
        /// 访问 InsertFirst 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitInsertFirst(InsertFirstCall method)
        {
            return method;
        }

        /// <summary>
        /// 访问 InsertOrUpdate 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitInsertOrUpdate(InsertOrUpdateCall method)
        {
            return method;
        }


        /// <summary>
        /// 访问 InsertWhenNotMatchedAnd 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitInsertWhenNotMatchedAnd(InsertWhenNotMatchedAndCall method)
        {
            return method;
        }

        /// <summary>
        /// 访问 InsertWithIdentity 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitInsertWithIdentity(InsertWithIdentityCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 InsertWithOutput 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitInsertWithOutput(InsertWithOutputCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 InsertWithOutputInto 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitInsertWithOutputInto(InsertWithOutputIntoCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 INTERSECT 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitIntersect(IntersectCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 INTO 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitInto(IntoCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 IsNull 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitIsNull(IsNullCall method)
        {
            return method;
        }

        /// <summary>
        /// 访问 IsNotNull 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitIsNotNull(IsNotNullCall method)
        {
            return method;
        }

        /// <summary>
        /// 访问 IsTemporary 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitIsTemporary(IsTemporaryCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 JOIN 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitJoin(JoinCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 LEFT JOIN 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitLeftJoin(LeftJoinCall method)
        {
            return method;
        }

        /// <summary>
        /// 访问 LoadWith 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitLoadWith(LoadWithCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 LIKE 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitLike(LikeCall method)
        {
            return method;
        }

        /// <summary>
        /// 访问 LikeLeft 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitLikeLeft(LikeLeftCall method)
        {
            return method;
        }

        /// <summary>
        /// 访问 LoadWithAsTable 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitLoadWithAsTable(LoadWithAsTableCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 LoadWithInternal 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitLoadWithInternal(LoadWithInternalCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 LONG COUNT 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitLongCount(LongCountCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 MAX 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitMax(MaxCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 MERGE 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitMerge(MergeCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 MergeInto 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitMergeInto(MergeIntoCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 MergeWithOutput 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitMergeWithOutput(MergeWithOutputCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 MergeWithOutputInto 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitMergeWithOutputInto(MergeWithOutputIntoCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 MIN 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitMin(MinCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 MultiInsert 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitMultiInsert(MultiInsertCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 OfType 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitOfType(OfTypeCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 On 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitOn(OnCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 OnTargetKey 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitOnTargetKey(OnTargetKeyCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 ORDER BY 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitOrderBy(OrderByCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 ORDER BY DESC 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitOrderByDescending(OrderByDescendingCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 QueryName 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitQueryName(QueryNameCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 RemoveOrderBy 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitRemoveOrderBy(RemoveOrderByCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 RIGHT JOIN 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitRightJoin(RightJoinCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 SchemaName 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitSchemaName(SchemaNameCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 SELECT 投影 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitSelect(SelectCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 SELECT DISTINCT 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitSelectDistinct(SelectDistinctCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 SELECT MANY 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitSelectMany(SelectManyCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 SelectQuery 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitSelectQuery(SelectQueryCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 ServerName 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitServerName(ServerNameCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 SET 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitSet(SetCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 SetPage 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitSetPage(SetPageCall method)
        {
            return method;
        }
        
        /// <summary>
        /// 访问 SINGLE 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitSingle(SingleCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 SingleOrDefault 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitSingleOrDefault(SingleOrDefaultCall method)
        {
            return method;
        }

        /// <summary>
        /// 访问 Sink 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitSink(SinkCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 SinkOR 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitSinkOR(SinkORCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 Rise 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitRise(RiseCall method)
        {
            return method;
        }


        /// <summary>
        /// 访问 SKIP 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitSkip(SkipCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 StartsWith 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitStartsWith(StartsWithCall method)
        {
            return method;
        }
        

        /// <summary>
        /// 访问 SUM 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitSum(SumCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 TableFromExpression 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitTableFromExpression(TableFromExpressionCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 TableID 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitTableID(TableIDCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 TableName 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitTableName(TableNameCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 TableOptions 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitTableOptions(TableOptionsCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 TagQuery 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitTagQuery(TagQueryCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 TAKE 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitTake(TakeCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 THEN BY 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitThenBy(ThenByCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 THEN BY DESC 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitThenByDescending(ThenByDescendingCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 ThenLoad 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitThenLoad(ThenLoadCall method)
        {
            return method;

        }
        /// <summary>
        /// 访问 ThenOrBy 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitThenOrBy(ThenOrByCall method)
        {
            return method;
        }

        /// <summary>
        /// 访问 ThenOrByDescending 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitThenOrByDescending(ThenOrByDescendingCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 TOP 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitTop(TopCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 ToPageList 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitToPageList(ToPageListCall method)
        {
            return method;
        }
        

        /// <summary>
        /// 访问 Truncate 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitTruncate(TruncateCall method)
        {
            return method;
        }

        /// <summary>
        /// 访问 UNION 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitUnion(UnionCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 UNION ALL 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitUnionAll(UnionAllCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 UPDATE 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitUpdate(UpdateCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 DoUpdate 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitDoUpdate(DoUpdateCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 UpdateWhenMatchedAnd 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitUpdateWhenMatchedAnd(UpdateWhenMatchedAndCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 UpdateWhenMatchedAndThenDelete 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitUpdateWhenMatchedAndThenDelete(UpdateWhenMatchedAndThenDeleteCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 UpdateWhenNotMatchedBySourceAnd 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitUpdateWhenNotMatchedBySourceAnd(UpdateWhenNotMatchedBySourceAndCall method)
        {
            return method;
        }

        /// <summary>
        /// 访问 UpdateWithOutput 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitUpdateWithOutput(UpdateWithOutputCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 UpdateWithOutputInto 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitUpdateWithOutputInto(UpdateWithOutputIntoCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 Using 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitUsing(UsingCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 UsingTarget 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitUsingTarget(UsingTargetCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 Value 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitValue(ValueCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 CASE WHEN 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitWhen(WhenCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 WHERE 条件 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitWhere(WhereCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 With 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitWith(WithCall method)
        {
            return method;
        }
        /// <summary>
        /// 访问 WithTableExpression 调用节点（默认返回原节点）。
        /// </summary>
        public virtual MethodCall VisitWithTableExpression(WithTableExpressionCall method)
        {
            return method;
        }

        public virtual MethodCall VisitAllAsync(AllAsyncCall method) => method;
        public virtual MethodCall VisitAnyAsync(AnyAsyncCall method) => method;
        public virtual MethodCall VisitCountAsync(CountAsyncCall method) => method;
        public virtual MethodCall VisitLongCountAsync(LongCountAsyncCall method) => method;
        public virtual MethodCall VisitSumAsync(SumAsyncCall method) => method;
        public virtual MethodCall VisitMinAsync(MinAsyncCall method) => method;
        public virtual MethodCall VisitMaxAsync(MaxAsyncCall method) => method;
        public virtual MethodCall VisitAverageAsync(AverageAsyncCall method) => method;
        public virtual MethodCall VisitFirstAsync(FirstAsyncCall method) => method;
        public virtual MethodCall VisitFirstOrDefaultAsync(FirstOrDefaultAsyncCall method) => method;
        public virtual MethodCall VisitSingleAsync(SingleAsyncCall method) => method;
        public virtual MethodCall VisitSingleOrDefaultAsync(SingleOrDefaultAsyncCall method) => method;
        public virtual MethodCall VisitContainsAsync(ContainsAsyncCall method) => method;
        public virtual MethodCall VisitElementAtAsync(ElementAtAsyncCall method) => method;
        public virtual MethodCall VisitElementAtOrDefaultAsync(ElementAtOrDefaultAsyncCall method) => method;

        #endregion


    }
}