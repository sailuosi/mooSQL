namespace mooSQL.utils
{
    /// <summary>
    /// 空数组缓存，net451 兼容替代 <c>Array.Empty&lt;T&gt;()</c>。
    /// </summary>
    public static class ArrayCache
    {
        /// <summary>返回类型 <typeparamref name="T"/> 的共享空数组实例。</summary>
        public static T[] Empty<T>() => EmptyArray<T>.Value;

        static class EmptyArray<T>
        {
            internal static readonly T[] Value = new T[0];
        }
    }
}
