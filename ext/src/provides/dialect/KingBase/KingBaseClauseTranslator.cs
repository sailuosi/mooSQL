#if !NET451
namespace mooSQL.data
{
    /// <summary>
    /// 金仓子句翻译：序列 nextval（与 Npgsql 相同）。
    /// </summary>
    public class KingBaseClauseTranslator : NpgClauseTranslator
    {
        /// <summary>
        /// KingBaseClauseTranslator type.
        /// </summary>
        /// <summary>
        /// KingBaseClauseTranslator type.
        /// </summary>
        public KingBaseClauseTranslator(Dialect dia) : base(dia)
        {
        }
    }
}
#endif
