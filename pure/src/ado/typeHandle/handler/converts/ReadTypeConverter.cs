using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mooSQL.utils;

namespace mooSQL.data.reader
{
    /// <summary>
    /// 读取类型转换器，提供类型转换的静态方法
    /// </summary>
    public static class ReadTypeConverter
    {
        public static R StringToGuid<T,R>(T s,DBInstance db)
        {
            var valType = typeof(R);
            var utype = valType;
            if (valType.IsNullable() && valType.GetGenericArguments()[0] == typeof(Guid))
            {
                utype = valType.UnwrapNullable();
            }

            if (typeof(T) == typeof(string) && utype == typeof(Guid))
            {
                var val = s as string;
                if (RegxUntils.isGUID(val))
                {
                    return (R)(object)Guid.Parse(val);
                }
                if (valType.IsNullable())
                {
                    return (R)(object)null;
                }
                return default;
            }
                //throw new InvalidOperationException("泛型类型不匹配：T需为string，R需为Guid");
                return default;
            /*
            if (RegxUntils.isGUID(s)) {
                return Guid.Parse(s);
            }
            
            return Guid.Empty;*/
             // 或者使用 Guid.TryParse 来处理可能的转换失败
        }
        public static R ByteArrToString<T, R>(T s, DBInstance db)
        {
            var valType = typeof(R);
            var utype = valType;
            if (valType.IsNullable() && valType.GetGenericArguments()[0] == typeof(string))
            {
                utype = valType.UnwrapNullable();
            }

            if (typeof(T) == typeof(byte[]) && utype == typeof(string))
            {
                var val = s as byte[];
                var v= Encoding.UTF8.GetString(val);
                return (R)(object)v;
            }
            //throw new InvalidOperationException("泛型类型不匹配：T需为string，R需为Guid");
            return default;

        }
        public static object ChangeType(object value, Type conversionType, IFormatProvider provider) { 
            return System.Convert.ChangeType(value, conversionType, provider);
        
        }

        public static R Convert<T, R>(params object[] values)
        {
            //if (typeof(T) == typeof(string) && typeof(R) == typeof(Guid))
            //{
            //    if (Guid.TryParse(value as string, out Guid result))
            //    {
            //        return (R)(object)result;
            //    }
            //    //throw new InvalidCastException("无法将字符串转换为Guid");
            //}
            //throw new InvalidOperationException("泛型类型不匹配：T需为string，R需为Guid");
            return default;
        }
    }


    /// <summary>
    /// 类型转换读取器，提供类型转换的实例方法
    /// </summary>
    public class TypeChangeReader
    {
        private DBInstance db;  
        public TypeChangeReader(DBInstance db) { 
                this.db = db;
        }


        public R Convert<T, R>(T value)
        {
            var valType = typeof(R);
            var utype = valType;
            if (valType.IsNullable() && valType.GetGenericArguments()[0] == typeof(Guid)) {
                utype = valType.UnwrapNullable();
            }

            if (typeof(T) == typeof(string) && utype == typeof(Guid))
            {
                var val = value as string;
                if (RegxUntils.isGUID(val))
                {
                    return (R)(object)Guid.Parse(val);
                }
                if (valType.IsNullable()) { 
                    return (R)(object)null;
                }
                return default;
            }
            //throw new InvalidOperationException("泛型类型不匹配：T需为string，R需为Guid");
            return default;
        }
    }
}
