using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.mapping
{
    public static class TypeConverterUtil
    {
        // 基础类型转换方法
        public static T Convert<T>(object value)
        {
            return Convert<T>(value, CultureInfo.CurrentCulture);
        }
        public static object Convert(object value,Type tar ,CultureInfo culture)
        {
            if (value == null || value == DBNull.Value)
                return default;

            if (value.GetType()== tar )
                return value;

            try
            {
                // 尝试使用TypeConverter
                TypeConverter converter = TypeDescriptor.GetConverter(tar);
                if (converter != null && converter.CanConvertFrom(value.GetType()))
                    return converter.ConvertFrom(null, culture, value);

                // 尝试使用IConvertible接口
                if (value is IConvertible)
                    return System.Convert.ChangeType(value, tar, culture);

                // 最后尝试强制转换
                return value ;
            }
            catch (Exception ex)
            {
                throw new InvalidCastException(
                    $"Cannot convert {value.GetType().Name} to {tar.Name}", ex);
            }
        }
        // 带文化信息的转换方法
        public static T Convert<T>(object value, CultureInfo culture)
        {
            if (value == null || value == DBNull.Value)
                return default(T);

            if (value is T typedValue)
                return typedValue;

            try
            {
                // 尝试使用TypeConverter
                TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter != null && converter.CanConvertFrom(value.GetType()))
                    return (T)converter.ConvertFrom(null, culture, value);

                // 尝试使用IConvertible接口
                if (value is IConvertible)
                    return (T)System.Convert.ChangeType(value, typeof(T), culture);

                // 最后尝试强制转换
                return (T)value;
            }
            catch (Exception ex)
            {
                throw new InvalidCastException(
                    $"Cannot convert {value.GetType().Name} to {typeof(T).Name}", ex);
            }
        }

        // 安全转换方法（转换失败返回默认值）
        public static T TryConvert<T>(object value, T defaultValue = default(T))
        {
            try
            {
                return Convert<T>(value);
            }
            catch
            {
                return defaultValue;
            }
        }

        // 特殊处理Nullable类型
        public static T? ConvertNullable<T>(object value) where T : struct
        {
            if (value == null || value == DBNull.Value)
                return null;

            return Convert<T>(value);
        }

        public static LambdaExpression GetConvertType(DBInstance DB, Type src, Type to)
        {

            var func = DB.dialect.mapping.GetValueConverter(src, to);

            var para = Expression.Parameter(src, "p");
            var call = Expression.Call(
                Expression.Constant(func), func.Method, para
                );
            var res = Expression.Lambda(call, para);
            return res;
        }
    }
}
