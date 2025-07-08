using System;
using System.ComponentModel;
using System.Data;

namespace mooSQL.data
{

    /// <summary>
    /// Not intended for direct usage
    /// </summary>
    /// <typeparam name="T">The type to have a cache for.</typeparam>

    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class TypeHandlerCache<T>
    {
        /// <summary>
        /// Not intended for direct usage.
        /// </summary>
        /// <param name="value">The object to parse.</param>

        public static T Parse(object value) => (T)handler.Parse(typeof(T), value);



        internal static void SetHandler(ITypeHandler handler)
        {
            TypeHandlerCache<T>.handler = handler;
        }

        private static ITypeHandler handler = null;
    }
    
}
