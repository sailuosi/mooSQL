namespace mooSQL.data
{
    /// <summary>
    /// SELECT 结果缓存键辅助（与 <see cref="SQLCmd.GetCacheKey"/> / <see cref="StepBuilder.useCachePrefix"/> 配合）。
    /// </summary>
    public static class ResultCacheKey
    {
        public const string UserPrefix = "RC:USER:";

        /// <summary>
        /// 规范化业务显式键：已以 <c>RC:</c> 开头则原样，否则 <c>RC:USER:{databaseId}:{userKey}</c>。
        /// </summary>
        public static string ForUser(string databaseId, string userKey)
        {
            if (string.IsNullOrWhiteSpace(userKey))
                return userKey;
            if (userKey.StartsWith(SQLCmd.ResultCacheKeyPrefix, System.StringComparison.Ordinal))
                return userKey;
            return UserPrefix + (databaseId ?? "") + ":" + userKey;
        }
    }
}
