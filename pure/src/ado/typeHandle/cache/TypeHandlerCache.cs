using System;
using System.ComponentModel;
using System.Data;

namespace mooSQL.data
{

    /// <summary>
    /// 不适用于直接使用
    /// </summary>
    /// <typeparam name="T">要为其创建缓存的类型。</typeparam>

    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class TypeHandlerCache<T>
    {
        /// <summary>
        /// 不适用于直接使用。
        /// </summary>
        /// <param name="value">要解析的对象。</param>

        public static T Parse(object value) => (T)handler.Parse(typeof(T), value);



        internal static void SetHandler(ITypeParser handler)
        {
            TypeHandlerCache<T>.handler = handler;
        }

        private static ITypeParser handler = null;
    }
    
}
