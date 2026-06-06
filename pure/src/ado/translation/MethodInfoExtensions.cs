using System.Collections.Concurrent;
using System.Reflection;

namespace mooSQL.data.translation
{
    internal static class MethodInfoExtensions
    {
        static readonly ConcurrentDictionary<MethodInfo, MethodInfo> GenericDefinitionCache = new();

        public static MethodInfo GetGenericMethodDefinitionCached(this MethodInfo method)
        {
            if (!method.IsGenericMethod)
                return method;

            return GenericDefinitionCache.GetOrAdd(method, static m => m.GetGenericMethodDefinition());
        }
    }
}
