using mooSQL.linq.Linq.Translation;

namespace mooSQL.linq.translator;

/// <summary>非 MSSQL/MySQL 方言的默认 MemberTranslator（日期/数学/SqlTypes + registry 优先）。</summary>
public class DefaultMemberTranslator : ProviderMemberTranslatorDefault
{
    protected override IMemberTranslator CreateDateMemberTranslator() => new DateFunctionsTranslatorBase();
}
