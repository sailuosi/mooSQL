#if !NET451
namespace mooSQL.data
{
    /// <summary>
    /// 金仓数据字典：PG 兼容模式沿用 information_schema / pg_*。
    /// </summary>
    public class KingBaseSentence : NpgSentence
    {
        public KingBaseSentence(Dialect dialect) : base(dialect)
        {
        }
    }
}
#endif
