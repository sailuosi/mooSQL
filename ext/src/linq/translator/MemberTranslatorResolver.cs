using mooSQL.data;
using mooSQL.linq.DataProvider.MySql.Translation;
using mooSQL.linq.DataProvider.SqlServer.Translation;
using mooSQL.linq.Linq.Translation;

namespace mooSQL.linq.translator;

/// <summary>
/// 按当前方言解析 <see cref="IMemberTranslator"/>（按方言解析 MemberTranslator）。
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
