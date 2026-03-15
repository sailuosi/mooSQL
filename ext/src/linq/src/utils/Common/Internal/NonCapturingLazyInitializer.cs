using System;
using System.Threading;

namespace mooSQL.linq.Common.Internal
{
    internal static class NonCapturingLazyInitializer
    {
        public static TValue EnsureInitialized<TParam, TValue>(
            ref TValue? target,
            TParam param,
            Func<TParam, TValue?> valueFactory)
            where TValue : class
        {
            var tmp = Volatile.Read(ref target);
            if (tmp != null)
            {
                return tmp;
            }

            Interlocked.CompareExchange(ref target, valueFactory(param), null);

            return target!;
        }


    }}
