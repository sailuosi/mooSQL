using mooSQL.data;
using mooSQL.linq.DataProvider.MySql.Translation;
using mooSQL.linq.DataProvider.SqlServer.Translation;
using mooSQL.linq.Linq.Translation;

namespace mooSQL.linq.translator;

/// <summary>
/// 按当前方言解析 <see cref="IMemberTranslator"/>（替代 linq2db DI 注册）。
/// </summary>
internal static class MemberTranslatorResolver
{
    public static IMemberTranslator Resolve(DBInstance db)
    {
        return db.dialect.GetType().Name switch
        {
            nameof(MSSQLDialect) => new SqlServerMemberTranslator(),
            "MySQLDialect"       => new MySqlMemberTranslator(),
            _                    => new CombinedMemberTranslator()
        };
    }
}
