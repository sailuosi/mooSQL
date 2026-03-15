// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace mooSQL.linq.Common.Internal.Cache
{
	internal sealed class CacheEntryHelper<TKey,TEntry>
		where TKey : notnull
	{
#if NET6_0_OR_GREATER
        private static readonly AsyncLocal<CacheEntryStack<TKey,TEntry>> _scopes = new ();
#else
        private static CacheEntryStack<TKey, TEntry> _scopes = new();
#endif


        internal static CacheEntryStack<TKey,TEntry>? Scopes
		{
#if NET6_0_OR_GREATER
            get => _scopes.Value;
			set => _scopes.Value = value!;
#else
            get => _scopes;
			set => _scopes = value;
#endif

		}

		internal static CacheEntry<TKey,TEntry>? Current
		{
			get
			{
				var scopes = GetOrCreateScopes();
				return scopes.Peek();
			}
		}

		internal static IDisposable EnterScope(CacheEntry<TKey,TEntry> entry)
		{
			var scopes = GetOrCreateScopes();

			var scopeLease = new ScopeLease(scopes);
			Scopes = scopes.Push(entry);

			return scopeLease;
		}

		private static CacheEntryStack<TKey,TEntry> GetOrCreateScopes()
		{
			return Scopes ??= CacheEntryStack<TKey,TEntry>.Empty;
		}

		private sealed class ScopeLease : IDisposable
		{
			readonly CacheEntryStack<TKey,TEntry> _cacheEntryStack;

			public ScopeLease(CacheEntryStack<TKey,TEntry> cacheEntryStack)
			{
				_cacheEntryStack = cacheEntryStack;
			}

			public void Dispose()
			{
				Scopes = _cacheEntryStack;
			}
		}
	}
}
