using System;
using System.Collections.Generic;
using System.Linq;

namespace mooSQL.data.model
{
	using Common;
    using mooSQL.utils;

    public class AliasContext
	{
		readonly HashSet<ISQLNode> _aliasesSet = new (ObjectReferenceEqualityComparer<ISQLNode>.Default);



		public void RegisterAliased(ISQLNode element)
		{
			_aliasesSet.Add(element);
		}

		public void RegisterAliased(ICollection<ISQLNode> elements)
		{
			_aliasesSet.AddRange(elements);
		}

		public bool IsAliased(ISQLNode element)
		{
			return _aliasesSet.Contains(element);
		}

		public ICollection<ISQLNode> GetAliased()
		{
			return _aliasesSet;
		}


	}
}
