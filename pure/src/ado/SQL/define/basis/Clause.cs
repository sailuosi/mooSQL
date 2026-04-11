using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;


namespace mooSQL.data.model
{
	/// <summary>
	/// 所有SQL模型节点的祖宗类。
	/// </summary>
    public abstract class Clause: ISQLNode
    {
        /// <summary>由子类传入节点类型与 CLR 类型，并登记扩展信息。</summary>
        protected Clause(ClauseType nodeType, Type type)
        {
            // 
            if (_supportInfo == null)
            {
                _supportInfo = new ConcurrentDictionary<Clause, ExtensionInfo>();
            }

            _supportInfo.TryAdd(this, new ExtensionInfo(nodeType, type));
        }
        ///// <summary>
        ///// 此处是为了兼容老代码
        ///// </summary>
        //protected Clause() { 
        
        //}

        private sealed class ExtensionInfo
        {
            public ExtensionInfo(ClauseType nodeType, Type type)
            {
                NodeType = nodeType;
                Type = type;
            }

            internal readonly ClauseType NodeType;
            internal readonly Type Type;
        }

        private static ConcurrentDictionary<Clause,ExtensionInfo> _supportInfo = new ConcurrentDictionary<Clause,ExtensionInfo>();


        /// <summary>语法节点类别（默认从内部字典读取，子类可重写）。</summary>
        public virtual ClauseType NodeType
        {
            get
            {
                if (_supportInfo != null && _supportInfo.TryGetValue(this, out ExtensionInfo? extInfo))
                {
                    return extInfo.NodeType;
                }

                // 
                throw new Exception("子类必须重写属性 Clause.NodeType");
            }
        }

        /// <summary>节点结果 CLR 类型（默认从内部字典读取，子类可重写）。</summary>
        public virtual Type Type
        {
            get
            {
                if (_supportInfo != null && _supportInfo.TryGetValue(this, out ExtensionInfo? extInfo))
                {
                    return extInfo.Type;
                }

                // 
                throw new Exception("子类必须重写属性，Clause.Type");
            }
        }


        /// <summary>访问者入口；默认转扩展访问。</summary>
        public virtual Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitExtension(this);
        }

        /// <summary>是否可在优化阶段归约为更简节点。</summary>
        public virtual bool CanReduce => false;

        /// <summary>归约实现；默认同形返回。</summary>
        public virtual Clause Reduce()
        {
            if (CanReduce) throw new Exception(" Error.ReducibleMustOverrideReduce");
            return this;
        }

        /// <summary>若可归约则先归约再访问，否则返回自身。</summary>
        protected internal virtual Clause VisitChildren(ClauseVisitor visitor)
        {
			if (!CanReduce) {
				return this;
			}
            return visitor.Visit(ReduceAndCheck());
        }

        /// <summary>执行归约并校验新节点非空且类型兼容。</summary>
        public Clause ReduceAndCheck()
        {
            if (!CanReduce) throw new Exception("Error.MustBeReducible");

            Clause newNode = Reduce();

            // 1. Reduction must return a new, non-null node
            // 2. Reduction must return a new node whose result type can be assigned to the type of the original node
            if (newNode == null || newNode == this) throw new Exception("Error.MustReduceToDifferent");
            if (!TypeUtils.AreReferenceAssignable(Type, newNode.Type)) throw new Exception("Error.ReducedNotCompatible");
            return newNode;
        }
    }


	/// <summary>AST 节点类别枚举（表达式、子句、整句等）。</summary>
    public enum ClauseType { 
    
		/// <summary>未分类/占位。</summary>
        none =0,
		/// <summary>列/字段引用。</summary>
		SqlField,
		/// <summary>函数调用。</summary>
		SqlFunction,
		/// <summary>参数占位。</summary>
		SqlParameter,
		/// <summary>一般 SQL 表达式。</summary>
		SqlExpression,
		/// <summary>可空包装表达式。</summary>
		SqlNullabilityExpression,
		/// <summary>锚点/上下文标记。</summary>
		SqlAnchor,
		/// <summary>对象（多属性）表达式。</summary>
		SqlObjectExpression,
		/// <summary>二元运算。</summary>
		SqlBinaryExpression,
		/// <summary>字面量常量。</summary>
		SqlValue,
		/// <summary>数据类型节点。</summary>
		SqlDataType,
		/// <summary>表元数据节点。</summary>
		SqlTable,
		/// <summary>表别名占位。</summary>
		SqlAliasPlaceholder,
		/// <summary>行构造 (a,b,c)。</summary>
		SqlRow,

		/// <summary>谓词抽象基类标记。</summary>
		AffirmWord,
		/// <summary>NOT 谓词。</summary>
		NotPredicate,
		/// <summary>恒真。</summary>
		TruePredicate, 
		/// <summary>恒假。</summary>
		FalsePredicate,
		/// <summary>单表达式谓词。</summary>
		ExprPredicate,
		/// <summary>双表达式比较谓词。</summary>
		ExprExprPredicate,
		/// <summary>LIKE。</summary>
		LikePredicate,
		/// <summary>全文检索类谓词。</summary>
		SearchStringPredicate,
		/// <summary>BETWEEN。</summary>
		BetweenPredicate,
		/// <summary>IS NULL。</summary>
		IsNullPredicate,
		/// <summary>IS DISTINCT FROM。</summary>
		IsDistinctPredicate,
		/// <summary>IS TRUE/FALSE/UNKNOWN。</summary>
		IsTruePredicate,
		/// <summary>IN 子查询。</summary>
		InSubQueryPredicate,
		/// <summary>IN 列表。</summary>
		InListPredicate,
		/// <summary>函数形式谓词（EXISTS 等）。</summary>
		FuncLikePredicate,

		/// <summary>查询块根。</summary>
		SqlQuery,
			/// <summary>SELECT 列表列。</summary>
			Column,
			/// <summary>WHERE/HAVING 搜索条件。</summary>
			SearchCondition,
			/// <summary>FROM 表来源。</summary>
			TableSource,
				/// <summary>JOIN 表。</summary>
				JoinedTable,

			/// <summary>SELECT 子句。</summary>
			SelectClause,
			/// <summary>INSERT 列列表片段。</summary>
			InsertClause,
			/// <summary>UPDATE SET 片段。</summary>
			UpdateClause,
				/// <summary>单列赋值。</summary>
				SetExpression,
			/// <summary>FROM 子句。</summary>
			FromClause,
			/// <summary>WHERE 子句。</summary>
			WhereClause,
			/// <summary>HAVING 子句。</summary>
			HavingClause,
			/// <summary>GROUP BY 子句。</summary>
			GroupByClause,
			/// <summary>ORDER BY 子句。</summary>
			OrderByClause,
				/// <summary>排序项。</summary>
				OrderByItem,
			/// <summary>UNION/EXCEPT 等集合运算。</summary>
			SetOperator,

		/// <summary>WITH 子句。</summary>
		WithClause,
		/// <summary>单个 CTE 定义。</summary>
		CteClause,
		/// <summary>CTE 表引用。</summary>
		SqlCteTable,
		/// <summary>内联原始 SQL 表。</summary>
		SqlRawSqlTable,
		/// <summary>VALUES 表值构造。</summary>
		SqlValuesTable,

		/// <summary>OUTPUT 子句节点（如 SQL Server）。</summary>
		OutputClause,

		/// <summary>SELECT 整句。</summary>
		SelectStatement,
		/// <summary>INSERT 整句。</summary>
		InsertStatement,
		/// <summary>INSERT OR UPDATE（方言）。</summary>
		InsertOrUpdateStatement,
		/// <summary>UPDATE 整句。</summary>
		UpdateStatement,
		/// <summary>DELETE 整句。</summary>
		DeleteStatement,
		/// <summary>MERGE 整句。</summary>
		MergeStatement,
		/// <summary>多行 INSERT。</summary>
		MultiInsertStatement,
			/// <summary>条件 INSERT 分支。</summary>
			ConditionalInsertClause,

		/// <summary>CREATE TABLE。</summary>
		CreateTableStatement,
		/// <summary>DROP TABLE。</summary>
		DropTableStatement,
		/// <summary>TRUNCATE TABLE。</summary>
		TruncateTableStatement,

		/// <summary>表值来源（VALUES/子查询）。</summary>
		SqlTableLikeSource,
		/// <summary>MERGE 分支 WHEN。</summary>
		MergeOperationClause,

		/// <summary>GROUPING SETS 分组集。</summary>
		GroupingSet,

		/// <summary>SQL 注释块。</summary>
		Comment,

		/// <summary>扩展占位节点。</summary>
		SqlExtension,

		/// <summary>
		/// LINQ 中直接使用的内联 SQL 表达式。
		/// </summary>
		SqlInlinedExpression,

		/// <summary>
		/// LINQ 中直接使用的 IToSqlConverter 表达式。
		/// </summary>
		SqlInlinedToSqlExpression,

		/// <summary>
		/// 查询片段上的扩展（如 hint）；见 <see cref="QueryExtension"/>。
		/// </summary>
		SqlQueryExtension,

		/// <summary>CAST。</summary>
		SqlCast,
		/// <summary>COALESCE。</summary>
		SqlCoalesce,
		/// <summary>条件表达式（类三元）。</summary>
		SqlCondition,
		/// <summary>CASE 搜索式。</summary>
		SqlCase,
		/// <summary>
		/// 简单 CASE。
		/// </summary>
		SqlSimpleCase,
		/// <summary>比较/三值比较节点。</summary>
		CompareTo,

		/// <summary>
		/// SQL Builder 编译环境节点。
		/// </summary>
		SQLBuilder,
		/// <summary>
		/// SQL 文本碎片节点。
		/// </summary>
		SQLFrag
    }
}
