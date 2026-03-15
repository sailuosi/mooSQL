using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace mooSQL.linq.SqlQuery
{
	using Common.Internal;

	using mooSQL.data.model;

	using Visitors;

	public static class QueryVisitorExtensions
	{
		internal static readonly ObjectPool<SqlQueryFindVisitor>          FindVisitorPool      = new(() => new SqlQueryFindVisitor(),          v => v.Cleanup(), 100);
		internal static readonly ObjectPool<SqlQueryActionVisitor>        ActionVisitorPool    = new(() => new SqlQueryActionVisitor(),        v => v.Cleanup(), 100);
		internal static readonly ObjectPool<SqlQueryParentFirstVisitor>   ParentVisitorPool    = new(() => new SqlQueryParentFirstVisitor(),   v => v.Cleanup(), 100);
		internal static readonly ObjectPool<SqlQueryCloneVisitor>         CloneVisitorPool     = new(() => new SqlQueryCloneVisitor(),         v => v.Cleanup(), 100);
		internal static readonly ObjectPool<QueryElementReplacingVisitor> ReplacingVisitorPool = new(() => new QueryElementReplacingVisitor(), v => v.Cleanup(), 100);

		static class PoolHolder<TContext>
		{
			public static readonly ObjectPool<SqlQueryFindVisitor<TContext>>        FindPool        = new(() => new SqlQueryFindVisitor<TContext>(),         v => v.Cleanup(), 100);
			public static readonly ObjectPool<SqlQueryFindExceptVisitor<TContext>>  FindExceptPool  = new(() => new SqlQueryFindExceptVisitor<TContext>(),   v => v.Cleanup(), 100);
			public static readonly ObjectPool<SqlQueryActionVisitor<TContext>>      ActionPool      = new(() => new SqlQueryActionVisitor<TContext>(),       v => v.Cleanup(), 100);
			public static readonly ObjectPool<SqlQueryParentFirstVisitor<TContext>> ParentFirstPool = new(() => new SqlQueryParentFirstVisitor<TContext>(),  v => v.Cleanup(), 100);
			public static readonly ObjectPool<SqlQueryCloneVisitor<TContext>>       ClonePool       = new(() => new SqlQueryCloneVisitor<TContext>(),        v => v.Cleanup(), 100);
			public static readonly ObjectPool<SqlQueryConvertVisitor<TContext>>     ConvertPool     = new(() => new SqlQueryConvertVisitor<TContext>(false), v => v.Cleanup(), 100);
			public static readonly ObjectPool<SqlQueryConvertVisitor<TContext>>     ConvertMutationPool = new(() => new SqlQueryConvertVisitor<TContext>(true), v => v.Cleanup(), 100);
		}

		#region Visit
		public static void Visit<TContext>(this Clause element, TContext context, Action<TContext, ISQLNode> action)
		{
			using var actionVisitor = PoolHolder<TContext>.ActionPool.Allocate();
			actionVisitor.Value.Visit(context, element, false, action);
		}

		public static void Visit(this Clause element, Action<ISQLNode> action)
		{
			using var actionVisitor = ActionVisitorPool.Allocate();
			actionVisitor.Value.Visit(element, false, action);
		}

		public static void VisitAll<TContext>(this Clause element, TContext context, Action<TContext, ISQLNode> action)
		{
			using var actionVisitor = PoolHolder<TContext>.ActionPool.Allocate();
			actionVisitor.Value.Visit(context, element, true, action);
		}

		public static void VisitAll(this Clause element, Action<ISQLNode> action)
		{
			using var actionVisitor = ActionVisitorPool.Allocate();
			actionVisitor.Value.Visit(element, true, action);
		}

		#endregion

		#region VisitParent
		public static void VisitParentFirst<TContext>(this ISQLNode element, TContext context, Func<TContext, ISQLNode, bool> action)
		{
			using var actionVisitor = PoolHolder<TContext>.ParentFirstPool.Allocate();
			actionVisitor.Value.Visit(context, element, false, action);
		}

		public static void VisitParentFirst(this ISQLNode element, Func<ISQLNode, bool> action)
		{
			using var actionVisitor = ParentVisitorPool.Allocate();
			actionVisitor.Value.Visit(element as Clause, false, action);
		}

		public static void VisitParentFirstAll<TContext>(this ISQLNode element, TContext context, Func<TContext, ISQLNode, bool> action)
		{
			using var actionVisitor = PoolHolder<TContext>.ParentFirstPool.Allocate();
			actionVisitor.Value.Visit(context, element, true, action);
		}

		public static void VisitParentFirstAll(this Clause element, Func<ISQLNode, bool> action)
		{
			using var actionVisitor = ParentVisitorPool.Allocate();
			actionVisitor.Value.Visit(element, true, action);
		}

		#endregion

		#region Find
		public static ISQLNode? Find<TContext>(this ISQLNode? element, TContext context, Func<TContext, ISQLNode, bool> find)
		{
			if (element == null)
				return null;

			using var findVisitor = PoolHolder<TContext>.FindPool.Allocate();
			return findVisitor.Value.Find(context, element, find);
		}

		public static ISQLNode? Find(this ISQLNode? element, Func<ISQLNode, bool> find)
		{
			if (element == null)
				return null;

			using var findVisitor = FindVisitorPool.Allocate();
			return findVisitor.Value.Find(element, find);
		}

		public static ISQLNode? Find(this ISQLNode? element, ClauseType type)
		{
			if (element == null)
				return null;

			using var findVisitor = PoolHolder<ClauseType>.FindPool.Allocate();
			return findVisitor.Value.Find(type, element, static (type, e) => e.NodeType == type);
		}
		#endregion

		#region FindExcept
		public static ISQLNode? FindExcept<TContext>(this ISQLNode? element, TContext context, ISQLNode skip, Func<TContext, ISQLNode, bool> find)
		{
			if (element == null)
				return null;

			using var findVisitor = PoolHolder<TContext>.FindExceptPool.Allocate();
			return findVisitor.Value.Find(context, element, skip, find);
		}
		#endregion

		#region Clone
		[return: NotNullIfNotNull(nameof(element))]
		public static T? Clone<T>(this T? element, Dictionary<Clause, Clause> objectTree)
			where T : Clause
		{
			if (element == null)
				return null;

			using var cloneVisitor = CloneVisitorPool.Allocate();
			cloneVisitor.Value.RegisterReplacements(objectTree);

			var clone = (T)cloneVisitor.Value.PerformClone(element);

			cloneVisitor.Value.GetReplacements(objectTree);

			return clone;
		}

		[return: NotNullIfNotNull(nameof(element))]
		public static T? Clone<T, TContext>(this T? element, TContext context, Dictionary<Clause, Clause> objectTree, Func<TContext, ISQLNode, bool> doClone)
			where T : Clause
		{
			if (element == null)
				return null;

			using var cloneVisitor = PoolHolder<TContext>.ClonePool.Allocate();
			cloneVisitor.Value.RegisterReplacements(objectTree);

			var clone = (T)cloneVisitor.Value.Clone(element, context, doClone);

			cloneVisitor.Value.GetReplacements(objectTree);

			return clone;
		}

		[return: NotNullIfNotNull(nameof(element))]
		public static T? Clone<T, TContext>(this T? element, TContext context, Func<TContext, Clause, bool> doClone)
			where T : Clause
		{
			if (element == null)
				return null;

			using var cloneVisitor = PoolHolder<TContext>.ClonePool.Allocate();

			return (T)cloneVisitor.Value.Clone(element, context, doClone);
		}

		[return: NotNullIfNotNull(nameof(element))]
		public static T? Clone<T>(this T? element, Func<Clause, bool> doClone)
			where T : Clause
		{
			if (element == null)
				return null;

			using var cloneVisitor = CloneVisitorPool.Allocate();

			return (T)cloneVisitor.Value.Clone(element, doClone);
		}

		[return: NotNullIfNotNull(nameof(element))]
		public static T? Clone<T>(this T? element)
			where T : Clause
		{
			if (element == null)
				return null;

			using var cloneVisitor = CloneVisitorPool.Allocate();

			return (T)cloneVisitor.Value.Clone(element, null);
		}
		#endregion

		#region Replace

		public static T Replace<T>(this T element, IDictionary<Clause, Clause> replacements,
			params Clause[]   toIgnore)
			where T : Clause
		{
			using var replacingElementVisitor = ReplacingVisitorPool.Allocate();

			return (T)replacingElementVisitor.Value.Replace(element, replacements, toIgnore);
		}

		#endregion

		#region Convert

		public static T Convert<TContext, T>(this T element, TContext context, Func<SqlQueryConvertVisitor<TContext>, Clause, Clause> convertAction, bool withStack)
			where T : Clause
		{
			using var convertVisitor = PoolHolder<TContext>.ConvertPool.Allocate();

			return (T?)convertVisitor.Value.Convert(element, context, convertAction, withStack) ?? element;
		}

		public static T Convert<TContext, T>(this T element, TContext context, Func<SqlQueryConvertVisitor<TContext>, Clause, Clause> convertAction)
			where T : Clause
		{
			using var convertVisitor = PoolHolder<TContext>.ConvertPool.Allocate();

			return (T?)convertVisitor.Value.Convert(element, context, convertAction, false) ?? element;
		}

		public static T Convert<T>(this T element, Func<SqlQueryConvertVisitor<object?>, Clause, Clause> convertAction, bool withStack)
			where T : Clause
		{
			if (withStack)
				throw new NotImplementedException();

			using var convertVisitor = PoolHolder<object?>.ConvertPool.Allocate();

			return (T?)convertVisitor.Value.Convert(element, null, convertAction, false) ?? element;
		}

		public static T Convert<T>(this T element, Func<SqlQueryConvertVisitor<object?>, Clause, Clause> convertAction)
			where T : Clause
		{
			using var convertVisitor = PoolHolder<object?>.ConvertPool.Allocate();

			return (T?)convertVisitor.Value.Convert(element, null, convertAction, false) ?? element;
		}

		public static T Convert<TContext, T>(this T element, TContext context, bool allowMutation, Func<SqlQueryConvertVisitor<TContext>, Clause, Clause> convertAction, bool withStack)
			where T : Clause
		{
			using var convertVisitor = PoolHolder<TContext>.ConvertPool.Allocate();

			return (T?)convertVisitor.Value.Convert(element, context, convertAction, withStack) ?? element;
		}

		public static T Convert<TContext, T>(this T element, TContext context, bool allowMutation, Func<SqlQueryConvertVisitor<TContext>, Clause, Clause> convertAction)
			where T : Clause
		{
			using var convertVisitor = allowMutation
				? PoolHolder<TContext>.ConvertMutationPool.Allocate()
				: PoolHolder<TContext>.ConvertPool.Allocate();

			return (T?)convertVisitor.Value.Convert(element, context, convertAction, false) ?? element;
		}

		public static T ConvertAll<TContext, T>(this T element, TContext context, Func<SqlQueryConvertVisitor<TContext>, Clause, Clause> convertAction)
			where T : Clause
		{
			using var convertVisitor = PoolHolder<TContext>.ConvertPool.Allocate();

			return (T?)convertVisitor.Value.Convert(element, context, convertAction, false) ?? element;
		}

		public static T ConvertAll<TContext, T>(this T element, TContext context, bool allowMutation, Func<SqlQueryConvertVisitor<TContext>, Clause, Clause> convertAction)
			where T : Clause
		{
			using var convertVisitor = allowMutation
				? PoolHolder<TContext>.ConvertMutationPool.Allocate()
				: PoolHolder<TContext>.ConvertPool.Allocate();

			return (T?)convertVisitor.Value.Convert(element, context, convertAction, false) ?? element;
		}

		public static T ConvertAll<T>(this T element, bool allowMutation, Func<SqlQueryConvertVisitor<object?>, Clause, Clause> convertAction)
			where T : Clause
        {
			using var convertVisitor = allowMutation
				? PoolHolder<object?>.ConvertMutationPool.Allocate()
				: PoolHolder<object?>.ConvertPool.Allocate();

			return (T?)convertVisitor.Value.Convert(element, null, convertAction, false) ?? element;
		}
		#endregion
	}
}
