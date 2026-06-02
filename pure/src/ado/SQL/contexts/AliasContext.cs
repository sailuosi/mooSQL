using System;
using System.Collections.Generic;
using System.Linq;

namespace mooSQL.data.model
{
	using Common;
    using mooSQL.utils;

    /// <summary>
    /// 类型 AliasContext。
    /// </summary>
    public class AliasContext
	{
		readonly HashSet<ISQLNode> _aliasesSet = new (ObjectReferenceEqualityComparer<ISQLNode>.Default);



		/// <summary>
		/// RegisterAliased 方法。
		/// </summary>
		public void RegisterAliased(ISQLNode element)
		{
			_aliasesSet.Add(element);
		}

		/// <summary>
		/// RegisterAliased 方法。
		/// </summary>
		public void RegisterAliased(ICollection<ISQLNode> elements)
		{
			_aliasesSet.AddRange(elements);
		}

		/// <summary>
		/// 判断是否为Aliased。
		/// </summary>
		public bool IsAliased(ISQLNode element)
		{
			return _aliasesSet.Contains(element);
		}

		/// <summary>
		/// 获取Aliased。
		/// </summary>
		public ICollection<ISQLNode> GetAliased()
		{
			return _aliasesSet;
		}


	}
}