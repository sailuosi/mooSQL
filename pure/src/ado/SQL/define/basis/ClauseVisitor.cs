/* 调用关系说明
 * 根访问器 Visit方法和接口的Visit 方法为入口，实体类不得调用，防止无限循环。
 * 每个叶子节点的模型类，均有一个对应自己的Visit方法，默认情况下，该类在Accept方法中调用自己对应的Visit方法。
 * 
 * 原理：
 * Accept 方法和访问器的每个具体访问方法之间的关系类似于一个分解开的switch方法，形成了实体类和访问方法之间的一种映射关系。
 * 调用链路： 访问器根和接口的Visit → 模型的Accept → 访问器的具体VisitXX 方法
 * 
 * 由于访问器的所有可有具体访问方法必须在核心侧就定义好，业务侧需要扩展时，就只能继承根访问器，然后通过switch来切换。这符合访问者模式的情况。
 */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using mooSQL.data.model.affirms;
using mooSQL.linq.SqlQuery;

namespace mooSQL.data.model
{
    /// <summary>
    /// SQL模型访问器，代表访问者或修改者。
    /// 用来被继承，以便自定义遍历、修改、复制SQL树
    /// </summary>
    public abstract class ClauseVisitor
    {

        protected ClauseVisitor() {

        }

        public DBInstance DBLive { get; set; }

        protected internal virtual Clause VisitExtension(Clause node)
        {
            return node.VisitChildren(this);
        }

        public ReadOnlyCollection<Clause> Visit(ReadOnlyCollection<Clause> nodes)
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(nodes);
#endif

            Clause[]? newNodes = null;
            for (int i = 0, n = nodes.Count; i < n; i++)
            {
                Clause node = Visit(nodes[i]);

                if (newNodes != null)
                {
                    newNodes[i] = node;
                }
                else if (!object.ReferenceEquals(node, nodes[i]))
                {
                    newNodes = new Clause[n];
                    for (int j = 0; j < i; j++)
                    {
                        newNodes[j] = nodes[j];
                    }
                    newNodes[i] = node;
                }
            }
            if (newNodes == null)
            {
                return nodes;
            }
            return new ReadOnlyCollection<Clause>(newNodes);
        }
        public List<Clause> VisitList(List<Clause> nodes)
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(nodes);
#endif

            List<Clause> newNodes = null;
            for (int i = 0, n = nodes.Count; i < n; i++)
            {
                Clause node = Visit(nodes[i]);

                if (newNodes != null)
                {
                    newNodes[i] = node;
                }
                else if (!object.ReferenceEquals(node, nodes[i]))
                {
                    newNodes = new List<Clause>();
                    for (int j = 0; j < i; j++)
                    {
                        newNodes[j] = nodes[j];
                    }
                    newNodes[i] = node;
                }
            }
            if (newNodes == null)
            {
                return nodes;
            }
            return newNodes;
        }
        public List<T> VisitEach<T>(List<T> nodes) where T : Clause
        {
            if (nodes == null) return nodes;

            for (int i = 0, n = nodes.Count; i < n; i++)
            {
                var node = Visit(nodes[i]);
                if (Object.ReferenceEquals(node, nodes[i])) {
                    nodes[i] = node as T;
                }
            }
            return nodes;
        }
        public List<T> VisitEach<T>(List<T> nodes, Func<T, T> func) where T : Clause
        {
            if (nodes == null) return nodes;

            for (int i = 0, n = nodes.Count; i < n; i++)
            {
                var node = func(nodes[i]);
                if (Object.ReferenceEquals(node, nodes[i]))
                {
                    nodes[i] = node as T;
                }
            }
            return nodes;
        }


        public static List<T> Visit<T>(List<T> nodes, Func<T, T> elementVisitor)
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(nodes);
            ArgumentNullException.ThrowIfNull(elementVisitor);
#endif

            T[]? newNodes = null;
            for (int i = 0, n = nodes.Count; i < n; i++)
            {
                T node = elementVisitor(nodes[i]);
                if (newNodes != null)
                {
                    newNodes[i] = node;
                }
                else if (!object.ReferenceEquals(node, nodes[i]))
                {
                    newNodes = new T[n];
                    for (int j = 0; j < i; j++)
                    {
                        newNodes[j] = nodes[j];
                    }
                    newNodes[i] = node;
                }
            }
            if (newNodes == null)
            {
                return nodes;
            }
            return new List<T>(newNodes);
        }

        public T? VisitAndConvert<T>(T? node, string? callerName) where T : Clause
        {
            if (node == null)
            {
                return null;
            }
            node = (Visit(node) as T);
            if (node == null)
            {
                throw new Exception(" Error.MustRewriteToSameNode(" + callerName + typeof(T) + callerName + ")");
            }
            return node;
        }

        public ReadOnlyCollection<T> VisitAndConvert<T>(ReadOnlyCollection<T> nodes, string? callerName) where T : Clause
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(nodes);
#endif

            T[]? newNodes = null;
            for (int i = 0, n = nodes.Count; i < n; i++)
            {
                T? node = Visit(nodes[i]) as T;
                if (node == null)
                {
                    throw new Exception(" Error.MustRewriteToSameNode(callerName, typeof(T), callerName)");
                }

                if (newNodes != null)
                {
                    newNodes[i] = node;
                }
                else if (!object.ReferenceEquals(node, nodes[i]))
                {
                    newNodes = new T[n];
                    for (int j = 0; j < i; j++)
                    {
                        newNodes[j] = nodes[j];
                    }
                    newNodes[i] = node;
                }
            }
            if (newNodes == null)
            {
                return nodes;
            }
            return new ReadOnlyCollection<T>(newNodes);
        }

        #region 抽象访问者,禁止表达式调用
        public virtual Clause Visit(Clause node)
        {
            return node.Accept(this);
        }
        public virtual Clause VisitAffirmWord(IAffirmWord affirmWord) { 
            return affirmWord.Accept(this);
        }
        public virtual Clause VisitIExpWord(IExpWord field)
        {
            return field.Accept(this);
        }

        public virtual Clause VisitTableNode(ITableNode clause)
        {
            return clause.Accept(this);
        }
        #endregion


        #region 具体类型的访问者，可被表达式调用
        /// <summary>
        /// 
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public virtual Clause VisitFieldWord(FieldWord field) {
            return field;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public virtual Clause VisitFunctionWord(FunctionWord field)
        {
            return field;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public virtual Clause VisitParameter(ParameterWord field)
        {
            return field;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public virtual Clause VisitExpression(ExpressionWord field)
        {
            return field;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitNullabilityExpression(NullabilityWord clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitAnchorWord(AnchorWord clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitObjectExpression(ObjectWord clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitBinaryExpression(BinaryWord clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitValueWord(ValueWord clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitDataTypeWord(DataTypeWord clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitTableWord(TableWord clause)
        {
            return clause;
        }

        public virtual Clause VisitDerivatedTable(DerivatedTableWord clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitBoxTable(BoxTable clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitAliasPlaceholder(AliasPlaceholderWord clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitRowWord(RowWord clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitAffirmNot(Not clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitTrueAffirm(TrueAffirm clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitFalseAffirm(FalseAffirm clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitAffirmExpr(Expr clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitAffirmExprExpr(ExprExpr clause)
        {
            clause.Expr1 = VisitIExpWord(clause.Expr1) as IExpWord;
            clause.Expr2 = VisitIExpWord(clause.Expr2) as IExpWord;
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitAffirmLike(Like clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitSearchStringPredicate(SearchString clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitAffirmBetween(Between clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitAffirmIsNull(IsNull clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitAffirmIsDistinct(IsDistinct clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitAffirmIsTrue(IsTrue clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitAffirmInSubQuery(InSubQuery clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitAffirmInList(InList clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitAffirmFuncLike(FuncLike clause)
        {
            return clause;
        }
        public virtual Clause VisitSqlQuery(SelectQueryClause clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitColumnWord(ColumnWord clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitSearchCondition(SearchConditionWord clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitTableSource(TableSourceWord clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitJoinedTable(JoinTableWord clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitSelectClause(SelectClause clause)
        {
            if (clause == null) { return clause; }
            if (clause.TakeValue != null) {
                clause.TakeValue=VisitIExpWord(clause.TakeValue) as IExpWord;
            }
            if (clause.SkipValue != null)
            {
                clause.SkipValue=VisitIExpWord(clause.SkipValue) as IExpWord;
            }
            //遍历明细
            if (clause.Columns !=null && clause.Columns.Count > 0) {
                for (int i = 0; i < clause.Columns.Count; i++) {
                    clause.Columns[i]= VisitColumnWord(clause.Columns[i]) as ColumnWord;
                }
            }
            return clause;
        }
        /// <summary>
        /// 标准SELECT 语句的遍历
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitSelectQuery(SelectQueryClause clause)
        {
            clause.Select =VisitSelectClause(clause.Select) as SelectClause;
            clause.From = VisitFromClause(clause.From) as FromClause;
            clause.Where = VisitWhereClause(clause.Where) as WhereClause;
            clause.GroupBy = VisitGroupByClause(clause.GroupBy) as GroupByClause;
            clause.Having = VisitHavingClause(clause.Having) as HavingClause;
            clause.OrderBy = VisitOrderByClause(clause.OrderBy) as OrderByClause;
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitInsertClause(InsertClause clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitUpdateClause(UpdateClause clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitSetWord(SetWord clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitFromClause(FromClause clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitWhereClause(WhereClause clause)
        {
            var sh= VisitSearchCondition(clause.SearchCondition);
            clause.SearchCondition = sh as SearchConditionWord;
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitHavingClause(HavingClause clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitGroupByClause(GroupByClause clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitOrderByClause(OrderByClause clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitOrderByItem(OrderByWord clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitSetOperator(SetOperatorWord clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="with"></param>
        /// <returns></returns>
        public virtual Clause VisitWithClause(WithClause with)
        {
            if (with == null || with.Clauses.Count == 0)
                return with;

            for (var i=0;i<with.Clauses.Count;i++)
            {
                var clause = with.Clauses[i];
                with.Clauses[i] = VisitCteClause(clause) as CTEClause;
            }
            return with;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitCteClause(CTEClause clause)
        {
            clause.Body = VisitSqlQuery(clause.Body) as SelectQueryClause;
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitCteTable(CteTableWord clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitRawSqlTable(RawSqlTableWord clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitValuesTable(ValuesTableWord clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitOutputClause(OutputClause clause)
        {
            var insertedTable = (ITableNode?)VisitTableNode(clause.InsertedTable);
            var deletedTable = (ITableNode?)VisitTableNode(clause.DeletedTable);
            var outputTable = (ITableNode?)VisitTableNode(clause.OutputTable);

            VisitEach(clause.OutputColumns);

            if (clause.HasOutputItems)
            {
                VisitEach(clause.OutputItems);
            }

            clause.Update(insertedTable, deletedTable, outputTable);
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitSelectSentence(SelectSentence clause)
        {
            clause.Tag = (CommentWord?)VisitComment(clause.Tag);
            clause.With = (WithClause?)VisitWithClause(clause.With);
            clause.SelectQuery = (SelectQueryClause?)VisitSqlQuery(clause.SelectQuery);
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitInsertSentence(InsertSentence clause)
        {
            clause.Tag = (CommentWord?)VisitComment(clause.Tag);
            clause.With = (WithClause?)VisitWithClause(clause.With);
            clause.SelectQuery = (SelectQueryClause?)VisitSqlQuery(clause.SelectQuery);
            clause.Insert = (InsertClause)VisitInsertClause(clause.Insert);
            clause.Output = (OutputClause?)VisitOutputClause(clause.Output);
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitInsertOrUpdateSentence(InsertOrUpdateSentence clause)
        {
            clause.Tag = (CommentWord?)VisitComment(clause.Tag);
            clause.With = (WithClause?)VisitWithClause(clause.With);
            clause.SelectQuery = (SelectQueryClause?)VisitSqlQuery(clause.SelectQuery);
            clause.Insert = (InsertClause)VisitInsertClause(clause.Insert);
            clause.Update = VisitUpdateClause(clause.Update) as UpdateClause;
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitUpdateSentence(UpdateSentence clause)
        {
            clause.Tag = (CommentWord?)VisitComment(clause.Tag);
            clause.With = (WithClause?)VisitWithClause(clause.With);
            clause.SelectQuery = (SelectQueryClause?)VisitSqlQuery(clause.SelectQuery);
            clause.Update = VisitUpdateClause(clause.Update) as UpdateClause;
            clause.SqlQueryExtensions = Visit(clause.SqlQueryExtensions, (c) =>
            {
                return VisitQueryExtension(c) as QueryExtension;
            });
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitDeleteSentence(DeleteSentence clause)
        {
            clause.Tag = (CommentWord?)VisitComment(clause.Tag);
            clause.With = (WithClause?)VisitWithClause(clause.With);
            clause.SelectQuery = (SelectQueryClause?)VisitSqlQuery(clause.SelectQuery);
            clause.Table = VisitTableNode(clause.Table) as ITableNode;
            clause.Top = VisitTableNode(clause.Table);
            clause.SqlQueryExtensions = Visit(clause.SqlQueryExtensions, (c) =>
            {
                return VisitQueryExtension(c) as QueryExtension;
            });
            return clause;

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitMergeSentence(MergeSentence clause)
        {
            clause.Tag = (CommentWord?)VisitComment(clause.Tag);
            clause.With = (WithClause?)VisitWithClause(clause.With);

            var target = (ITableNode)VisitTableNode(clause.Target);
            var source = (TableLikeSourceWord)VisitTableNode(clause.Source);
            var on = (SearchConditionWord)VisitSearchCondition(clause.On);
            var output = (OutputClause?)VisitOutputClause(clause.Output);

            VisitEach(clause.Operations, (c) => VisitMergeOperationClause(c) as MergeOperationClause);

            clause.SqlQueryExtensions = Visit(clause.SqlQueryExtensions, (c) =>
            {
                return VisitQueryExtension(c) as QueryExtension;
            });
            clause.Update(target, source, on, output);
            return clause;

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitMultiInsertSentence(MultiInsertSentence clause)
        {
            var source = (ITableNode)VisitTableLikeSource(clause.Source as TableLikeSourceWord);
            VisitEach(clause.Inserts, (c) => VisitConditionalInsertClause(c) as ConditionalInsertClause);
            clause.SqlQueryExtensions = Visit(clause.SqlQueryExtensions, (c) =>
            {
                return VisitQueryExtension(c) as QueryExtension;
            });

            clause.Update(source);
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitConditionalInsertClause(ConditionalInsertClause clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitCreateTableSentence(CreateTableSentence clause)
        {
            clause.Tag = VisitComment(clause.Tag) as CommentWord;

            var table = (ITableNode)VisitTableNode(clause.Table);

            clause.SqlQueryExtensions = Visit(clause.SqlQueryExtensions, (c) =>
            {
                return VisitQueryExtension(c) as QueryExtension;
            });

            clause.Update(table);
            return clause;

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitDropTableSentence(DropTableSentence clause)
        {
            clause.Tag = VisitComment(clause.Tag) as CommentWord;

            var table = (ITableNode)VisitTableNode(clause.Table);

            clause.SqlQueryExtensions = Visit(clause.SqlQueryExtensions, (c) =>
            {
                return VisitQueryExtension(c) as QueryExtension;
            });

            clause.Update(table);
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitTruncateTableSentence(TruncateTableSentence clause)
        {
            clause.Tag = VisitComment(clause.Tag) as CommentWord;

            clause.Table = (ITableNode)VisitTableNode(clause.Table);

            clause.SqlQueryExtensions = Visit(clause.SqlQueryExtensions, (c) =>
            {
                return VisitQueryExtension(c) as QueryExtension;
            });

            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitTableLikeSource(TableLikeSourceWord clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitMergeOperationClause(MergeOperationClause clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitGroupingSet(GroupingSetWord clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitComment(CommentWord clause)
        {
            return clause;
        }
        //public virtual Clause VisitSqlExtension(IQueryExtension clause)
        //{
        //    return clause;
        //}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitInlinedSqlExpression(InlinedSqlWord clause)
        {
            var parameter = (ParameterWord)VisitParameter(clause.Parameter);
            var inlinedValue = (ExpressionWord)VisitIExpWord(clause.InlinedValue);
            clause.Modify(parameter, inlinedValue);
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitInlinedToSqlExpression(InlinedToSqlWord clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitQueryExtension(QueryExtension clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitConditionExpression(ConditionWord clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitCastExpression(CastWord clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitCoalesceExpression(CoalesceWord clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitCaseExpression(CaseWord clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitCompareToExpression(CompareToWord clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitSQLBuilder(SQLBuilderClause clause) { 
            return clause;
        }
        public virtual Clause VisitSQLBuilders(SQLBuildersClause clause)
        {
            return clause;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clause"></param>
        /// <returns></returns>
        public virtual Clause VisitSQLFrag(SQLFragClause clause)
        {
            return clause;
        }
        #endregion
    }
}
