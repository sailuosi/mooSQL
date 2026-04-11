using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;

namespace mooSQL.data
{

    using mooSQL.data.model;

	/// <summary>
	/// SQL 提供程序能力标志，供外部提供程序使用。
	/// </summary>
	[DataContract]
	public sealed class SQLProviderFlags
	{
		/// <summary>
		/// 供外部提供程序使用的自定义标志列表。
		/// </summary>
		[DataMember(Order = 1)]
		public List<string> CustomFlags { get; set; } = new List<string>();

		/// <summary>
		/// 表示提供程序（非数据库）使用位置参数而非命名参数（按在查询中的出现顺序赋值，而非按参数名）
		/// </summary>
		[DataMember(Order =  2)]
		public bool        IsParameterOrderDependent      { get; set; }

		/// <summary>
		/// 表示 TAKE/TOP/LIMIT 可接受参数
		/// </summary>
		[DataMember(Order =  3)]
		public bool        AcceptsTakeAsParameter         { get; set; }
		/// <summary>
		/// 表示仅当同时指定了 SKIP/OFFSET 时，TAKE/LIMIT 才可接受参数
		/// </summary>
		[DataMember(Order =  4)]
		public bool        AcceptsTakeAsParameterIfSkip   { get; set; }
		/// <summary>
		/// 表示支持 TOP/TAKE/LIMIT 分页子句
		/// </summary>
		[DataMember(Order =  5)]
		public bool        IsTakeSupported                { get; set; }
		/// <summary>
		/// 表示在无 TAKE 子句时也支持 SKIP/OFFSET 分页子句（参数）。
		/// 若提供程序模拟该能力，即使数据库不支持也可设置此标志。
		/// 例如：<c>TAKE [MAX_ALLOWED_VALUE] SKIP skip_value </c>
		/// </summary>
		[DataMember(Order =  6)]
		public bool        IsSkipSupported                { get; set; }
		/// <summary>
		/// 表示仅当同时指定了 TAKE/LIMIT 时才支持 SKIP/OFFSET 分页子句（参数）
		/// </summary>
		[DataMember(Order =  7)]
		public bool        IsSkipSupportedIfTake          { get; set; }
		/// <summary>
		/// 表示支持的 TAKE/LIMIT 提示类型。
		/// 默认（由 <see cref="DataProviderBase"/> 设置）：<c>null</c>（无）。
		/// </summary>
		[DataMember(Order =  8)]
		public TakeHintType?  TakeHintsSupported              { get; set; }
		/// <summary>
		/// 表示子查询中支持分页子句
		/// </summary>
		[DataMember(Order =  9)]
		public bool        IsSubQueryTakeSupported        { get; set; }

		/// <summary>
		/// 表示提供程序对“带 TOP 的子查询”的 JOIN 存在问题。
		/// 默认 <c>false</c>。
		/// </summary>
		/// <remarks>当前用作 Sybase 缺陷的变通方案。</remarks>
		[DataMember(Order = 10)]
		public bool IsJoinDerivedTableWithTakeInvalid { get; set; }

		/// <summary>
		/// 表示相关子查询中支持分页子句
		/// </summary>
		[DataMember(Order = 11)]
		public bool IsCorrelatedSubQueryTakeSupported { get; set; }

		/// <summary>
		/// 表示提供程序支持无条件的 JOIN（如 ON 1=1）
		/// </summary>
		[DataMember(Order = 12)]
		public bool IsSupportsJoinWithoutCondition { get; set; }
		
		/// <summary>
		/// 表示列表达式子查询中支持 skip 子句
		/// </summary>
		[DataMember(Order =  13)]
		public bool        IsSubQuerySkipSupported        { get; set; }

		/// <summary>
		/// 表示 select 列表中支持标量子查询。
		/// 例如 <c>SELECT (SELECT TOP 1 value FROM some_table) AS MyColumn, ...</c>
		/// </summary>
		[DataMember(Order = 14)]
		public bool        IsSubQueryColumnSupported      { get; set; }
		/// <summary>
		/// 表示子查询中支持 <c>ORDER BY</c> 子句。
		/// </summary>
		[DataMember(Order = 15)]
		public bool        IsSubQueryOrderBySupported     { get; set; }
		/// <summary>
		/// 表示数据库支持将 count 子查询作为列中的标量。
		/// <code>SELECT (SELECT COUNT(*) FROM some_table) FROM ...</code>
		/// </summary>
		[DataMember(Order = 16)]
		public bool        IsCountSubQuerySupported       { get; set; }

		/// <summary>
		/// 表示带自增的插入查询需要显式输出参数才能从数据库获取自增值。
		/// </summary>
		[DataMember(Order = 17)]
		public bool        IsIdentityParameterRequired    { get; set; }
		/// <summary>
		/// 表示支持 OUTER/CROSS APPLY。
		/// </summary>
		[DataMember(Order = 18)]
		public bool        IsApplyJoinSupported           { get; set; }
		/// <summary>
		/// 表示 CROSS APPLY 支持条件，例如 LATERAL JOIN。
		/// </summary>
		[DataMember(Order = 19)]
		public bool IsCrossApplyJoinSupportsCondition { get; set; }
		/// <summary>
		/// 表示 OUTER APPLY 支持条件，例如 LATERAL JOIN。
		/// </summary>
		[DataMember(Order = 20)]
		public bool IsOuterApplyJoinSupportsCondition { get; set; }
		/// <summary>
		/// 表示支持单条查询的“插入或更新”操作。
		/// 否则将用两条查询模拟（先更新，若无更新则再插入）。
		/// </summary>
		[DataMember(Order = 21)]
		public bool        IsInsertOrUpdateSupported      { get; set; }
		/// <summary>
		/// 表示多语句批处理中提供程序可在语句间共享参数。
		/// </summary>
		[DataMember(Order = 22)]
		public bool        CanCombineParameters           { get; set; }
		/// <summary>
		/// 单个 <c>IN</c> 谓词中值的数量上限（不拆成多个 IN 时）。
		/// </summary>
		[DataMember(Order = 23)]
		public int         MaxInListValuesCount           { get; set; }

		/// <summary>
		/// 若为 <c>true</c>，DELETE 语句 OUTPUT 子句中的已删除记录字段应通过特殊表名（如 DELETED 或 OLD）引用；否则用目标表引用
		/// </summary>
		[DataMember(Order = 24)]
		public bool        OutputDeleteUseSpecialTable    { get; set; }
		/// <summary>
		/// 若为 <c>true</c>，INSERT 语句 OUTPUT 子句中的新增记录字段应通过特殊表名（如 INSERTED 或 NEW）引用；否则用目标表引用
		/// </summary>
		[DataMember(Order = 25)]
		public bool        OutputInsertUseSpecialTable    { get; set; }
		/// <summary>
		/// 若为 <c>true</c>，UPDATE 语句的 OUTPUT 子句通过特殊表名同时支持 OLD 与 NEW 数据；否则仅能通过目标表引用更新后的当前记录字段
		/// </summary>
		[DataMember(Order = 26)]
		public bool        OutputUpdateUseSpecialTables   { get; set; }

		/// <summary>
		/// 表示支持 CROSS JOIN
		/// </summary>
		[DataMember(Order = 27)]
		public bool        IsCrossJoinSupported              { get; set; }

		/// <summary>
		/// 表示支持 CTE（公用表表达式）。
		/// 若提供程序不支持 CTE，使用 CTE 时将抛出不支持异常。
		/// </summary>
		[DataMember(Order = 28)]
		public bool IsCommonTableExpressionsSupported     { get; set; }

		/// <summary>
		/// 表示 ORDER BY 语句中支持聚合函数
		/// </summary>
		[DataMember(Order = 29)]
		public bool IsOrderByAggregateFunctionsSupported  { get; set; }

		/// <summary>
		/// 提供程序支持 EXCEPT ALL、INTERSECT ALL 集合运算符；否则将模拟实现
		/// </summary>
		[DataMember(Order = 30)]
		public bool IsAllSetOperationsSupported           { get; set; }

		/// <summary>
		/// 提供程序支持 EXCEPT、INTERSECT 集合运算符；否则将模拟实现
		/// </summary>
		[DataMember(Order = 31)]
		public bool IsDistinctSetOperationsSupported      { get; set; }

		/// <summary>
		/// 提供程序支持带外部引用的聚合表达式，例如：
		/// <code>
		/// SELECT
		/// (
		///		SELECT SUM(inner.FieldX + outer.FieldOuter)
		///		FROM table2 inner
		/// ) AS Sum_Column
		/// FROM table1 outer
		///</code>
		/// 否则聚合表达式会被包装在子查询中，再对子查询列应用聚合函数
		/// </summary>
		[DataMember(Order = 32)]
		public bool AcceptsOuterExpressionInAggregate { get; set; }

		/// <summary>
		/// 表示支持如下 UPDATE 语法：
		/// <code>
		/// UPDATE A
		/// SET ...
		/// FROM B
		/// </code>
		
		/// </summary>
		[DataMember(Order = 33)]
		public bool IsUpdateFromSupported             { get; set; }

		/// <summary>
		/// 提供程序支持命名查询块 QB_NAME(qb)
		/// </summary>
		[DataMember(Order = 34)]
		public bool IsNamingQueryBlockSupported       { get; set; }

		/// <summary>
		/// 表示提供程序支持窗口函数。
		/// 默认值：<c>true</c>。
		/// </summary>
		[DataMember(Order = 35)]
		public bool IsWindowFunctionsSupported { get; set; }

		/// <summary>
		/// 当查询需要多次数据库请求才能完成时使用的隔离级别（如预加载或客户端 GroupBy）。
		/// </summary>
		[DataMember(Order = 36)]
		public IsolationLevel DefaultMultiQueryIsolationLevel { get; set; }

		/// <summary>
		/// 提供程序在不同位置对行构造器 (1, 2, 3) 的支持（标志位）。
		/// </summary>
		[DataMember(Order = 37), DefaultValue(RowFeature.None)]
		public RowFeature RowConstructorSupport { get; set; }

		/// <summary>
		/// 默认值：<c>false</c>。表示是否不支持相关子查询。
		/// </summary>
		[DataMember(Order = 38)]
		public bool DoesNotSupportCorrelatedSubquery { get; set; }

		/// <summary>
		/// 默认值：<c>false</c>。Contains 是否优先使用 Exists。
		/// </summary>
		[DataMember(Order = 39)]
		public bool IsExistsPreferableForContains   { get; set; }

		/// <summary>
		/// 提供程序支持无 ORDER BY 的 ROW_NUMBER OVER ()。
		/// 默认值：<c>true</c>。
		/// </summary>
		[DataMember(Order = 40), DefaultValue(true)]
		public bool IsRowNumberWithoutOrderBySupported { get; set; } = true;

		/// <summary>
		/// 提供程序支持子查询中引用父表的条件。
		/// 默认值：<c>true</c>。
		/// </summary>
		[DataMember(Order = 41), DefaultValue(true)]
		public bool IsSubqueryWithParentReferenceInJoinConditionSupported { get; set; } = true;

		/// <summary>
		/// 提供程序支持嵌套深度大于 1 时引用外部作用域的列子查询。
		/// 默认值：<c>true</c>。
		/// </summary>
		[DataMember(Order = 42), DefaultValue(true)]
		public bool IsColumnSubqueryWithParentReferenceSupported { get; set; } = true;

		/// <summary>
		/// 提供程序支持嵌套深度大于 1 时引用外部作用域的列子查询（且带 Take）。
		/// </summary>
		/// <remarks>
		/// 仅用于 Oracle 11。linq2db 通过 ROWNUM 模拟 Take(n)，会导致额外嵌套。
		/// 默认值：<c>true</c>。
		/// </remarks>
		[DataMember(Order = 43), DefaultValue(true)]
		public bool IsColumnSubqueryWithParentReferenceAndTakeSupported { get; set; } = true;

		/// <summary>
		/// 针对 Oracle 在“列列表中含父表列且带 IS NOT NULL 条件的子查询”的缺陷的变通。
		/// 默认值：<c>false</c>。
		/// </summary>
		/// <remarks>
		/// 参见 Issue3557Case1 测试。
		/// </remarks>
		[DataMember(Order = 44), DefaultValue(false)]
		public bool IsColumnSubqueryShouldNotContainParentIsNotNull { get; set; } = false;

		/// <summary>
		/// 提供程序支持在递归 CTE 内带条件的 INNER JOIN（目前仅 DB2 不支持）。
		/// 默认值：<c>true</c>。
		/// </summary>
		[DataMember(Order = 45), DefaultValue(true)]
		public bool IsRecursiveCTEJoinWithConditionSupported { get; set; } = true;

		/// <summary>
		/// 提供程序支持在 OUTER JOIN 内使用 INNER JOIN，例如：
		/// <code>
		/// LEFT JOIN table1 ON ...
		///	   INNER JOIN query ON ...
		/// </code>
		///
		/// 否则会生成：LEFT JOIN (SELECT ... FROM table1 INNER JOIN query ON ...) 形式。
		/// 默认：<c>true</c>。目前仅 Access 不支持。
		/// </summary>
		[DataMember(Order = 46), DefaultValue(true)]
		public bool IsOuterJoinSupportsInnerJoin { get; set; } = true;

		/// <summary>
		/// 表示提供程序支持 FROM 子句中多表与 JOIN 混合（如 table1 INNER JOIN query ON ... , table2）。
		/// 否则会生成子查询形式。默认：<c>true</c>。目前仅 Access 不支持。
		/// </summary>
		[DataMember(Order = 47), DefaultValue(true)]
		public bool IsMultiTablesSupportsJoins { get; set; } = true;

		/// <summary>
		/// 表示顶层 CTE 查询支持 ORDER BY。
		/// 默认值：<c>true</c>。
		/// </summary>
		[DataMember(Order = 48), DefaultValue(true)]
		public bool IsCTESupportsOrdering { get; set; } = true;

		/// <summary>
		/// 表示提供程序在 LEFT JOIN 转换上存在缺陷（如 can_be_null 被错误地视为始终非 null）。
		/// 变通做法是对所有投影字段做可空性检查。默认值：<c>false</c>。
		/// </summary>
		[DataMember(Order =  49)]
		public bool IsAccessBuggyLeftJoinConstantNullability { get; set; }

		/// <summary>
		/// 表示提供程序支持布尔类型。
		/// 默认值：<c>false</c>。
		/// </summary>
		[DataMember(Order =  50)]
		public bool SupportsBooleanComparison { get; set; }

		/// <summary>
		/// 提供程序支持嵌套 JOIN（如 A JOIN (B JOIN C ON ?) ON ?），否则会替换为子查询形式
		/// </summary>
		[DataMember(Order = 51), DefaultValue(true)]
		public bool IsNestedJoinsSupported { get; set; } = true;

		/// <summary>
		/// 提供程序支持 COUNT(DISTINCT column)；否则将模拟实现
		/// </summary>
		[DataMember(Order = 52)]
		public bool IsCountDistinctSupported { get; set; }

		/// <summary>
		/// 提供程序支持 SUM/AVG/MIN/MAX(DISTINCT column)；否则将模拟实现
		/// </summary>
		[DataMember(Order = 53)]
		public bool IsAggregationDistinctSupported { get; set; }

		/// <summary>
		/// 提供程序支持派生表中的 ORDER BY；否则将模拟实现
		/// </summary>
		[DataMember(Order = 54)]
		public bool IsDerivedTableOrderBySupported { get; set; }

		/// <summary>
		/// 提供程序支持 UPDATE 查询的 TAKE 限制
		/// </summary>
		[DataMember(Order = 55)]
		public bool IsUpdateTakeSupported { get; set; }

		/// <summary>
		/// 提供程序支持 UPDATE 查询的 SKIP+TAKE 限制
		/// </summary>
		[DataMember(Order = 56)]
		public bool IsUpdateSkipTakeSupported { get; set; }

		/// <summary>
		/// 提供程序支持可简单转换为 JOIN 的相关子查询
		/// </summary>
		/// <remarks>
		/// 仅用于 ClickHouse 提供程序。
		/// </remarks>
		[DataMember(Order = 57)]
		public bool IsSupportedSimpleCorrelatedSubqueries { get; set; }
		/// <summary>
		/// 
		/// </summary>
		/// <param name="selectQuery"></param>
		/// <returns></returns>
		public bool GetAcceptsTakeAsParameterFlag(SelectQueryClause selectQuery)
		{
			return AcceptsTakeAsParameter || AcceptsTakeAsParameterIfSkip && selectQuery.Select.SkipValue != null;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="takeExpression"></param>
		/// <param name="skipExpression"></param>
		/// <returns></returns>
		public bool GetIsSkipSupportedFlag(IExpWord? takeExpression, IExpWord? skipExpression)
		{
			return IsSkipSupported || IsSkipSupportedIfTake && takeExpression != null;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="hints"></param>
		/// <returns></returns>
		public bool GetIsTakeHintsSupported(TakeHintType hints)
		{
			if (TakeHintsSupported == null)
				return false;

			return (TakeHintsSupported.Value & hints) == hints;
		}

		#region Equality
		/// <summary>
		/// 当前需要相等性支持以便在远程上下文中避免错误使用带有不同标志的缓存依赖类型
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return IsParameterOrderDependent                           .GetHashCode()
				^ AcceptsTakeAsParameter                               .GetHashCode()
				^ AcceptsTakeAsParameterIfSkip                         .GetHashCode()
				^ IsTakeSupported                                      .GetHashCode()
				^ IsSkipSupported                                      .GetHashCode()
				^ IsSkipSupportedIfTake                                .GetHashCode()
				^ IsSubQueryTakeSupported                              .GetHashCode()
				^ IsJoinDerivedTableWithTakeInvalid                    .GetHashCode()
				^ IsCorrelatedSubQueryTakeSupported                    .GetHashCode()
				^ IsSupportsJoinWithoutCondition                       .GetHashCode()
				^ IsSubQuerySkipSupported                              .GetHashCode()
				^ IsSubQueryColumnSupported                            .GetHashCode()
				^ IsSubQueryOrderBySupported                           .GetHashCode()
				^ IsCountSubQuerySupported                             .GetHashCode()
				^ IsIdentityParameterRequired                          .GetHashCode()
				^ IsApplyJoinSupported                                 .GetHashCode()
				^ IsCrossApplyJoinSupportsCondition                    .GetHashCode()
				^ IsOuterApplyJoinSupportsCondition                    .GetHashCode()
				^ IsInsertOrUpdateSupported                            .GetHashCode()
				^ CanCombineParameters                                 .GetHashCode()
				^ MaxInListValuesCount                                 .GetHashCode()
				^ (TakeHintsSupported?                                 .GetHashCode() ?? 0)
				^ IsCrossJoinSupported                                 .GetHashCode()
				^ IsCommonTableExpressionsSupported                    .GetHashCode()
				^ IsOrderByAggregateFunctionsSupported                 .GetHashCode()
				^ IsAllSetOperationsSupported                          .GetHashCode()
				^ IsDistinctSetOperationsSupported                     .GetHashCode()
				^ IsCountDistinctSupported                             .GetHashCode()
				^ IsNestedJoinsSupported                               .GetHashCode()
				^ IsAggregationDistinctSupported                       .GetHashCode()
				^ IsUpdateFromSupported                                .GetHashCode()
				^ DefaultMultiQueryIsolationLevel                      .GetHashCode()
				^ AcceptsOuterExpressionInAggregate                    .GetHashCode()
				^ IsNamingQueryBlockSupported                          .GetHashCode()
				^ IsWindowFunctionsSupported                           .GetHashCode()
				^ RowConstructorSupport                                .GetHashCode()
				^ OutputDeleteUseSpecialTable                          .GetHashCode()
				^ OutputInsertUseSpecialTable                          .GetHashCode()
				^ OutputUpdateUseSpecialTables                         .GetHashCode()
				^ DoesNotSupportCorrelatedSubquery                     .GetHashCode()
				^ IsExistsPreferableForContains                        .GetHashCode()
				^ IsRowNumberWithoutOrderBySupported                   .GetHashCode()
				^ IsSubqueryWithParentReferenceInJoinConditionSupported.GetHashCode()
				^ IsColumnSubqueryWithParentReferenceSupported         .GetHashCode()
				^ IsColumnSubqueryWithParentReferenceAndTakeSupported  .GetHashCode()
				^ IsColumnSubqueryShouldNotContainParentIsNotNull      .GetHashCode()
				^ IsRecursiveCTEJoinWithConditionSupported             .GetHashCode()
				^ IsOuterJoinSupportsInnerJoin                         .GetHashCode()
				^ IsMultiTablesSupportsJoins                           .GetHashCode()
				^ IsCTESupportsOrdering                                .GetHashCode()
				^ IsAccessBuggyLeftJoinConstantNullability             .GetHashCode()
				^ SupportsBooleanComparison                            .GetHashCode()
				^ IsDerivedTableOrderBySupported                       .GetHashCode()
				^ IsUpdateTakeSupported                                .GetHashCode()
				^ IsUpdateSkipTakeSupported                            .GetHashCode()
				^ IsSupportedSimpleCorrelatedSubqueries                .GetHashCode()
				^ CustomFlags.Aggregate(0, (hash, flag) => flag.GetHashCode() ^ hash);
	}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object? obj)
		{
			return obj is SQLProviderFlags other
				&& IsParameterOrderDependent                             == other.IsParameterOrderDependent
				&& AcceptsTakeAsParameter                                == other.AcceptsTakeAsParameter
				&& AcceptsTakeAsParameterIfSkip                          == other.AcceptsTakeAsParameterIfSkip
				&& IsTakeSupported                                       == other.IsTakeSupported
				&& IsSkipSupported                                       == other.IsSkipSupported
				&& IsSkipSupportedIfTake                                 == other.IsSkipSupportedIfTake
				&& IsSubQueryTakeSupported                               == other.IsSubQueryTakeSupported
				&& IsJoinDerivedTableWithTakeInvalid                     == other.IsJoinDerivedTableWithTakeInvalid
				&& IsCorrelatedSubQueryTakeSupported                     == other.IsCorrelatedSubQueryTakeSupported
				&& IsSupportsJoinWithoutCondition                        == other.IsSupportsJoinWithoutCondition
				&& IsSubQuerySkipSupported                               == other.IsSubQuerySkipSupported
				&& IsSubQueryColumnSupported                             == other.IsSubQueryColumnSupported
				&& IsSubQueryOrderBySupported                            == other.IsSubQueryOrderBySupported
				&& IsCountSubQuerySupported                              == other.IsCountSubQuerySupported
				&& IsIdentityParameterRequired                           == other.IsIdentityParameterRequired
				&& IsApplyJoinSupported                                  == other.IsApplyJoinSupported
				&& IsCrossApplyJoinSupportsCondition                     == other.IsCrossApplyJoinSupportsCondition
				&& IsOuterApplyJoinSupportsCondition                     == other.IsOuterApplyJoinSupportsCondition
				&& IsInsertOrUpdateSupported                             == other.IsInsertOrUpdateSupported
				&& CanCombineParameters                                  == other.CanCombineParameters
				&& MaxInListValuesCount                                  == other.MaxInListValuesCount
				&& TakeHintsSupported                                    == other.TakeHintsSupported
				&& IsCrossJoinSupported                                  == other.IsCrossJoinSupported
				&& IsCommonTableExpressionsSupported                     == other.IsCommonTableExpressionsSupported
				&& IsOrderByAggregateFunctionsSupported                  == other.IsOrderByAggregateFunctionsSupported
				&& IsAllSetOperationsSupported                           == other.IsAllSetOperationsSupported
				&& IsDistinctSetOperationsSupported                      == other.IsDistinctSetOperationsSupported
				&& IsCountDistinctSupported                              == other.IsCountDistinctSupported
				&& IsNestedJoinsSupported                                == other.IsNestedJoinsSupported
				&& IsAggregationDistinctSupported                        == other.IsAggregationDistinctSupported
				&& IsUpdateFromSupported                                 == other.IsUpdateFromSupported
				&& DefaultMultiQueryIsolationLevel                       == other.DefaultMultiQueryIsolationLevel
				&& AcceptsOuterExpressionInAggregate                     == other.AcceptsOuterExpressionInAggregate
				&& IsNamingQueryBlockSupported                           == other.IsNamingQueryBlockSupported
				&& IsWindowFunctionsSupported                            == other.IsWindowFunctionsSupported
				&& RowConstructorSupport                                 == other.RowConstructorSupport
				&& OutputDeleteUseSpecialTable                           == other.OutputDeleteUseSpecialTable
				&& OutputInsertUseSpecialTable                           == other.OutputInsertUseSpecialTable
				&& OutputUpdateUseSpecialTables                          == other.OutputUpdateUseSpecialTables
				&& DoesNotSupportCorrelatedSubquery                      == other.DoesNotSupportCorrelatedSubquery
				&& IsExistsPreferableForContains                         == other.IsExistsPreferableForContains
				&& IsRowNumberWithoutOrderBySupported                    == other.IsRowNumberWithoutOrderBySupported
				&& IsSubqueryWithParentReferenceInJoinConditionSupported == other.IsSubqueryWithParentReferenceInJoinConditionSupported
				&& IsColumnSubqueryWithParentReferenceSupported          == other.IsColumnSubqueryWithParentReferenceSupported
				&& IsColumnSubqueryWithParentReferenceAndTakeSupported   == other.IsColumnSubqueryWithParentReferenceAndTakeSupported
				&& IsColumnSubqueryShouldNotContainParentIsNotNull       == other.IsColumnSubqueryShouldNotContainParentIsNotNull
				&& IsRecursiveCTEJoinWithConditionSupported              == other.IsRecursiveCTEJoinWithConditionSupported
				&& IsOuterJoinSupportsInnerJoin                          == other.IsOuterJoinSupportsInnerJoin
				&& IsMultiTablesSupportsJoins                            == other.IsMultiTablesSupportsJoins
				&& IsCTESupportsOrdering                                 == other.IsCTESupportsOrdering
				&& IsAccessBuggyLeftJoinConstantNullability              == other.IsAccessBuggyLeftJoinConstantNullability
				&& SupportsBooleanComparison                             == other.SupportsBooleanComparison
				&& IsDerivedTableOrderBySupported                        == other.IsDerivedTableOrderBySupported
				&& IsUpdateTakeSupported                                 == other.IsUpdateTakeSupported
				&& IsUpdateSkipTakeSupported                             == other.IsUpdateSkipTakeSupported
				&& IsSupportedSimpleCorrelatedSubqueries                 == other.IsSupportedSimpleCorrelatedSubqueries
				// CustomFlags as List wasn't best idea
				&& CustomFlags.Count                                     == other.CustomFlags.Count
				&& (CustomFlags.Count                                    == 0
					|| CustomFlags.OrderBy(_ => _).SequenceEqual(other.CustomFlags.OrderBy(_ => _)));
		}
		#endregion
	}

}
