using mooSQL.data.model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace mooSQL.linq.SqlQuery
{
	public partial class ReservedWords
	{
		 ReservedWords()
		{
			_reservedWords[string.Empty]               = _reservedWordsAll;
			_reservedWords[ProviderName.PostgreSQL]    = _reservedWordsPostgres;
			_reservedWords[ProviderName.Oracle]        = _reservedWordsOracle;
			_reservedWords[ProviderName.Firebird]      = _reservedWordsFirebird;
			_reservedWords[ProviderName.Informix]      = _reservedWordsInformix;

			var assembly = typeof(SelectQueryClause).Assembly;
			//var name = assembly.GetManifestResourceNames().Single(_ => _.EndsWith("ReservedWords.txt"));

			//using (var stream = assembly.GetManifestResourceStream(name)!)
			//using (var reader = new StreamReader(stream))
			//{
			//	string? s;
			//	while ((s = reader.ReadLine()) != null)
			//	{
			//		if (!s.StartsWith("#"))
			//		{
			//			_reservedWordsAll     .Add(s);
			//			_reservedWordsInformix.Add(s);
			//		}
			//	}
			//}

			//name = assembly.GetManifestResourceNames().Single(_ => _.EndsWith("ReservedWordsPostgres.txt"));

			//using (var stream = assembly.GetManifestResourceStream(name)!)
			//using (var reader = new StreamReader(stream))
			//{
			//	string? s;
			//	while ((s = reader.ReadLine()) != null)
			//	{
			//		if (!s.StartsWith("#"))
			//		{
			//			_reservedWordsPostgres.Add(s);
			//			_reservedWordsAll     .Add(s);
			//		}
			//	}
			//}

			//name = assembly.GetManifestResourceNames().Single(_ => _.EndsWith("ReservedWordsOracle.txt"));

			//using (var stream = assembly.GetManifestResourceStream(name)!)
			//using (var reader = new StreamReader(stream))
			//{
			//	string? s;
			//	while ((s = reader.ReadLine()) != null)
			//	{
			//		if(!s.StartsWith("#"))
			//		{
			//			_reservedWordsOracle.Add(s);
			//			_reservedWordsAll   .Add(s);
			//		}
			//	}
			//}

			//name = assembly.GetManifestResourceNames().Single(_ => _.EndsWith("ReservedWordsFirebird.txt"));

			//using (var stream = assembly.GetManifestResourceStream(name)!)
			//using (var reader = new StreamReader(stream))
			//{
			//	string? s;
			//	while ((s = reader.ReadLine()) != null)
			//	{
			//		if (!s.StartsWith("#"))
			//		{
			//			_reservedWordsFirebird.Add(s);
			//			_reservedWordsAll     .Add(s);
			//		}
			//	}
			//}

			_reservedWordsInformix.Add("item");
			_reservedWordsAll     .Add("item");
		}

		 readonly HashSet<string> _reservedWordsAll      = new (StringComparer.OrdinalIgnoreCase);
		 readonly HashSet<string> _reservedWordsPostgres = new (StringComparer.OrdinalIgnoreCase);
		 readonly HashSet<string> _reservedWordsOracle   = new (StringComparer.OrdinalIgnoreCase);
		 readonly HashSet<string> _reservedWordsFirebird = new (StringComparer.OrdinalIgnoreCase);
		 readonly HashSet<string> _reservedWordsInformix = new (StringComparer.OrdinalIgnoreCase);

		 readonly ConcurrentDictionary<string,HashSet<string>> _reservedWords = new (StringComparer.OrdinalIgnoreCase);

		private static ReservedWords Instance;
		public static  bool IsReserved(string word, string? providerName = null)
		{
			if (Instance == null) {
				Instance = new ReservedWords();

            }
			//if (providerName == null)
				return Instance.words.Contains(word);

			//if (!Instance._reservedWords.TryGetValue(providerName, out var words))
			//	words = Instance.words;

			//return words.Contains(word);
		}

		public  void Add(string word, string? providerName = null)
		{
			lock (_reservedWordsAll)
				_reservedWordsAll.Add(word);

			if (providerName == null)
				return;

			var set = _reservedWords.GetOrAdd(providerName, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

			lock (set)
				set.Add(word);
		}
	}
}
