namespace mooSQL.data
{
    /// <summary>
    /// openGauss / GaussDB 表达式：SQL 与 Npgsql 一致；Provider 按 TFM 指向 GaussDB 或 Npgsql。
    /// </summary>
    public class OpenGaussExpress : NpgsqlExpress
    {
        public OpenGaussExpress(Dialect dia) : base(dia)
        {
#if NET8_0_OR_GREATER
            _provideType = "HuaweiCloud.GaussDB.GaussDBFactory,HuaweiCloud.Driver.GaussDB";
#else
            _provideType = "Npgsql.NpgsqlFactory,Npgsql";
#endif
        }
    }
}
