using mooSQL.data.clip;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.utils
{
    /// <summary>
    /// 匿名类型工具类
    /// </summary>
    public class AnonyTypeUtil
    {
        /// <summary>
        /// 创建匿名类型实例，并为所有属性赋予默认值（引用类型为null，值类型为其类型的默认值）
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object CreateAnonymousWithDefaults(Type type)
        {
            // 获取所有属性并生成默认值数组
            var properties = type.GetProperties();
            object[] defaultValues = properties.map(p =>
            {
                // 处理可空类型和引用类型
                if (!p.PropertyType.IsValueType || Nullable.GetUnderlyingType(p.PropertyType) != null)
                    return null;

                // 值类型返回默认值
                return Activator.CreateInstance(p.PropertyType);
            }).ToArray();

            // 获取构造函数（匿名类的构造函数参数顺序与属性声明顺序相同）
            ConstructorInfo ctor = type.GetConstructors().First();
            return ctor.Invoke(defaultValues);
        }



        static FrequencyBasedCache<Type, TypeNewEnv> _typeNewCache;

        private static FrequencyBasedCache<Type, TypeNewEnv> TypeNewCache
        {
            get
            {

                if (_typeNewCache == null)
                {
                    _typeNewCache = new FrequencyBasedCache<Type, TypeNewEnv>(TimeSpan.FromMinutes(60));
                }
                return _typeNewCache;
            }
        }

        /// <summary>
        /// 创建类的实例（自动填充默认参数值）
        /// </summary>
        public static T CreateInstanceWithDefaults<T>()
        {
            return (T)CreateInstanceWithDefaults(typeof(T));
        }

        /// <summary>
        /// 创建类的实例（自动填充默认参数值）
        /// </summary>
        public static object CreateInstanceWithDefaults(Type type)
        {
            TypeNewEnv constructor;

            if (TypeNewCache.TryGetValue(type, out constructor))
            {

            }
            else {
                var cts = type.GetConstructors();
                if (cts.Length == 0) {
                    throw new InvalidOperationException($"类型 {type.FullName} 没有构造函数");
                }
                //寻找参数最少的构造函数
                int count = -1;
                ConstructorInfo contru = null;
                foreach (var item in cts) {
                    var p = item.GetParameters();
                    if (count == -1|| p.Length < count) {
                        count = p.Length;
                        contru = item;
                    }
                }
                ParameterInfo[] parameters = contru.GetParameters();
                object[] args = new object[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    args[i] = GetDefaultValue(parameters[i].ParameterType);
                }
                constructor = new TypeNewEnv() {
                    TarType = type,
                    Constructor = contru,
                    Args = args
                };
                TypeNewCache.Add(type, constructor);
            }
            


            return constructor.Constructor.Invoke(constructor.Args);
        }

        private static object GetDefaultValue(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }

    internal class TypeNewEnv { 
        public Type TarType;
        public ConstructorInfo Constructor;
        public object[] Args;
    }
}
