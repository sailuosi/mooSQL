using System;

namespace mooSQL.data
{
    public partial class EntityContext
    {
        EntityRelationRegistry _relations;

        /// <summary>
        /// 客户端级实体关系注册表（Fluent configureEntity / Relation）。
        /// </summary>
        public EntityRelationRegistry Relations
        {
            get
            {
                if (_relations == null)
                    _relations = new EntityRelationRegistry();
                return _relations;
            }
        }

        /// <summary>
        /// Fluent 配置实体关系（对标 CRL ConfigEntity）。
        /// </summary>
        public EntityContext configureEntity<T>(Action<EntityRelationBuilder<T>> configure) where T : class
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));
            var builder = new EntityRelationBuilder<T>(this);
            configure(builder);
            return this;
        }

        /// <summary>
        /// 导航列写入后同步 <see cref="EntityDictionary.Fields"/>，供 FindField / includeNav 使用。
        /// </summary>
        internal void SyncFieldCache(Type entityType, EntityColumn column)
        {
            if (entityType == null || column == null) return;
            if (!typeMap.TryGetValue(entityType, out var dic) || dic?.Fields == null) return;
            dic.Fields[column.PropertyName] = column;
        }
    }
}
