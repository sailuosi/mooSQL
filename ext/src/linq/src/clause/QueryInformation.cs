using mooSQL.data.model;
using System;
using System.Collections.Generic;

namespace mooSQL.linq.SqlQuery
{
	/// <summary>
	/// 这是内部API，不应由业务侧使用.
	/// It may change or be removed without further notice.
	/// </summary>
	public class QueryInformation
	{
		public SelectQueryClause RootQuery { get; }

		private Dictionary<SelectQueryClause, HierarchyInfo>?     _parents;
		private Dictionary<SelectQueryClause, List<SelectQueryClause>>? _tree;

		/// <summary>
		/// 这是内部API，不应由业务侧使用.
		/// It may change or be removed without further notice.
		/// </summary>
		public QueryInformation(SelectQueryClause rootQuery)
		{
			RootQuery = rootQuery ?? throw new ArgumentNullException(nameof(rootQuery));
		}

		/// <summary>
		/// Returns parent query if query is subquery for select
		/// </summary>
		/// <param name="selectQuery"></param>
		/// <returns></returns>
		public SelectQueryClause? GetParentQuery(SelectQueryClause selectQuery)
		{
			var info = GetHierarchyInfo(selectQuery);
			return info?.HierarchyType == HierarchyType.From || info?.HierarchyType == HierarchyType.Join
				? info.MasterQuery
				: null;
		}

		/// <summary>
		/// Returns HirarchyInfo for specific selectQuery
		/// </summary>
		/// <param name="selectQuery"></param>
		/// <returns></returns>
		public HierarchyInfo? GetHierarchyInfo(SelectQueryClause selectQuery)
		{
			CheckInitialized();
			_parents!.TryGetValue(selectQuery, out var result);
			return result;
		}

		private void CheckInitialized()
		{
			if (_parents == null)
			{
				_parents = new Dictionary<SelectQueryClause, HierarchyInfo>();
				_tree    = new Dictionary<SelectQueryClause, List<SelectQueryClause>>();
				BuildParentHierarchy(RootQuery);
			}
		}

		/// <summary>
		/// Resync tree info. Can be called also during enumeration.
		/// </summary>
		public void Resync()
		{
			_parents = null;
			_tree    = null;
		}

		public IEnumerable<SelectQueryClause> GetQueriesParentFirst()
		{
			return GetQueriesParentFirst(RootQuery);
		}

		public IEnumerable<SelectQueryClause> GetQueriesParentFirst(SelectQueryClause root)
		{
			yield return root;

			CheckInitialized();

			if (_tree!.TryGetValue(root, out var list))
			{
				// assuming that list at this stage is immutable
				foreach (var item in list)
				foreach (var subItem in GetQueriesParentFirst(item))
				{
					yield return subItem;
				}
			}
		}

		public IEnumerable<SelectQueryClause> GetQueriesChildFirst()
		{
			return GetQueriesChildFirst(RootQuery);
		}

		public IEnumerable<SelectQueryClause> GetQueriesChildFirst(SelectQueryClause root)
		{
			CheckInitialized();

			if (_tree!.TryGetValue(root, out var list))
			{
				foreach (var item in list)
				foreach (var subItem in GetQueriesChildFirst(item))
				{
					yield return subItem;
				}

				// assuming that list at this stage is immutable
				foreach (var item in list)
				{
					yield return item;
				}
			}

			yield return root;
		}

		void RegisterHierarchry(SelectQueryClause parent, SelectQueryClause child, HierarchyInfo info)
		{
			_parents![child] = info;

			if (!_tree!.TryGetValue(parent, out var list))
			{
				list = new List<SelectQueryClause>();
				_tree.Add(parent, list);
			}
			list.Add(child);
		}

		void BuildParentHierarchy(SelectQueryClause selectQuery)
		{
			foreach (var table in selectQuery.From.Tables)
			{
				if (table.FindISrc() is SelectQueryClause s)
				{
					RegisterHierarchry(selectQuery, s, new HierarchyInfo(selectQuery, HierarchyType.From, selectQuery));

					foreach (var setOperator in s.SetOperators)
					{
						RegisterHierarchry(selectQuery, setOperator.SelectQuery, new HierarchyInfo(selectQuery, HierarchyType.SetOperator, setOperator));
						BuildParentHierarchy(setOperator.SelectQuery);
					}

					BuildParentHierarchy(s);
				}

				foreach (var joinedTable in table.GetJoins())
				{
					if (joinedTable.Table.FindISrc() is SelectQueryClause joinQuery)
					{
						RegisterHierarchry(selectQuery, joinQuery,
							new HierarchyInfo(selectQuery, HierarchyType.Join, joinedTable));
						BuildParentHierarchy(joinQuery);
					}
				}

			}

			var items = new List<ISQLNode>
			{
				selectQuery.GroupBy,
				selectQuery.Having,
				selectQuery.Where,
				selectQuery.OrderBy
			};

			items.AddRange(selectQuery.Select.Columns.content);
			if (!selectQuery.Where.IsEmpty)
				items.Add(selectQuery.Where);

			var ctx = new BuildParentHierarchyContext(this, selectQuery);
			foreach (var item in items)
			{
				ctx.Parent = null;
				item.VisitParentFirst(ctx, static (context, e) =>
				{
					if (e is SelectQueryClause q)
					{
						context.Info.RegisterHierarchry(context.SelectQuery, q, new HierarchyInfo(context.SelectQuery, HierarchyType.InnerQuery, context.Parent));
						context.Info.BuildParentHierarchy(q);
						return false;
					}

					context.Parent = e;

					return true;
				});
			}
		}

		private sealed class BuildParentHierarchyContext
		{
			public BuildParentHierarchyContext(QueryInformation qi, SelectQueryClause selectQuery)
			{
				Info        = qi;
				SelectQuery = selectQuery;
			}

			public readonly QueryInformation Info;
			public readonly SelectQueryClause      SelectQuery;

			public ISQLNode? Parent;
		}

		public enum HierarchyType
		{
			From,
			Join,
			SetOperator,
			InnerQuery
		}

		public class HierarchyInfo
		{
			public HierarchyInfo(SelectQueryClause masterQuery, HierarchyType hierarchyType, ISQLNode? parentElement)
			{
				MasterQuery   = masterQuery;
				HierarchyType = hierarchyType;
				ParentElement = parentElement;
			}

			public SelectQueryClause    MasterQuery   { get; }
			public HierarchyType  HierarchyType { get; }
			public ISQLNode? ParentElement { get; }
		}
	}
}
