using System.Data.Common;

namespace mooSQL.data
{
    /// <summary>
    /// CrateDB 方言：继承 Npgsql（PG wire + stock Npgsql），覆盖 Bulk / Express / Sentence。
    /// </summary>
    public class CrateDBDialect : NpgsqlDialect
    {
        public CrateDBDialect() : base()
        {
            expression = new CrateDBExpress(this);
            sentence = new CrateDBSentence(this);
            // mapping / function / clauseTranslator 沿用 NpgsqlDialect 基类赋值
        }

        /// <summary>
        /// Crate 一般不支持 PostgreSQL COPY BINARY；退化为多行 INSERT。
        /// </summary>
        public override DbBulkCopy GetBulkCopy()
        {
            return new DbBulkCopyFallback(this.dbInstance);
        }
    }
}
