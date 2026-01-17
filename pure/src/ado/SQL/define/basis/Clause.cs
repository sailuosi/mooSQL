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


        public virtual Clause Accept(ClauseVisitor visitor)
        {
            return visitor.VisitExtension(this);
        }

        public virtual bool CanReduce => false;

        public virtual Clause Reduce()
        {
            if (CanReduce) throw new Exception(" Error.ReducibleMustOverrideReduce");
            return this;
        }

        protected internal virtual Clause VisitChildren(ClauseVisitor visitor)
        {
			if (!CanReduce) {
				return this;
			}
            return visitor.Visit(ReduceAndCheck());
        }

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


    public enum ClauseType { 
    
        none =0,
		SqlField,
		SqlFunction,
		SqlParameter,
		SqlExpression,
		SqlNullabilityExpression,
		SqlAnchor,
		SqlObjectExpression,
		SqlBinaryExpression,
		SqlValue,
		SqlDataType,
		SqlTable,
		SqlAliasPlaceholder,
		SqlRow,

		AffirmWord,
		NotPredicate,
		TruePredicate, 
		FalsePredicate,
		ExprPredicate,
		ExprExprPredicate,
		LikePredicate,
		SearchStringPredicate,
		BetweenPredicate,
		IsNullPredicate,
		IsDistinctPredicate,
		IsTruePredicate,
		InSubQueryPredicate,
		InListPredicate,
		FuncLikePredicate,

		SqlQuery,
			Column,
			SearchCondition,
			TableSource,
				JoinedTable,

			SelectClause,
			InsertClause,
			UpdateClause,
				SetExpression,
			FromClause,
			WhereClause,
			HavingClause,
			GroupByClause,
			OrderByClause,
				OrderByItem,
			SetOperator,

		WithClause,
		CteClause,
		SqlCteTable,
		SqlRawSqlTable,
		SqlValuesTable,

		OutputClause,

		SelectStatement,
		InsertStatement,
		InsertOrUpdateStatement,
		UpdateStatement,
		DeleteStatement,
		MergeStatement,
		MultiInsertStatement,
			ConditionalInsertClause,

		CreateTableStatement,
		DropTableStatement,
		TruncateTableStatement,

		SqlTableLikeSource,
		MergeOperationClause,

		GroupingSet,

		Comment,

		SqlExtension,

		/// <summary>
		/// ISqlExpression used in LINQ query directly
		/// </summary>
		SqlInlinedExpression,

		/// <summary>
		/// IToSqlConverter used in LINQ query directly
		/// </summary>
		SqlInlinedToSqlExpression,

		/// <summary>
		/// Custom query extensions, e.g. hints, applied to specific query fragment.
		/// Implemented by <see cref="SqlQuery.SqlQueryExtension"/>.
		/// </summary>
		SqlQueryExtension,

		SqlCast,
		SqlCoalesce,
		SqlCondition,
		SqlCase,
		SqlSimpleCase,
		CompareTo,

		/// <summary>
		/// 编译环境
		/// </summary>
		SQLBuilder,
		/// <summary>
		/// SQL碎片
		/// </summary>
		SQLFrag
    }
}
