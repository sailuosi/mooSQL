using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

namespace mooSQL.linq.utils
{
	using mooSQL.linq.utils;
	using mooSQL.linq;
	using mooSQL.linq.mapping;

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
