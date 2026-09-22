#if !NET451
namespace mooSQL.data
{
    /// <summary>
    /// 金仓表达式：SQL 与 Npgsql 一致，Provider 指向 Kdbndp。
    /// </summary>
    public class KingBaseExpress : NpgsqlExpress
    {
        public KingBaseExpress(Dialect dia) : base(dia)
        {
            _provideType = "Kdbndp.KdbndpFactory,Kdbndp";
        }
    }
}
#endif
