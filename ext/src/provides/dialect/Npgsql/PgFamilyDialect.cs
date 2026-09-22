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
        protected PgFamilyDialect()
        {
            expression = new NpgsqlExpress(this);
            clauseTranslator = new NpgClauseTranslator(this);
            mapping = new NpgMappingPanel();
            sentence = new NpgSentence(this);
            function = new NpgSQLFunction();
        }

        /// <summary>PG 族默认 Bulk：多行 INSERT；原生 COPY 仅由 Npgsql 产品覆盖。</summary>
        public override DbBulkCopy GetBulkCopy()
            => new DbBulkCopyFallback(this.dbInstance);

        public override bool SupportsMerge() => true;

        protected override IMemberTranslator CreateMemberTranslator()
            => new NpgsqlMemberTranslator();
    }
}
