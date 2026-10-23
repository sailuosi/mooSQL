using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace mooSQL.linq.clause
{
	using mooSQL.linq.utils;

	using mooSQL.data.model;
	/// <summary>
	/// QueryVisitorExtensions type.
	/// </summary>

	public static class QueryVisitorExtensions
	{
		internal static readonly ObjectPool<ClauseFindVisitor>          FindVisitorPool      = new(() => new ClauseFindVisitor(),          v => v.Cleanup(), 100);
		internal static readonly ObjectPool<ClauseActionVisitor>        ActionVisitorPool    = new(() => new ClauseActionVisitor(),        v => v.Cleanup(), 100);
		internal static readonly ObjectPool<ClauseParentFirstVisitor>   ParentVisitorPool    = new(() => new ClauseParentFirstVisitor(),   v => v.Cleanup(), 100);
		internal static readonly ObjectPool<SentenceCloneVisitor>         CloneVisitorPool     = new(() => new SentenceCloneVisitor(),         v => v.Cleanup(), 100);
		internal static readonly ObjectPool<ClauseReplacingVisitor> ReplacingVisitorPool = new(() => new ClauseReplacingVisitor(), v => v.Cleanup(), 100);

		static class PoolHolder<TContext>
		{
			public static readonly ObjectPool<ClauseFindVisitor<TContext>>        FindPool        = new(() => new ClauseFindVisitor<TContext>(),         v => v.Cleanup(), 100);
			public static readonly ObjectPool<ClauseFindExceptVisitor<TContext>>  FindExceptPool  = new(() => new ClauseFindExceptVisitor<TContext>(),   v => v.Cleanup(), 100);
			public static readonly ObjectPool<ClauseActionVisitor<TContext>>      ActionPool      = new(() => new ClauseActionVisitor<TContext>(),       v => v.Cleanup(), 100);
			public static readonly ObjectPool<ClauseParentFirstVisitor<TContext>> ParentFirstPool = new(() => new ClauseParentFirstVisitor<TContext>(),  v => v.Cleanup(), 100);
			public static readonly ObjectPool<SentenceCloneVisitor<TContext>>       ClonePool       = new(() => new SentenceCloneVisitor<TContext>(),        v => v.Cleanup(), 100);
			public static readonly ObjectPool<SqlQueryConvertVisitor<TContext>>     ConvertPool     = new(() => new SqlQueryConvertVisitor<TContext>(false), v => v.Cleanup(), 100);
			public static readonly ObjectPool<SqlQueryConvertVisitor<TContext>>     ConvertMutationPool = new(() => new SqlQueryConvertVisitor<TContext>(true), v => v.Cleanup(), 100);
		}

		#region Visit
		/// <summary>
		/// Visit&lt;TContext&gt;
		/// </summary>
		/// <summary>
		/// Visit&lt;TContext&gt;
		/// </summary>
		public static void Visit<TContext>(this Clause element, TContext context, Action<TContext, ISQLNode> action)
		{
			using var actionVisitor = PoolHolder<TContext>.ActionPool.Allocate();
			actionVisitor.Value.Visit(context, element, false, action);
		}
		/// <summary>
		/// Visit
		/// </summary>

		public static void Visit(this Clause element, Action<ISQLNode> action)
		{
			using var actionVisitor = ActionVisitorPool.Allocate();
			actionVisitor.Value.Visit(element, false, action);
		}
		/// <summary>
		/// VisitAll&lt;TContext&gt;
		/// </summary>

		public static void VisitAll<TContext>(this Clause element, TContext context, Action<TContext, ISQLNode> action)
		{
			using var actionVisitor = PoolHolder<TContext>.ActionPool.Allocate();
			actionVisitor.Value.Visit(context, element, true, action);
		}
		/// <summary>
		/// VisitAll
		/// </summary>

		public static void VisitAll(this Clause element, Action<ISQLNode> action)
		{
			using var actionVisitor = ActionVisitorPool.Allocate();
			actionVisitor.Value.Visit(element, true, action);
		}

		#endregion

		#region VisitParent
		/// <summary>
		/// VisitParentFirst&lt;TContext&gt;
		/// </summary>
		/// <summary>
		/// VisitParentFirst&lt;TContext&gt;
		/// </summary>
		public static void VisitParentFirst<TContext>(this ISQLNode element, TContext context, Func<TContext, ISQLNode, bool> action)
		{
			using var actionVisitor = PoolHolder<TContext>.ParentFirstPool.Allocate();
			actionVisitor.Value.Visit(context, element, false, action);
		}
		/// <summary>
		/// VisitParentFirst
		/// </summary>

		public static void VisitParentFirst(this ISQLNode element, Func<ISQLNode, bool> action)
		{
			using var actionVisitor = ParentVisitorPool.Allocate();
			actionVisitor.Value.Visit(element as Clause, false, action);
		}
		/// <summary>
		/// VisitParentFirstAll&lt;TContext&gt;
		/// </summary>

		public static void VisitParentFirstAll<TContext>(this ISQLNode element, TContext context, Func<TContext, ISQLNode, bool> action)
		{
			using var actionVisitor = PoolHolder<TContext>.ParentFirstPool.Allocate();
			actionVisitor.Value.Visit(context, element, true, action);
		}
		/// <summary>
		/// VisitParentFirstAll
		/// </summary>

		public static void VisitParentFirstAll(this Clause element, Func<ISQLNode, bool> action)
		{
			using var actionVisitor = ParentVisitorPool.Allocate();
			actionVisitor.Value.Visit(element, true, action);
		}

		#endregion

		#region Find
		/// <summary>
		/// Find&lt;TContext&gt;
		/// </summary>
		/// <summary>
		/// Find&lt;TContext&gt;
		/// </summary>
		public static ISQLNode? Find<TContext>(this ISQLNode? element, TContext context, Func<TContext, ISQLNode, bool> find)
		{
			if (element == null)
				return null;

			using var findVisitor = PoolHolder<TContext>.FindPool.Allocate();
			return findVisitor.Value.Find(context, element, find);
		}
		/// <summary>
		/// Find
		/// </summary>

		public static ISQLNode? Find(this ISQLNode? element, Func<ISQLNode, bool> find)
		{
			if (element == null)
				return null;

			using var findVisitor = FindVisitorPool.Allocate();
			return findVisitor.Value.Find(element, find);
		}
		/// <summary>
		/// Find
		/// </summary>

		public static ISQLNode? Find(this ISQLNode? element, ClauseType type)
		{
			if (element == null)
				return null;

			using var findVisitor = PoolHolder<ClauseType>.FindPool.Allocate();
			return findVisitor.Value.Find(type, element, static (type, e) => e.NodeType == type);
		}
		#endregion

		#region FindExcept
		/// <summary>
		/// FindExcept&lt;TContext&gt;
		/// </summary>
		/// <summary>
		/// FindExcept&lt;TContext&gt;
		/// </summary>
		public static ISQLNode? FindExcept<TContext>(this ISQLNode? element, TContext context, ISQLNode skip, Func<TContext, ISQLNode, bool> find)
		{
			if (element == null)
				return null;

			using var findVisitor = PoolHolder<TContext>.FindExceptPool.Allocate();
			return findVisitor.Value.Find(context, element, skip, find);
		}
		#endregion

		#region Clone
		/// <summary>
		/// Clone&lt;T&gt;
		/// </summary>
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
		/// <summary>
		/// Clone&lt;T, TContext&gt;
		/// </summary>

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
		/// <summary>
		/// Clone&lt;T, TContext&gt;
		/// </summary>

		[return: NotNullIfNotNull(nameof(element))]
		public static T? Clone<T, TContext>(this T? element, TContext context, Func<TContext, Clause, bool> doClone)
			where T : Clause
		{
			if (element == null)
				return null;

			using var cloneVisitor = PoolHolder<TContext>.ClonePool.Allocate();

			return (T)cloneVisitor.Value.Clone(element, context, doClone);
		}
		/// <summary>
		/// Clone&lt;T&gt;
		/// </summary>

		[return: NotNullIfNotNull(nameof(element))]
		public static T? Clone<T>(this T? element, Func<Clause, bool> doClone)
			where T : Clause
		{
			if (element == null)
				return null;

			using var cloneVisitor = CloneVisitorPool.Allocate();

			return (T)cloneVisitor.Value.Clone(element, doClone);
		}
		/// <summary>
		/// Clone&lt;T&gt;
		/// </summary>

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
		/// <summary>
		/// Replace&lt;T&gt;
		/// </summary>

		public static T Replace<T>(this T element, IDictionary<Clause, Clause> replacements,
			params Clause[]   toIgnore)
			where T : Clause
		{
			using var replacingElementVisitor = ReplacingVisitorPool.Allocate();

			return (T)replacingElementVisitor.Value.Replace(element, replacements, toIgnore);
		}

		#endregion

		#region Convert
		/// <summary>
		/// Convert&lt;TContext, T&gt;
		/// </summary>

		public static T Convert<TContext, T>(this T element, TContext context, Func<SqlQueryConvertVisitor<TContext>, Clause, Clause> convertAction, bool withStack)
			where T : Clause
		{
			using var convertVisitor = PoolHolder<TContext>.ConvertPool.Allocate();

			return (T?)convertVisitor.Value.Convert(element, context, convertAction, withStack) ?? element;
		}
		/// <summary>
		/// Convert&lt;TContext, T&gt;
		/// </summary>

		public static T Convert<TContext, T>(this T element, TContext context, Func<SqlQueryConvertVisitor<TContext>, Clause, Clause> convertAction)
			where T : Clause
		{
			using var convertVisitor = PoolHolder<TContext>.ConvertPool.Allocate();

			return (T?)convertVisitor.Value.Convert(element, context, convertAction, false) ?? element;
		}
		/// <summary>
		/// Convert&lt;T&gt;
		/// </summary>

		public static T Convert<T>(this T element, Func<SqlQueryConvertVisitor<object?>, Clause, Clause> convertAction, bool withStack)
			where T : Clause
		{
			if (withStack)
				throw new NotImplementedException();

			using var convertVisitor = PoolHolder<object?>.ConvertPool.Allocate();

			return (T?)convertVisitor.Value.Convert(element, null, convertAction, false) ?? element;
		}
		/// <summary>
		/// Convert&lt;T&gt;
		/// </summary>

		public static T Convert<T>(this T element, Func<SqlQueryConvertVisitor<object?>, Clause, Clause> convertAction)
			where T : Clause
		{
			using var convertVisitor = PoolHolder<object?>.ConvertPool.Allocate();

			return (T?)convertVisitor.Value.Convert(element, null, convertAction, false) ?? element;
		}
		/// <summary>
		/// Convert&lt;TContext, T&gt;
		/// </summary>

		public static T Convert<TContext, T>(this T element, TContext context, bool allowMutation, Func<SqlQueryConvertVisitor<TContext>, Clause, Clause> convertAction, bool withStack)
			where T : Clause
		{
			using var convertVisitor = PoolHolder<TContext>.ConvertPool.Allocate();

			return (T?)convertVisitor.Value.Convert(element, context, convertAction, withStack) ?? element;
		}
		/// <summary>
		/// Convert&lt;TContext, T&gt;
		/// </summary>

		public static T Convert<TContext, T>(this T element, TContext context, bool allowMutation, Func<SqlQueryConvertVisitor<TContext>, Clause, Clause> convertAction)
			where T : Clause
		{
			using var convertVisitor = allowMutation
				? PoolHolder<TContext>.ConvertMutationPool.Allocate()
				: PoolHolder<TContext>.ConvertPool.Allocate();

			return (T?)convertVisitor.Value.Convert(element, context, convertAction, false) ?? element;
		}
		/// <summary>
		/// ConvertAll&lt;TContext, T&gt;
		/// </summary>

		public static T ConvertAll<TContext, T>(this T element, TContext context, Func<SqlQueryConvertVisitor<TContext>, Clause, Clause> convertAction)
			where T : Clause
		{
			using var convertVisitor = PoolHolder<TContext>.ConvertPool.Allocate();

			return (T?)convertVisitor.Value.Convert(element, context, convertAction, false) ?? element;
		}
		/// <summary>
		/// ConvertAll&lt;TContext, T&gt;
		/// </summary>

		public static T ConvertAll<TContext, T>(this T element, TContext context, bool allowMutation, Func<SqlQueryConvertVisitor<TContext>, Clause, Clause> convertAction)
			where T : Clause
		{
			using var convertVisitor = allowMutation
				? PoolHolder<TContext>.ConvertMutationPool.Allocate()
				: PoolHolder<TContext>.ConvertPool.Allocate();

			return (T?)convertVisitor.Value.Convert(element, context, convertAction, false) ?? element;
		}
		/// <summary>
		/// ConvertAll&lt;T&gt;
		/// </summary>

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
