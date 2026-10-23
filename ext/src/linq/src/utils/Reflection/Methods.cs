using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using mooSQL.linq;

namespace mooSQL.linq.utils
{
	using mooSQL.linq.expressions;
	using mooSQL.data;
	using mooSQL.data.model;
    using mooSQL.linq.clause;
	/// <summary>
	/// This API supports the mooSQL LINQ infrastructure and is not intended to be used directly from your code.
	/// This API may change or be removed in future releases.
	/// </summary>
	public static class Methods
	{
		/// <summary>
		/// ADONet
		/// </summary>
		/// <summary>
		/// ADONet
		/// </summary>
		public static class ADONet
		{
			/// <summary>
			/// IsDBNull
			/// </summary>
			/// <summary>
			/// IsDBNull
			/// </summary>
			public static readonly MethodInfo   IsDBNull         = MemberHelper.MethodOf  <DbDataReader>(r => r.IsDBNull(0));
			/// <summary>
			/// IsDBNullAsync
			/// </summary>
			/// <summary>
			/// IsDBNullAsync
			/// </summary>
			public static readonly MethodInfo   IsDBNullAsync    = MemberHelper.MethodOf  <DbDataReader>(r => r.IsDBNullAsync(0));
			/// <summary>
			/// ConnectionString
			/// </summary>
			/// <summary>
			/// ConnectionString
			/// </summary>
			public static readonly PropertyInfo ConnectionString = MemberHelper.PropertyOf<DbConnection>(c => c.ConnectionString);
		}
		/// <summary>
		/// System
		/// </summary>

		public static class System
		{
			/// <summary>
			/// Guid_NewGuid
			/// </summary>
			/// <summary>
			/// Guid_NewGuid
			/// </summary>
			public static readonly MethodInfo Guid_NewGuid     = MemberHelper.MethodOf(     () => Guid.NewGuid());
			/// <summary>
			/// Guid_ToString
			/// </summary>
			/// <summary>
			/// Guid_ToString
			/// </summary>
			public static readonly MethodInfo Guid_ToString    = MemberHelper.MethodOf<Guid>(g => g.ToString());
			/// <summary>
			/// Guid_ToByteArray
			/// </summary>
			/// <summary>
			/// Guid_ToByteArray
			/// </summary>
			public static readonly MethodInfo Guid_ToByteArray = MemberHelper.MethodOf<Guid>(g => g.ToByteArray());
		}
		/// <summary>
		/// Enumerable
		/// </summary>

		public static class Enumerable
		{
			/// <summary>
			/// ToArray
			/// </summary>
			/// <summary>
			/// ToArray
			/// </summary>
			public static readonly MethodInfo ToArray     = MemberHelper.MethodOfGeneric<IEnumerable<int>>(e => e.ToArray());
			/// <summary>
			/// ToList
			/// </summary>
			/// <summary>
			/// ToList
			/// </summary>
			public static readonly MethodInfo ToList      = MemberHelper.MethodOfGeneric<IEnumerable<int>>(e => e.ToList());
			/// <summary>
			/// Select
			/// </summary>

			public static readonly MethodInfo Select      = MemberHelper.MethodOfGeneric<IEnumerable<int>>(e => e.Select(p => p));
			/// <summary>
			/// Where
			/// </summary>
			/// <summary>
			/// Where
			/// </summary>
			public static readonly MethodInfo Where       = MemberHelper.MethodOfGeneric<IEnumerable<int>>(q => q.Where((Func<int, bool>)null!));
			/// <summary>
			/// Take
			/// </summary>
			/// <summary>
			/// Take
			/// </summary>
			public static readonly MethodInfo Take        = MemberHelper.MethodOfGeneric<IEnumerable<int>>(q => q.Take(1));
			/// <summary>
			/// Skip
			/// </summary>
			/// <summary>
			/// Skip
			/// </summary>
			public static readonly MethodInfo Skip        = MemberHelper.MethodOfGeneric<IEnumerable<int>>(q => q.Skip(1));
			/// <summary>
			/// Contains
			/// </summary>
			/// <summary>
			/// Contains
			/// </summary>
			public static readonly MethodInfo Contains    = MemberHelper.MethodOfGeneric<IEnumerable<int>>(q => q.Contains(1));
			/// <summary>
			/// First
			/// </summary>

			public static readonly MethodInfo First                    = MemberHelper.MethodOfGeneric<IEnumerable<int>>(q => q.First());
			/// <summary>
			/// FirstOrDefault
			/// </summary>
			/// <summary>
			/// FirstOrDefault
			/// </summary>
			public static readonly MethodInfo FirstOrDefault           = MemberHelper.MethodOfGeneric<IEnumerable<int>>(q => q.FirstOrDefault());
			/// <summary>
			/// FirstOrDefaultCondition
			/// </summary>
			/// <summary>
			/// FirstOrDefaultCondition
			/// </summary>
			public static readonly MethodInfo FirstOrDefaultCondition  = MemberHelper.MethodOfGeneric<IEnumerable<int>>(q => q.FirstOrDefault(e => true));
			/// <summary>
			/// Single
			/// </summary>

			public static readonly MethodInfo Single                   = MemberHelper.MethodOfGeneric<IEnumerable<int>>(q => q.Single());
			/// <summary>
			/// SingleOrDefault
			/// </summary>
			/// <summary>
			/// SingleOrDefault
			/// </summary>
			public static readonly MethodInfo SingleOrDefault          = MemberHelper.MethodOfGeneric<IEnumerable<int>>(q => q.SingleOrDefault());
			/// <summary>
			/// SingleOrDefaultCondition
			/// </summary>
			/// <summary>
			/// SingleOrDefaultCondition
			/// </summary>
			public static readonly MethodInfo SingleOrDefaultCondition = MemberHelper.MethodOfGeneric<IEnumerable<int>>(q => q.SingleOrDefault(e => true));
			/// <summary>
			/// ElementAt
			/// </summary>

			public static readonly MethodInfo ElementAt               = MemberHelper.MethodOfGeneric<IEnumerable<int>>(q => q.ElementAt(0));
			/// <summary>
			/// ElementAtOrDefault
			/// </summary>
			/// <summary>
			/// ElementAtOrDefault
			/// </summary>
			public static readonly MethodInfo ElementAtOrDefault      = MemberHelper.MethodOfGeneric<IEnumerable<int>>(q => q.ElementAtOrDefault(0));
			/// <summary>
			/// DefaultIfEmpty
			/// </summary>

			public static readonly MethodInfo DefaultIfEmpty       = MemberHelper.MethodOfGeneric<IEnumerable<int>>(q => q.DefaultIfEmpty());
			/// <summary>
			/// DefaultIfEmptyValue
			/// </summary>
			/// <summary>
			/// DefaultIfEmptyValue
			/// </summary>
			public static readonly MethodInfo DefaultIfEmptyValue  = MemberHelper.MethodOfGeneric<IEnumerable<int>>(q => q.DefaultIfEmpty(0));
			/// <summary>
			/// OfType
			/// </summary>

			public static readonly MethodInfo OfType               = MemberHelper.MethodOfGeneric<IEnumerable<int>>(q => q.OfType<int>());
			/// <summary>
			/// SelectManySimple
			/// </summary>

			public static readonly MethodInfo SelectManySimple     = MemberHelper.MethodOfGeneric<IEnumerable<int>>(q => q.SelectMany(a => new int[0]));
			/// <summary>
			/// SelectManyProjection
			/// </summary>
			/// <summary>
			/// SelectManyProjection
			/// </summary>
			public static readonly MethodInfo SelectManyProjection = MemberHelper.MethodOfGeneric<IEnumerable<int>>(q => q.SelectMany(a => new int[0], (m, d) => d));
			/// <summary>
			/// Join
			/// </summary>

			public static readonly MethodInfo Join      = MemberHelper.MethodOfGeneric<IEnumerable<LW1>, IEnumerable<LW2>>((m, d) => m.Join(d, _ => _.Value1, _ => _.Value2, (m, d) => d));
			/// <summary>
			/// GroupJoin
			/// </summary>
			/// <summary>
			/// GroupJoin
			/// </summary>
			public static readonly MethodInfo GroupJoin = MemberHelper.MethodOfGeneric<IEnumerable<LW1>, IEnumerable<LW2>>((m, d) => m.GroupJoin(d, _ => _.Value1, _ => _.Value2, (m, d) => d));
			/// <summary>
			/// Distinct
			/// </summary>

			public static readonly MethodInfo Distinct    = MemberHelper.MethodOfGeneric<IEnumerable<int>>(q => q.Distinct());
		}
		/// <summary>
		/// Queryable
		/// </summary>

		public static class Queryable
		{
			/// <summary>
			/// ToArray
			/// </summary>
			/// <summary>
			/// ToArray
			/// </summary>
			public static readonly MethodInfo ToArray      = MemberHelper.MethodOfGeneric<IQueryable<int>>(e => e.ToArray());
			/// <summary>
			/// ToList
			/// </summary>
			/// <summary>
			/// ToList
			/// </summary>
			public static readonly MethodInfo ToList       = MemberHelper.MethodOfGeneric<IQueryable<int>>(e => e.ToList());
			/// <summary>
			/// AsEnumerable
			/// </summary>
			/// <summary>
			/// AsEnumerable
			/// </summary>
			public static readonly MethodInfo AsEnumerable = MemberHelper.MethodOfGeneric<IQueryable<int>>(e => e.AsEnumerable());
			/// <summary>
			/// AsQueryable
			/// </summary>
			/// <summary>
			/// AsQueryable
			/// </summary>
			public static readonly MethodInfo AsQueryable  = MemberHelper.MethodOfGeneric<IEnumerable<int>>(e => e.AsQueryable());
			/// <summary>
			/// Select
			/// </summary>

			public static readonly MethodInfo Select   = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.Select(p => p));
			/// <summary>
			/// Where
			/// </summary>
			/// <summary>
			/// Where
			/// </summary>
			public static readonly MethodInfo Where    = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.Where((Expression<Func<int, bool>>)null!));
			/// <summary>
			/// Take
			/// </summary>
			/// <summary>
			/// Take
			/// </summary>
			public static readonly MethodInfo Take     = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.Take(1));
			/// <summary>
			/// Skip
			/// </summary>
			/// <summary>
			/// Skip
			/// </summary>
			public static readonly MethodInfo Skip     = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.Skip(1));
			/// <summary>
			/// Contains
			/// </summary>
			/// <summary>
			/// Contains
			/// </summary>
			public static readonly MethodInfo Contains = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.Contains(1));
			/// <summary>
			/// First
			/// </summary>

			public static readonly MethodInfo First                   = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.First());
			/// <summary>
			/// FirstOrDefault
			/// </summary>
			/// <summary>
			/// FirstOrDefault
			/// </summary>
			public static readonly MethodInfo FirstOrDefault          = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.FirstOrDefault());
			/// <summary>
			/// FirstOrDefaultCondition
			/// </summary>
			/// <summary>
			/// FirstOrDefaultCondition
			/// </summary>
			public static readonly MethodInfo FirstOrDefaultCondition = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.FirstOrDefault(e => true));
			/// <summary>
			/// Single
			/// </summary>

			public static readonly MethodInfo Single                   = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.Single());
			/// <summary>
			/// SingleOrDefault
			/// </summary>
			/// <summary>
			/// SingleOrDefault
			/// </summary>
			public static readonly MethodInfo SingleOrDefault          = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.SingleOrDefault());
			/// <summary>
			/// SingleOrDefaultCondition
			/// </summary>
			/// <summary>
			/// SingleOrDefaultCondition
			/// </summary>
			public static readonly MethodInfo SingleOrDefaultCondition = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.SingleOrDefault(e => true));
			/// <summary>
			/// ElementAt
			/// </summary>

			public static readonly MethodInfo ElementAt               = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.ElementAt(0));
			/// <summary>
			/// ElementAtOrDefault
			/// </summary>
			/// <summary>
			/// ElementAtOrDefault
			/// </summary>
			public static readonly MethodInfo ElementAtOrDefault      = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.ElementAtOrDefault(0));
			/// <summary>
			/// ElementAtAsync
			/// </summary>
			/// <summary>
			/// ElementAtAsync
			/// </summary>
			public static readonly MethodInfo ElementAtAsync          = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.ElementAtAsync(() => 0, default));
			/// <summary>
			/// ElementAtOrDefaultAsync
			/// </summary>
			/// <summary>
			/// ElementAtOrDefaultAsync
			/// </summary>
			public static readonly MethodInfo ElementAtOrDefaultAsync = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.ElementAtOrDefaultAsync(() => 0, default));
			/// <summary>
			/// DefaultIfEmpty
			/// </summary>

			public static readonly MethodInfo DefaultIfEmpty      = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.DefaultIfEmpty());
			/// <summary>
			/// DefaultIfEmptyValue
			/// </summary>
			/// <summary>
			/// DefaultIfEmptyValue
			/// </summary>
			public static readonly MethodInfo DefaultIfEmptyValue = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.DefaultIfEmpty(0));
			/// <summary>
			/// OfType
			/// </summary>

			public static readonly MethodInfo OfType               = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.OfType<double>());
			/// <summary>
			/// SelectManySimple
			/// </summary>

			public static readonly MethodInfo SelectManySimple     = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.SelectMany(a => new int[0]));
			/// <summary>
			/// SelectManyProjection
			/// </summary>
			/// <summary>
			/// SelectManyProjection
			/// </summary>
			public static readonly MethodInfo SelectManyProjection = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.SelectMany(a => new int[0], (m, d) => d));
			/// <summary>
			/// Join
			/// </summary>

			public static readonly MethodInfo Join      = MemberHelper.MethodOfGeneric<IQueryable<LW1>, IQueryable<LW2>>((m, d) => m.Join(d, _ => _.Value1, _ => _.Value2, (m,      d) => d));
			/// <summary>
			/// GroupJoin
			/// </summary>
			/// <summary>
			/// GroupJoin
			/// </summary>
			public static readonly MethodInfo GroupJoin = MemberHelper.MethodOfGeneric<IQueryable<LW1>, IQueryable<LW2>>((m, d) => m.GroupJoin(d, _ => _.Value1, _ => _.Value2, (m, d) => d));
			/// <summary>
			/// Distinct
			/// </summary>

			public static readonly MethodInfo Distinct    = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.Distinct());
		}
		/// <summary>
		/// SooQuery
		/// </summary>

		public static class SooQuery
		{
			internal static readonly MethodInfo EvaluateExpression = MemberHelper.MethodOf(() => ExpressionEvaluator.EvaluateExpression(null));
			/// <summary>
			/// useQueryable
			/// </summary>

			public static readonly MethodInfo useQueryable = MemberHelper.MethodOfGeneric<DBInstance>(dc => dc.useQueryable<object>());
			/// <summary>
			/// IncludesAsTable
			/// </summary>

			public static readonly MethodInfo IncludesAsTable       = MemberHelper.MethodOfGeneric<IDbQuery<LW1>>(q => q.IncludesAsTable(e => e.Single2));

			internal static readonly MethodInfo IncludeInternal    = MemberHelper.MethodOfGeneric<IQueryable<LW1>>(q => q.IncludeInternal(null!, null));
			/// <summary>
			/// Includes
			/// </summary>

			public static readonly MethodInfo Includes              = MemberHelper.MethodOfGeneric<IQueryable<LW1>>(q => q.Includes(e => e.Single2));
			/// <summary>
			/// IncludesSingleFilter
			/// </summary>
			/// <summary>
			/// IncludesSingleFilter
			/// </summary>
			public static readonly MethodInfo IncludesSingleFilter  = MemberHelper.MethodOfGeneric<IQueryable<LW1>>(q => q.Includes(e => e.Single2, eq => eq.Where(e => e.Value2 == 1)));
			/// <summary>
			/// IncludesManyFilter
			/// </summary>
			/// <summary>
			/// IncludesManyFilter
			/// </summary>
			public static readonly MethodInfo IncludesManyFilter    = MemberHelper.MethodOfGeneric<IQueryable<LW1>>(q => q.Includes(e => e.Many2,   eq => eq.Where(e => e.Value2 == 1)));
			/// <summary>
			/// ThenIncludeFromSingle
			/// </summary>

			public static readonly MethodInfo ThenIncludeFromSingle             = MemberHelper.MethodOfGeneric<IQueryable<LW1>>(q => q.Includes(e => e.Single2).ThenInclude(e => e.Single3));
			/// <summary>
			/// ThenIncludeFromMany
			/// </summary>
			/// <summary>
			/// ThenIncludeFromMany
			/// </summary>
			public static readonly MethodInfo ThenIncludeFromMany               = MemberHelper.MethodOfGeneric<IQueryable<LW1>>(q => q.Includes(e => e.Many2).ThenInclude(e => e.Single3));
			/// <summary>
			/// ThenIncludeFromSingleSingleFilter
			/// </summary>

			public static readonly MethodInfo ThenIncludeFromSingleSingleFilter = MemberHelper.MethodOfGeneric<IQueryable<LW1>>(q => q.Includes(e => e.Single2).ThenInclude(e => e.Single3, eq => eq.Where(e => e.Value3 == 3)));
			/// <summary>
			/// ThenIncludeFromSingleManyFilter
			/// </summary>
			/// <summary>
			/// ThenIncludeFromSingleManyFilter
			/// </summary>
			public static readonly MethodInfo ThenIncludeFromSingleManyFilter   = MemberHelper.MethodOfGeneric<IQueryable<LW1>>(q => q.Includes(e => e.Single2).ThenInclude(e => e.Many3,   eq => eq.Where(e => e.Value3 == 3)));
			/// <summary>
			/// ThenIncludeFromManySingleFilter
			/// </summary>
			/// <summary>
			/// ThenIncludeFromManySingleFilter
			/// </summary>
			public static readonly MethodInfo ThenIncludeFromManySingleFilter   = MemberHelper.MethodOfGeneric<IQueryable<LW1>>(q => q.Includes(e => e.Many2).ThenInclude(e => e.Single3,   eq => eq.Where(e => e.Value3 == 3)));
			/// <summary>
			/// ThenIncludeFromManyManyFilter
			/// </summary>
			/// <summary>
			/// ThenIncludeFromManyManyFilter
			/// </summary>
			public static readonly MethodInfo ThenIncludeFromManyManyFilter     = MemberHelper.MethodOfGeneric<IQueryable<LW1>>(q => q.Includes(e => e.Many2).ThenInclude(e => e.Many3,     eq => eq.Where(e => e.Value3 == 3)));
			/// <summary>
			/// ElementAtLambda
			/// </summary>

			public static readonly MethodInfo ElementAtLambda = MemberHelper.MethodOfGeneric<IQueryable<LW1>>(q => q.ElementAt(() => 1));
			/// <summary>
			/// SkipLambda
			/// </summary>
			/// <summary>
			/// SkipLambda
			/// </summary>
			public static readonly MethodInfo SkipLambda      = MemberHelper.MethodOfGeneric<IQueryable<LW1>>(q => q.Skip(() => 1));
			/// <summary>
			/// RemoveOrderBy
			/// </summary>

			public static readonly MethodInfo RemoveOrderBy       = MemberHelper.MethodOfGeneric<IQueryable<object>>(q => q.RemoveOrderBy());
			/// <summary>
			/// IgnoreFilters
			/// </summary>
			/// <summary>
			/// IgnoreFilters
			/// </summary>
			public static readonly MethodInfo IgnoreFilters       = MemberHelper.MethodOfGeneric<IQueryable<object>>(q => q.IgnoreFilters());
			/// <summary>
			/// DisableGuard
			/// </summary>
			/// <summary>
			/// DisableGuard
			/// </summary>
			public static readonly MethodInfo DisableGuard        = MemberHelper.MethodOfGeneric<IQueryable<IGrouping<int, object>>>(q => q.DisableGuard());
			/// <summary>
			/// InlineParameters
			/// </summary>
			/// <summary>
			/// InlineParameters
			/// </summary>
			public static readonly MethodInfo InlineParameters    = MemberHelper.MethodOfGeneric<IQueryable<object>>(q => q.InlineParameters());

			internal static readonly MethodInfo SelectDistinct    = MemberHelper.MethodOfGeneric<IQueryable<IGrouping<int, object>>>(q => q.SelectDistinct());
			/// <summary>
			/// AsQueryable
			/// </summary>

			public static readonly MethodInfo AsQueryable         = MemberHelper.MethodOfGeneric<IQueryable<object>>(q => q.AsQueryable(null!));
			/// <summary>
			/// AsSubQuery
			/// </summary>
			/// <summary>
			/// AsSubQuery
			/// </summary>
			public static readonly MethodInfo AsSubQuery          = MemberHelper.MethodOfGeneric<IQueryable<object>>(q => q.AsSubQuery());
			/// <summary>
			/// TagQuery
			/// </summary>

			public static readonly MethodInfo TagQuery            = MemberHelper.MethodOfGeneric<IQueryable<object>>(q => q.TagQuery(string.Empty));
			/// <summary>
			/// JoinTypePredicateSelector
			/// </summary>

			public static readonly MethodInfo JoinTypePredicateSelector = MemberHelper.MethodOfGeneric((IQueryable<object> q1, IQueryable<object> q2, Expression<Func<object, object, bool>> p, Expression<Func<object, object, object>> r) => q1.Join(q2, default(SqlJoinType), p, r));
			/// <summary>
			/// GroupBy
			/// </summary>

			public static class GroupBy
			{
				/// <summary>
				/// Rollup
				/// </summary>
				/// <summary>
				/// Rollup
				/// </summary>
				public static readonly MethodInfo Rollup       = MemberHelper.MethodOfGeneric<linq.SooFunctionExtension.IGroupBy>(g => g.Rollup<object>(null!));
				/// <summary>
				/// Cube
				/// </summary>
				/// <summary>
				/// Cube
				/// </summary>
				public static readonly MethodInfo Cube         = MemberHelper.MethodOfGeneric<linq.SooFunctionExtension.IGroupBy>(g => g.Cube<object>(null!));
				/// <summary>
				/// GroupingSets
				/// </summary>
				/// <summary>
				/// GroupingSets
				/// </summary>
				public static readonly MethodInfo GroupingSets = MemberHelper.MethodOfGeneric<linq.SooFunctionExtension.IGroupBy>(g => g.GroupingSets<object>(null!));
				/// <summary>
				/// Grouping
				/// </summary>

				public static readonly MethodInfo Grouping     = MemberHelper.MethodOf(() => linq.SooFunctionExtension.Grouping(null!));
			}
			/// <summary>
			/// SqlExt
			/// </summary>

			public static class SqlExt
			{
				/// <summary>
				/// ToNotNull
				/// </summary>
				/// <summary>
				/// ToNotNull
				/// </summary>
				public static readonly MethodInfo ToNotNull        = MemberHelper.MethodOfGeneric<int?>(i => linq.DbFunc.ToNotNull(i));
				/// <summary>
				/// ToNotNullable
				/// </summary>
				/// <summary>
				/// ToNotNullable
				/// </summary>
				public static readonly MethodInfo ToNotNullable    = MemberHelper.MethodOfGeneric<int?>(i => linq.DbFunc.ToNotNullable(i));
				/// <summary>
				/// Alias
				/// </summary>
				/// <summary>
				/// Alias
				/// </summary>
				public static readonly MethodInfo Alias            = MemberHelper.MethodOfGeneric<int?>(i => linq.DbFunc.Alias(i, ""));
				// don't use MethodOfGeneric here (DbFunc.Property treatened in special way by it)
				/// <summary>
				/// Property
				/// </summary>
				/// <summary>
				/// Property
				/// </summary>
				public static readonly MethodInfo Property         = typeof(linq.DbFunc).GetMethodEx(nameof(linq.DbFunc.Property))!.GetGenericMethodDefinition();
			}
			/// <summary>
			/// Applies pending changes for the edit table.
			/// </summary>

			public static class Update
			{
				/// <summary>
				/// AsUpdatable
				/// </summary>
				/// <summary>
				/// AsUpdatable
				/// </summary>
				public static readonly MethodInfo AsUpdatable            = MemberHelper.MethodOfGeneric<IQueryable<int>>(q => q.AsUpdatable());
				/// <summary>
				/// SetQueryableSetCustom
				/// </summary>
				/// <summary>
				/// SetQueryableSetCustom
				/// </summary>
				public static readonly MethodInfo SetQueryableSetCustom  = MemberHelper.MethodOfGeneric<IQueryable<LW1>>(q => q.Set(e => string.Empty));
				/// <summary>
				/// SetQueryableExpression
				/// </summary>
				/// <summary>
				/// SetQueryableExpression
				/// </summary>
				public static readonly MethodInfo SetQueryableExpression = MemberHelper.MethodOfGeneric<IQueryable<LW1>>(q => q.Set(e => e.Value1, () => 1));
				/// <summary>
				/// SetQueryablePrev
				/// </summary>
				/// <summary>
				/// SetQueryablePrev
				/// </summary>
				public static readonly MethodInfo SetQueryablePrev       = MemberHelper.MethodOfGeneric<IQueryable<LW1>>(q => q.Set(e => e.Value1, prev => 1));
				/// <summary>
				/// SetUpdatableSetCustom
				/// </summary>
				/// <summary>
				/// SetUpdatableSetCustom
				/// </summary>
				public static readonly MethodInfo SetUpdatableSetCustom  = MemberHelper.MethodOfGeneric<IUpdatable<LW1>>(q => q.Set(e => string.Empty));
				/// <summary>
				/// SetUpdatableExpression
				/// </summary>
				/// <summary>
				/// SetUpdatableExpression
				/// </summary>
				public static readonly MethodInfo SetUpdatableExpression = MemberHelper.MethodOfGeneric<IUpdatable<LW1>>(q => q.Set(e => e.Value1, () => 1));
				/// <summary>
				/// SetUpdatablePrev
				/// </summary>
				/// <summary>
				/// SetUpdatablePrev
				/// </summary>
				public static readonly MethodInfo SetUpdatablePrev       = MemberHelper.MethodOfGeneric<IUpdatable<LW1>>(q => q.Set(e => e.Value1, prev => 1));
				/// <summary>
				/// SetUpdatableValue
				/// </summary>
				/// <summary>
				/// SetUpdatableValue
				/// </summary>
				public static readonly MethodInfo SetUpdatableValue      = MemberHelper.MethodOfGeneric<IUpdatable<LW1>>(q => q.Set(e => e.Value1, 1));
			}
			/// <summary>
			/// Insert
			/// </summary>

			public static class Insert
			{
				/// <summary>
				/// SI
				/// </summary>
				/// <summary>
				/// SI
				/// </summary>
				public static class SI
				{
					/// <summary>
					/// Value
					/// </summary>
					/// <summary>
					/// Value
					/// </summary>
					public static readonly MethodInfo Value                 = MemberHelper.MethodOfGeneric<ISelectInsertable<LW1, LW2>>(q => q.Value(e => e.Value2, 1));
					/// <summary>
					/// ValueExpression
					/// </summary>
					/// <summary>
					/// ValueExpression
					/// </summary>
					public static readonly MethodInfo ValueExpression       = MemberHelper.MethodOfGeneric<ISelectInsertable<LW1, LW2>>(q => q.Value(e => e.Value2, () => 1));
					/// <summary>
					/// ValueSourceExpression
					/// </summary>
					/// <summary>
					/// ValueSourceExpression
					/// </summary>
					public static readonly MethodInfo ValueSourceExpression = MemberHelper.MethodOfGeneric<ISelectInsertable<LW1, LW2>>(q => q.Value(t => t.Value2, s => s.Value1));
					/// <summary>
					/// Insert
					/// </summary>

					public static readonly MethodInfo Insert      = MemberHelper.MethodOfGeneric<ISelectInsertable<LW1, LW2>>(q => q.Insert());
					/// <summary>
					/// InsertAsync
					/// </summary>
					/// <summary>
					/// InsertAsync
					/// </summary>
					public static readonly MethodInfo InsertAsync = MemberHelper.MethodOfGeneric<ISelectInsertable<LW1, LW2>>(q => q.InsertAsync(default));
					/// <summary>
					/// InsertWithIdentity
					/// </summary>

					public static readonly MethodInfo InsertWithIdentity = MemberHelper.MethodOfGeneric<ISelectInsertable<LW1, LW2>>(i => i.InsertWithIdentity());
				}
			}
			/// <summary>
			/// Merge
			/// </summary>

			public static class Merge
			{
				/// <summary>
				/// MergeMethodInfo1
				/// </summary>
				/// <summary>
				/// MergeMethodInfo1
				/// </summary>
				public static readonly MethodInfo MergeMethodInfo1                          = MemberHelper.MethodOfGeneric<IDbQuery<LW1>>(q => q.Merge(string.Empty));
				/// <summary>
				/// MergeMethodInfo2
				/// </summary>
				/// <summary>
				/// MergeMethodInfo2
				/// </summary>
				public static readonly MethodInfo MergeMethodInfo2                          = MemberHelper.MethodOfGeneric<IQueryable<LW1>>(q => q.Merge());
				/// <summary>
				/// MergeIntoMethodInfo1
				/// </summary>
				/// <summary>
				/// MergeIntoMethodInfo1
				/// </summary>
				public static readonly MethodInfo MergeIntoMethodInfo1                      = MemberHelper.MethodOfGeneric((IQueryable<LW1> q, IDbQuery<LW2> t) => q.MergeInto(t, string.Empty));
				/// <summary>
				/// MergeIntoMethodInfo2
				/// </summary>
				/// <summary>
				/// MergeIntoMethodInfo2
				/// </summary>
				public static readonly MethodInfo MergeIntoMethodInfo2                      = MemberHelper.MethodOfGeneric((IQueryable<LW1> q, IQueryable<LW2> t) => q.MergeInto(t));
				/// <summary>
				/// UsingMethodInfo1
				/// </summary>
				/// <summary>
				/// UsingMethodInfo1
				/// </summary>
				public static readonly MethodInfo UsingMethodInfo1                          = MemberHelper.MethodOfGeneric((IMergeableUsing<LW1> q, IQueryable<LW2> t) => q.Using(t));
				/// <summary>
				/// UsingMethodInfo2
				/// </summary>
				/// <summary>
				/// UsingMethodInfo2
				/// </summary>
				public static readonly MethodInfo UsingMethodInfo2                          = MemberHelper.MethodOfGeneric((IMergeableUsing<LW1> q, IEnumerable<LW2> t) => q.Using(t));
				/// <summary>
				/// UsingTargetMethodInfo
				/// </summary>
				/// <summary>
				/// UsingTargetMethodInfo
				/// </summary>
				public static readonly MethodInfo UsingTargetMethodInfo                     = MemberHelper.MethodOfGeneric<IMergeableUsing<LW1>>(q => q.UsingTarget());
				/// <summary>
				/// OnMethodInfo1
				/// </summary>
				/// <summary>
				/// OnMethodInfo1
				/// </summary>
				public static readonly MethodInfo OnMethodInfo1                             = MemberHelper.MethodOfGeneric((IMergeableOn<LW1, LW2> q, Expression<Func<LW1, int>> p, Expression<Func<LW2, int>> s) => q.On(p, s));
				/// <summary>
				/// OnMethodInfo2
				/// </summary>
				/// <summary>
				/// OnMethodInfo2
				/// </summary>
				public static readonly MethodInfo OnMethodInfo2                             = MemberHelper.MethodOfGeneric((IMergeableOn<LW1, LW2> q, Expression<Func<LW1, LW2, bool>> p) => q.On(p));
				/// <summary>
				/// OnTargetKeyMethodInfo
				/// </summary>
				/// <summary>
				/// OnTargetKeyMethodInfo
				/// </summary>
				public static readonly MethodInfo OnTargetKeyMethodInfo                     = MemberHelper.MethodOfGeneric<IMergeableOn<LW1, LW1>>(q => q.OnTargetKey());
				/// <summary>
				/// InsertWhenNotMatchedAndMethodInfo
				/// </summary>
				/// <summary>
				/// InsertWhenNotMatchedAndMethodInfo
				/// </summary>
				public static readonly MethodInfo InsertWhenNotMatchedAndMethodInfo         = MemberHelper.MethodOfGeneric((IMergeable<LW1, LW2> q, Expression<Func<LW2, bool>> p, Expression<Func<LW2, LW1>> s) => q.InsertWhenNotMatchedAnd(p, s));
				/// <summary>
				/// UpdateWhenMatchedAndMethodInfo
				/// </summary>
				/// <summary>
				/// UpdateWhenMatchedAndMethodInfo
				/// </summary>
				public static readonly MethodInfo UpdateWhenMatchedAndMethodInfo            = MemberHelper.MethodOfGeneric((IMergeable<LW1, LW2> q, Expression<Func<LW1, LW2, bool>> p, Expression<Func<LW1, LW2, LW1>> s) => q.UpdateWhenMatchedAnd(p, s));
				/// <summary>
				/// UpdateWhenMatchedAndThenDeleteMethodInfo
				/// </summary>
				/// <summary>
				/// UpdateWhenMatchedAndThenDeleteMethodInfo
				/// </summary>
				public static readonly MethodInfo UpdateWhenMatchedAndThenDeleteMethodInfo  = MemberHelper.MethodOfGeneric((IMergeable<LW1, LW2> q, Expression<Func<LW1, LW2, bool>> p, Expression<Func<LW1, LW2, LW1>> s, Expression<Func<LW1, LW2, bool>> d) => q.UpdateWhenMatchedAndThenDelete(p, s, d));
				/// <summary>
				/// DeleteWhenMatchedAndMethodInfo
				/// </summary>
				/// <summary>
				/// DeleteWhenMatchedAndMethodInfo
				/// </summary>
				public static readonly MethodInfo DeleteWhenMatchedAndMethodInfo            = MemberHelper.MethodOfGeneric((IMergeable<LW1, LW2> q, Expression<Func<LW1, LW2, bool>> p) => q.DeleteWhenMatchedAnd(p));
				/// <summary>
				/// UpdateWhenNotMatchedBySourceAndMethodInfo
				/// </summary>
				/// <summary>
				/// UpdateWhenNotMatchedBySourceAndMethodInfo
				/// </summary>
				public static readonly MethodInfo UpdateWhenNotMatchedBySourceAndMethodInfo = MemberHelper.MethodOfGeneric((IMergeable<LW1, LW2> q, Expression<Func<LW1, bool>> p, Expression<Func<LW1, LW1>> s) => q.UpdateWhenNotMatchedBySourceAnd(p, s));
				/// <summary>
				/// DeleteWhenNotMatchedBySourceAndMethodInfo
				/// </summary>
				/// <summary>
				/// DeleteWhenNotMatchedBySourceAndMethodInfo
				/// </summary>
				public static readonly MethodInfo DeleteWhenNotMatchedBySourceAndMethodInfo = MemberHelper.MethodOfGeneric((IMergeable<LW1, LW2> q, Expression<Func<LW1, bool>> p) => q.DeleteWhenNotMatchedBySourceAnd(p));
				/// <summary>
				/// ExecuteMergeMethodInfo
				/// </summary>

				public static readonly MethodInfo ExecuteMergeMethodInfo    = MemberHelper.MethodOfGeneric<IMergeable<LW1, LW2>>(q => q.Merge());
				/// <summary>
				/// MergeWithOutput
				/// </summary>
				/// <summary>
				/// MergeWithOutput
				/// </summary>
				public static readonly MethodInfo MergeWithOutput           = MemberHelper.MethodOfGeneric<IMergeable<LW1, LW2>>(q => q.MergeWithOutput((_, _, _) => 0));
				/// <summary>
				/// MergeWithOutputSource
				/// </summary>
				/// <summary>
				/// MergeWithOutputSource
				/// </summary>
				public static readonly MethodInfo MergeWithOutputSource     = MemberHelper.MethodOfGeneric<IMergeable<LW1, LW2>>(q => q.MergeWithOutput((_, _, _, _) => 0));
				/// <summary>
				/// MergeWithOutputInto
				/// </summary>
				/// <summary>
				/// MergeWithOutputInto
				/// </summary>
				public static readonly MethodInfo MergeWithOutputInto       = MemberHelper.MethodOfGeneric<IMergeable<LW1, LW2>>(q => q.MergeWithOutputInto(null!, (_, _, _) => 0));
				/// <summary>
				/// MergeWithOutputIntoSource
				/// </summary>
				/// <summary>
				/// MergeWithOutputIntoSource
				/// </summary>
				public static readonly MethodInfo MergeWithOutputIntoSource = MemberHelper.MethodOfGeneric<IMergeable<LW1, LW2>>(q => q.MergeWithOutputInto(null!, (_, _, _,_) => 0));
			}
			/// <summary>
			/// MultiInsert
			/// </summary>

			public static class MultiInsert
			{
				// Sadly member names must be different from their englobing type, this actually is `.MultiInsert()`
				/// <summary>
				/// Begin
				/// </summary>
				/// <summary>
				/// Begin
				/// </summary>
				public static readonly MethodInfo Begin       = typeof(MultiInsertExtensions).GetMethod(nameof(MultiInsertExtensions.MultiInsert))!;
				/// <summary>
				/// Into
				/// </summary>
				/// <summary>
				/// Into
				/// </summary>
				public static readonly MethodInfo Into        = typeof(MultiInsertExtensions).GetMethod(nameof(MultiInsertExtensions.Into))!;
				/// <summary>
				/// When
				/// </summary>
				/// <summary>
				/// When
				/// </summary>
				public static readonly MethodInfo When        = typeof(MultiInsertExtensions).GetMethod(nameof(MultiInsertExtensions.When))!;
				/// <summary>
				/// Else
				/// </summary>
				/// <summary>
				/// Else
				/// </summary>
				public static readonly MethodInfo Else        = typeof(MultiInsertExtensions).GetMethod(nameof(MultiInsertExtensions.Else))!;
				/// <summary>
				/// Insert
				/// </summary>
				/// <summary>
				/// Insert
				/// </summary>
				public static readonly MethodInfo Insert      = typeof(MultiInsertExtensions).GetMethod(nameof(MultiInsertExtensions.Insert))!;
				/// <summary>
				/// InsertAll
				/// </summary>
				/// <summary>
				/// InsertAll
				/// </summary>
				public static readonly MethodInfo InsertAll   = typeof(MultiInsertExtensions).GetMethod(nameof(MultiInsertExtensions.InsertAll))!;
				/// <summary>
				/// InsertFirst
				/// </summary>
				/// <summary>
				/// InsertFirst
				/// </summary>
				public static readonly MethodInfo InsertFirst = typeof(MultiInsertExtensions).GetMethod(nameof(MultiInsertExtensions.InsertFirst))!;
			}
			/// <summary>
			/// DataParameter
			/// </summary>

			public static class DataParameter
			{
				/// <summary>
				/// DbDataType
				/// </summary>
				/// <summary>
				/// DbDataType
				/// </summary>
				public static readonly PropertyInfo DbDataType = MemberHelper.PropertyOf<global::mooSQL.linq.utils.DataParameter>(dp => dp.DbDataType);
				/// <summary>
				/// Value
				/// </summary>
				/// <summary>
				/// Value
				/// </summary>
				public static readonly PropertyInfo Value      = MemberHelper.PropertyOf<global::mooSQL.linq.utils.DataParameter>(dp => dp.Value);
			}



		}

		#region Method definition helper classes

		#pragma warning disable 649

		abstract class LW1
		{
			public int   Value1;

			public LW2   Single2 = null!;
			public LW2[] Many2   = null!;
		}

		abstract class LW2
		{
			public int   Value2;

			public LW3   Single3 = null!;
			public LW3[] Many3   = null!;

		}

		abstract class LW3
		{
			public int Value3;
		}

		#pragma warning restore 649

		#endregion

	}
}
