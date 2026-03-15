using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

namespace mooSQL.linq.Common
{
	using Data;
	using Linq;
	using Mapping;
	using Reflection;

	/// <summary>
	/// Various general-purpose helpers.
	/// </summary>
	public static class Tools
	{


		internal static HashSet<T> AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> items)
		{
			foreach (var item in items)
				hashSet.Add(item);
			return hashSet;
		}

	}
}
