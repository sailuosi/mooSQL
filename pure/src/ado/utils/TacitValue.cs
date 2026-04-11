using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.utils
{
    /// <summary>
    /// 默认值
    /// </summary>
    public static class TacitValue
    {
        static TacitValue()
        {
            _values[typeof(int)] = default(int);
            _values[typeof(uint)] = default(uint);
            _values[typeof(byte)] = default(byte);
            _values[typeof(char)] = default(char);
            _values[typeof(bool)] = default(bool);
            _values[typeof(sbyte)] = default(sbyte);
            _values[typeof(short)] = default(short);
            _values[typeof(long)] = default(long);
            _values[typeof(ushort)] = default(ushort);
            _values[typeof(ulong)] = default(ulong);
            _values[typeof(float)] = default(float);
            _values[typeof(double)] = default(double);
            _values[typeof(decimal)] = default(decimal);
            _values[typeof(DateTime)] = default(DateTime);
            _values[typeof(TimeSpan)] = default(TimeSpan);
            _values[typeof(DateTimeOffset)] = default(DateTimeOffset);
            _values[typeof(Guid)] = default(Guid);
            _values[typeof(string)] = default(string);
        }

        static readonly ConcurrentDictionary<Type, object?> _values = new ConcurrentDictionary<Type, object?>();


        /// <summary>
        /// 获取默认值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetValue<T>()
        {
            if (_values.TryGetValue(typeof(T), out var value))
                return (T)value!;

            _values[typeof(T)] = default(T)!;

            return default(T)!;
        }
        /// <summary>
        /// 按运行时类型从内部字典读取已缓存的隐式值；未设置时返回 null。
        /// </summary>
        /// <param name="type">目标类型。</param>
        /// <returns>已存储的对象；不存在则为 null。</returns>
        public static object GetValue(Type type)
        {
            if (_values.TryGetValue(type, out var value))
                return value!;

            return null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        public static void SetValue<T>(T value)
        {
            _values[typeof(T)] = value;
        }
    }


    /// <summary>
    /// 默认值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class TacitValue<T>
    {
        static T _value = TacitValue.GetValue<T>();

        /// <summary>
        /// 默认值
        /// </summary>
        public static T Value
        {
            get => _value;
            set => TacitValue.SetValue(_value = value);
        }
    }
}
