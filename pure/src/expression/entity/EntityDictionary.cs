using mooSQL.data.Mapping;
using System;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// 单个实体类型的字段字典与 <see cref="EntityInfo"/> 缓存。
    /// </summary>
    public class EntityDictionary
    {
        /// <summary>实体 CLR 类型。</summary>
        public Type entityType;

        /// <summary>
        /// 按类型构造，需调用 <see cref="init"/> 初始化映射。
        /// </summary>
        public EntityDictionary(Type entityType) { 
            this.entityType = entityType;
        }

        /// <summary>属性名到列映射。</summary>
        public Dictionary<string, EntityColumn> Fields { get; private set; }
        /// <summary>表级实体信息。</summary>
        public EntityInfo EntityInfo { get; private set; }

        /// <summary>
        /// 使用解析工厂填充 <see cref="EntityInfo"/> 与 <see cref="Fields"/>。
        /// </summary>
        public void init(IEntityAnalyseFactory parser) { 
        
            var entity= parser.doAnalyse(entityType);
            if (entity == null) {
                throw new Exception("实体类的数据库映射信息解析失败！");
            }
            init(entity);
        }

        /// <summary>
        /// 使用已构建的 <see cref="EntityInfo"/>（如源生成工厂）填充字段字典。
        /// </summary>
        public void init(EntityInfo entity)
        {
            if (entity == null)
                throw new Exception("实体类的数据库映射信息解析失败！");
            this.EntityInfo = entity;
            Fields = new Dictionary<string, EntityColumn>(StringComparer.Ordinal);
            foreach (var col in entity.Columns)
            {
                if (col == null) continue;
                var key = col.PropertyName;
                if (string.IsNullOrEmpty(key))
                    key = col.PropertyInfo?.Name;
                if (string.IsNullOrEmpty(key))
                    continue;
                Fields[key] = col;
            }
        }
    }
}
