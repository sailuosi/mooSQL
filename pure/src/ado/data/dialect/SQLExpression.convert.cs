using System;
using System.Collections.Generic;
using mooSQL.data.model;

namespace mooSQL.data
{
    /// <summary>
    /// 参数值按物理类型 / 实体列元数据转换（tryParse）。
    /// </summary>
    public abstract partial class SQLExpression
    {
        /// <summary>按 <see cref="DataFam"/> 转换参数；成功写出 converted。</summary>
        public delegate bool TryConvertHandler(object value, out object converted);

        readonly Dictionary<DataFam, TryConvertHandler> _parameterConverters =
            new Dictionary<DataFam, TryConvertHandler>();

        /// <summary>
        /// 注册物理类型转换器（后写覆盖）。基类默认不注册。
        /// </summary>
        protected void RegisterParameterConverter(DataFam dataFam, TryConvertHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            if (dataFam == DataFam.Undefined)
                throw new ArgumentException("Cannot register converter for Undefined.", nameof(dataFam));
            _parameterConverters[dataFam] = handler;
        }

        /// <summary>
        /// 按物理类型转换参数值（严格 tryParse）。无转换器则透传成功。
        /// </summary>
        public virtual bool TryConvertParameter(DataFam dataFam, object value, out object converted)
        {
            converted = value;
            if (value == null || value == DBNull.Value)
                return true;
            if (dataFam == DataFam.Undefined)
                return true;
            if (!_parameterConverters.TryGetValue(dataFam, out var handler))
                return true;
            return handler(value, out converted);
        }

        /// <summary>
        /// 列有效物理类型：优先 <see cref="DBLive"/> 登记，否则 DataType / CLR Guid。
        /// </summary>
        public virtual DataFam GetEffectiveDataFam(EntityColumn col)
        {
            if (DBLive != null)
                return DBLive.GetEffectiveDataFam(col);
            if (col == null)
                return DataFam.Undefined;
            if (col.DataType != DataFam.Undefined)
                return col.DataType;
            var under = col.UnderType;
            if (under == typeof(Guid) || under == typeof(Guid?))
                return DataFam.Guid;
            return DataFam.Undefined;
        }

        /// <summary>
        /// 按实体列转换：枚举映射 + 有效物理类型 TryConvert。
        /// 基类乐观：底层失败时回写原值（或枚举结果）并返回 true。
        /// </summary>
        public virtual bool TryConvertColumnValue(
            EntityColumn col,
            object value,
            out object converted,
            out DataFam effectiveFam)
        {
            effectiveFam = GetEffectiveDataFam(col);
            converted = ApplyEnumColumnMapping(value, col);

            if (!TryConvertParameter(effectiveFam, converted, out var typed))
            {
                // 乐观：不抛，交回可用值
                converted = converted ?? value;
                return true;
            }

            converted = typed;
            return true;
        }

        /// <summary>
        /// 枚举 → 字符名或底层数值；非枚举原样返回。
        /// </summary>
        protected virtual object ApplyEnumColumnMapping(object value, EntityColumn col)
        {
            if (value == null) return null;
            if (value is not Enum) return value;

            var fam = EnumColumnMapping.GetEffectiveDataType(col);
            if (EnumColumnMapping.HasCustomDataType(col) && EnumColumnMapping.IsCharacterDataType(fam))
                return value.ToString();

            return EnumColumnMapping.ToUnderlyingNumeric(value);
        }

        /// <summary>
        /// 乐观列转换：忽略成败，返回转换值或回退后的可用值（供仓储等不关心 bool 的调用方）。
        /// </summary>
        public object ConvertColumnValue(EntityColumn col, object value)
        {
            TryConvertColumnValue(col, value, out var converted, out _);
            return converted;
        }

        /// <summary>
        /// Guid 规范（实例方法，供转换器注册使用）。
        /// </summary>
        protected virtual bool TryCoerceGuid(object value, out Guid guid)
        {
            guid = default;
            if (value == null || value == DBNull.Value)
                return false;
            if (value is Guid g)
            {
                guid = g;
                return true;
            }
            return Guid.TryParse(Convert.ToString(value), out guid);
        }

        /// <summary>
        /// Guid 转换器：成功写出 Guid，失败 false。
        /// </summary>
        protected bool TryConvertGuidParameter(object value, out object converted)
        {
            converted = value;
            if (!TryCoerceGuid(value, out var g))
                return false;
            converted = g;
            return true;
        }
    }
}
