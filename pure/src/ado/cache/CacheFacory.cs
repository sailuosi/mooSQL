


namespace mooSQL.data
{
    public class CacheFacory
    {
        public static HashCache hashCache = null;

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
