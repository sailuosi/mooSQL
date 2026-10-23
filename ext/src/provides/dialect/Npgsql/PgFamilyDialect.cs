using System.Data.Common;
using mooSQL.data.Npgsql;
using mooSQL.linq;

namespace mooSQL.data
{
    /// <summary>
    /// PostgreSQL Family 方言基类：PG SQL 轮子与默认 Bulk=<see cref="DbBulkCopyFallback"/>。
    /// ADO 不绑定 Npgsql 具体类型，由产品方言实现（Npgsql / OpenGauss…）。
    /// </summary>
    public abstract class PgFamilyDialect : ExtDialect
    {
        /// <summary>
        /// PgFamilyDialect type.
        /// </summary>
        /// <summary>
        /// PgFamilyDialect type.
        /// </summary>
        protected PgFamilyDialect()
        {
            expression = new NpgsqlExpress(this);
            clauseTranslator = new NpgClauseTranslator(this);
            mapping = new NpgMappingPanel();
            sentence = new NpgSentence(this);
            function = new NpgSQLFunction();
            Option.ProviderFlags.IsInsertReturningSupported = true;
        }

        /// <summary>PG 族默认 Bulk：多行 INSERT；原生 COPY 仅由 Npgsql 产品覆盖。</summary>
        public override DbBulkCopy GetBulkCopy()
            => new DbBulkCopyFallback(this.dbInstance);
/// <summary>
/// Returns whether MERGE statements are supported.
/// </summary>
 

        public override bool SupportsMerge() => true;
/// <summary>
/// Creates the LINQ member translator for this dialect.
/// </summary>
 

        protected override IMemberTranslator CreateMemberTranslator()
            => new NpgsqlMemberTranslator();
    }
}
