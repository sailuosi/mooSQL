namespace mooSQL.data
{
    /// <summary>
    /// 写侧实体成员访问器（Get/Set/IsNull）。由源生成或反射实现，经 <see cref="MooClient"/> 注册后使用。
    /// </summary>
    public interface IEntityTypeAccessor
    {
        /// <summary>按 CLR 属性名读取实体成员值。</summary>
        object GetValue(object entity, string propertyName);

        /// <summary>按 CLR 属性名写入实体成员值。</summary>
        void SetValue(object entity, string propertyName, object value);

        /// <summary>成员值是否为 null（对齐 Save/Upsert 主键短路：仅引用类型 null）。</summary>
        bool IsNull(object entity, string propertyName);
    }
}
