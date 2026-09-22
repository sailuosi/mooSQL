namespace mooSQL.data
{
    /// <summary>
    /// openGauss / GaussDB 数据字典：PG 兼容模式沿用 information_schema / pg_*。
    /// </summary>
    public class OpenGaussSentence : NpgSentence
    {
        public OpenGaussSentence(Dialect dialect) : base(dialect)
        {
        }
    }
}
