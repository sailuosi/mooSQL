using System;
using System.Runtime.CompilerServices;

namespace mooSQL.utils
{
    using mooSQL.data.Extensions;
    using mooSQL.data.model;
    using System.Diagnostics;
    using System.Reflection;
    internal sealed class CacheDict<TKey, TValue> where TKey : notnull
    {
        // cache size is always ^2.
        // items are placed at [hash ^ mask]
        // new item will displace previous one at the same location.
        private readonly int _mask;
        private readonly Entry[] _entries;

        // class, to ensure atomic updates.
        private sealed class Entry
        {
            internal readonly int _hash;
            internal readonly TKey _key;
            internal readonly TValue _value;

            internal Entry(int hash, TKey key, TValue value)
            {
                _hash = hash;
                _key = key;
                _value = value;
            }
        }

        /// <summary>
        /// Creates a dictionary-like object used for caches.
        /// </summary>
        /// <param name="size">The maximum number of elements to store will be this number aligned to next ^2.</param>
        internal CacheDict(int size)
        {
            int alignedSize = AlignSize(size);
            _mask = alignedSize - 1;
            _entries = new Entry[alignedSize];
        }

        private static int AlignSize(int size)
        {
            Debug.Assert(size > 0);

            size--;
            size |= size >> 1;
            size |= size >> 2;
            size |= size >> 4;
            size |= size >> 8;
            size |= size >> 16;
            size++;

            Debug.Assert((size & (~size + 1)) == size, "aligned size should be a power of 2");
            return size;
        }

        /// <summary>
        /// Tries to get the value associated with 'key', returning true if it's found and
        /// false if it's not present.
        /// </summary>
        internal bool TryGetValue(TKey key, out TValue value)
        {
            int hash = key.GetHashCode();
            int idx = hash & _mask;

            Entry entry = Volatile.Read(ref _entries[idx]);
            if (entry != null && entry._hash == hash && entry._key.Equals(key))
            {
                value = entry._value;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Adds a new element to the cache, possibly replacing some
        /// element that is already present.
        /// </summary>
        internal void Add(TKey key, TValue value)
        {
            int hash = key.GetHashCode();
            int idx = hash & _mask;

            Entry entry = Volatile.Read(ref _entries[idx]);
            if (entry == null || entry._hash != hash || !entry._key.Equals(key))
            {
                Volatile.Write(ref _entries[idx], new Entry(hash, key, value));
            }
        }

        /// <summary>
        /// Sets the value associated with the given key.
        /// </summary>
        internal TValue this[TKey key]
        {
            set
            {
                Add(key, value);
            }
        }
    }
    public static class TypeExtensions
	{


		// don't change visibility, used by linq2db.EntityFramework


        private static readonly CacheDict<MethodBase, ParameterInfo[]> s_paramInfoCache = new CacheDict<MethodBase, ParameterInfo[]>(75);

        /// <summary>
        /// Returns the matching method if the parameter types are reference
        /// assignable from the provided type arguments, otherwise null.
        /// </summary>
        public static MethodInfo? GetAnyStaticMethodValidated(
            this Type type,
            string name,
            Type[] types)
        {
            Debug.Assert(types != null);
            MethodInfo? method = type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly, null, types, null);
            return method.MatchesArgumentTypes(types) ? method : null;
        }

        /// <summary>
        /// Returns true if the method's parameter types are reference assignable from
        /// the argument types, otherwise false.
        ///
        /// An example that can make the method return false is that
        /// typeof(double).GetMethod("op_Equality", ..., new[] { typeof(double), typeof(int) })
        /// returns a method with two double parameters, which doesn't match the provided
        /// argument types.
        /// </summary>
        private static bool MatchesArgumentTypes(this MethodInfo? mi, Type[] argTypes)
        {
            Debug.Assert(argTypes != null);

            if (mi == null)
            {
                return false;
            }

            ParameterInfo[] ps = mi.GetParametersCached();

            if (ps.Length != argTypes.Length)
            {
                return false;
            }

            for (int i = 0; i < ps.Length; i++)
            {
                if (!TypeUtils.AreReferenceAssignable(ps[i].ParameterType, argTypes[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static Type GetReturnType(this MethodBase mi) => mi.IsConstructor ? mi.DeclaringType! : ((MethodInfo)mi).ReturnType;


        internal static ParameterInfo[] GetParametersCached(this MethodBase method)
        {
            CacheDict<MethodBase, ParameterInfo[]> pic = s_paramInfoCache;
            if (!pic.TryGetValue(method, out ParameterInfo[]? pis))
            {
                pis = method.GetParameters();
#if NET5_0_OR_GREATER
                if (method.DeclaringType?.IsCollectible == false)
                {
                    pic[method] = pis;
                }
#endif
            }

            return pis;
        }

        // Expression trees/compiler just use IsByRef, why do we need this?
        // (see LambdaCompiler.EmitArguments for usage in the compiler)
        internal static bool IsByRefParameter(this ParameterInfo pi)
        {
            // not using IsIn/IsOut properties as they are not available in Silverlight:
            if (pi.ParameterType.IsByRef)
                return true;

            return (pi.Attributes & ParameterAttributes.Out) == ParameterAttributes.Out;
        }
    }
}
