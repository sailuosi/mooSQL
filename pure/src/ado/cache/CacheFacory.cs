


namespace mooSQL.data
{
    /// <summary>
    /// 类型 CacheFacory。
    /// </summary>
    public class CacheFacory
    {
        /// <summary>
        /// 字段 hashCache（HashCache）。
        /// </summary>
        public static HashCache hashCache = null;

        /// <summary>
        /// getHashCache 方法（返回 HashCache）。
        /// </summary>
        public static HashCache getHashCache()
        {
            if (hashCache == null)
            {
                hashCache = new HashCache();
            }
            return hashCache;
        }

    }
}