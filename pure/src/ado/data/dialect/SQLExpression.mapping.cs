using mooSQL.data.model;

namespace mooSQL.data
{
    public abstract partial class SQLExpression
    {
        /// <summary>
        /// 按列元数据变换写入/条件参数值（枚举 → 底层数值或字符名）。非枚举原样返回。
        /// 供 SQLClip 条件与实体 insert/update/delete 写路径共用；方言可 override。
        /// </summary>
        public virtual object ResolveColumnValue(object value, EntityColumn col)
        {
            if (value == null) return null;
            if (value is not Enum) return value;

            var fam = EnumColumnMapping.GetEffectiveDataType(col);
            if (EnumColumnMapping.HasCustomDataType(col) && EnumColumnMapping.IsCharacterDataType(fam))
                return value.ToString();

            return EnumColumnMapping.ToUnderlyingNumeric(value);
        }
    }
}
