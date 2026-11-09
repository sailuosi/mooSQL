using System;

namespace mooSQL.data
{
    public partial class EntityColumn
    {
        /// <summary>
        /// 经 Client 注册的 Accessor（或反射回退）读取实体成员值。
        /// </summary>
        public object GetEntityValue(object entity, MooClient client)
        {
            if (entity is null) return null;
            var name = PropertyName ?? PropertyInfo?.Name;
            if (string.IsNullOrEmpty(name))
                return PropertyInfo?.GetValue(entity);

            var type = belongTable?.Type ?? entity.GetType();
            return EntityAccessorResolver.Resolve(client, type).ReadValue(entity, name);
        }

        /// <summary>
        /// 经 Client 注册的 Accessor（或反射回退）写入实体成员值。
        /// </summary>
        public void SetEntityValue(object entity, object value, MooClient client)
        {
            if (entity is null) return;
            var name = PropertyName ?? PropertyInfo?.Name;
            if (string.IsNullOrEmpty(name))
            {
                PropertyInfo?.SetValue(entity, value);
                return;
            }

            var type = belongTable?.Type ?? entity.GetType();
            EntityAccessorResolver.Resolve(client, type).WriteValue(entity, name, value);
        }

        /// <summary>
        /// 成员值是否为 null（对齐 Save/Upsert 主键短路）。
        /// </summary>
        public bool IsEntityValueNull(object entity, MooClient client)
            => GetEntityValue(entity, client) == null;
    }
}
