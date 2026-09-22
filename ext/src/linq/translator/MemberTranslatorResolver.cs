using mooSQL.data;
using mooSQL.linq.Linq.Translation;

namespace mooSQL.linq.translator;

/// <summary>
/// 按方言解析 <see cref="IMemberTranslator"/>，并挂接 Pure <see cref="mooSQL.data.translation.DbFuncRegistry"/>。
/// </summary>
internal static class MemberTranslatorResolver
{
    public static IMemberTranslator Resolve(DBInstance db)
    {
        DbFuncRegistryBootstrap.EnsureRegistered(db);

        IMemberTranslator inner = db.dialect.GetType().Name switch
        {
            nameof(MSSQLDialect) => new SqlServerMemberTranslator(),
            "MySQLDialect"       => new MySqlMemberTranslator(),
            nameof(SQLiteDialect)=> new SQLiteMemberTranslator(),
            nameof(NpgsqlDialect)=> new NpgsqlMemberTranslator(),
            nameof(CrateDBDialect)=> new NpgsqlMemberTranslator(),
#if NET6_0_OR_GREATER
            nameof(DuckDBDialect)=> new DuckDBMemberTranslator(),
            nameof(ClickHouseDialect)=> new ClickHouseMemberTranslator(),
#endif
            _                    => new DefaultMemberTranslator()
        };

        return new RegistryAwareMemberTranslator(inner, db);
    }
}
