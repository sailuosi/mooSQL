using System;
using System.Data.Common;
using System.Globalization;

namespace mooSQL.data
{
    public partial class PackUp
    {
        /// <summary>
        /// 是否启用 AOT 物化（读取关联 MooClient.EnableAot）。
        /// </summary>
        internal bool IsAotEnabled => _client?.EnableAot ?? false;

        internal bool UseGetFieldValueFor(Type type) => UseGetFieldValue(type);

        internal object ParseUntyped(Type type, object value)
        {
            if (value is null || value is DBNull) return null;
            if (type.IsInstanceOfType(value)) return value;

            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type.IsEnum)
            {
                if (value is float || value is double || value is decimal)
                    value = Convert.ChangeType(value, Enum.GetUnderlyingType(type), CultureInfo.InvariantCulture);
                return Enum.ToObject(type, value);
            }
            if (typeHandlers.TryGetValue(type, out var handler))
                return handler.Parse(type, value);
            return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 按 Client 配置选择 Emit 或 AOT 物化器。
        /// </summary>
        internal Func<DbDataReader, DBInstance, object> GetTypeMaterializer(
            Type type,
            DbDataReader reader,
            int startBound = 0,
            int length = -1,
            bool returnNullIfFirstMissing = false,
            DBInstance db = null)
        {
            if (!IsAotEnabled)
                return GetTypePackImpl(type, reader, startBound, length, returnNullIfFirstMissing, db);

            return MaterializerResolver.Resolve(this, type, reader, startBound, length, returnNullIfFirstMissing);
        }
    }
}
