namespace mooSQL.data
{
    /// <summary>
    /// openGauss / GaussDB 子句翻译：序列 nextval（与 Npgsql 相同）。
    /// </summary>
    public class OpenGaussClauseTranslator : NpgClauseTranslator
    {
        public OpenGaussClauseTranslator(Dialect dia) : base(dia)
        {
        }
    }
}
