using System;

namespace mooSQL.data
{
    public partial class MooClient
    {
        /// <summary>
        /// Fluent 配置实体关系（对标 CRL ConfigEntity / Relation）。
        /// </summary>
        /// <example>
        /// client.configureEntity&lt;Blog&gt;(p =&gt;
        /// {
        ///     p.Relation&lt;Post&gt;((a, b) =&gt; a.Id == b.BlogId);
        ///     p.Relation&lt;BlogUser&gt;((a, b) =&gt; a.UserId == b.Id);
        /// });
        /// </example>
        public MooClient configureEntity<T>(Action<EntityRelationBuilder<T>> configure) where T : class
        {
            EntityCash.configureEntity(configure);
            return this;
        }
    }
}
