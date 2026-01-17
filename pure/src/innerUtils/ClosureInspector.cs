using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.utils
{
    /// <summary>
    /// 闭包访问类
    /// </summary>
    public static class ClosureInspector
    {
        private static readonly Dictionary<Type, Func<object, Dictionary<string, object>>> _accessorCache = new();
        /// <summary>
        /// 获取失败时返回null，成功时返回闭包实例的字段字典
        /// </summary>
        /// <param name="closureInstance"></param>
        /// <returns></returns>
        public static Dictionary<string, object> GetValue(object closureInstance)
        {
            if (closureInstance == null) return null;

            var type = closureInstance.GetType();
            if (!IsClosureClass(type)) return null;

            if (!_accessorCache.TryGetValue(type, out var accessor))
            {
                accessor = BuildAccessor(type);
                _accessorCache[type] = accessor;
            }

            return accessor(closureInstance);
        }
        /// <summary>
        /// 获取闭包成员字典，默认已经检查过是否为闭包类，无需再次检查
        /// </summary>
        /// <param name="closureInstance"></param>
        /// <returns></returns>
        public static Dictionary<string, object> GetValueN(object closureInstance)
        {
            if (closureInstance == null) return null;

            var type = closureInstance.GetType();
            if (!_accessorCache.TryGetValue(type, out var accessor))
            {
                accessor = BuildAccessor(type);
                _accessorCache[type] = accessor;
            }

            return accessor(closureInstance);
        }
        /// <summary>
        /// 获取闭包成员，默认已经检查过是否为闭包类，无需再次检查
        /// </summary>
        /// <param name="closureInstance"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static object GetFieldValueN(object closureInstance, string field) { 
            var dict = GetValueN(closureInstance);
            if (dict == null) return null;
            if (dict.ContainsKey(field)) { 
                return dict[field];
            }
            return null;
        }

        private static Func<object, Dictionary<string, object>> BuildAccessor(Type closureType)
        {
            var fields = closureType.GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var param = Expression.Parameter(typeof(object));
            var instance = Expression.Convert(param, closureType);
            var dict = Expression.Variable(typeof(Dictionary<string, object>));

            var expressions = new List<Expression>
        {
            Expression.Assign(dict, Expression.New(typeof(Dictionary<string, object>)))
        };

            foreach (var field in fields)
            {
                expressions.Add(Expression.Call(
                    dict,
                    typeof(Dictionary<string, object>).GetMethod("Add"),
                    Expression.Constant(field.Name),
                    Expression.Convert(Expression.Field(instance, field), typeof(object))
                ));
            }

            expressions.Add(dict);

            var block = Expression.Block(
                new[] { dict },
                expressions
            );

            return Expression.Lambda<Func<object, Dictionary<string, object>>>(block, param).Compile();
        }

        public static bool IsClosureClass(Type type) =>
            type.IsClass &&
            type.IsSealed &&
            Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute)) &&
            (type.Name.Contains("DisplayClass") || type.Name.Contains("Closure"));
    }
}
