using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{
    /// <summary>
    /// 构造工具
    /// </summary>
    public class CallUntil
    {
        /// <summary>
        /// 根据方法名，创建代表该方法的语法类
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>


        public static MethodCall CreateCall(MethodCallExpression method)
        {
            
            var name= method.Method.Name;
            var tar= MethodCallFactory.Create(name);
            if (tar != null) { 
                tar.MethodInfo = method.Method;
                tar.callExpression = method;
                tar.Arguments = method.Arguments;
                return tar;
            }
            return null;
        }
    }
    /// <summary>
    /// 方法调用工厂类，用于创建不同类型的MethodCall对象。采用表达式和缓存技术，提高性能。
    /// </summary>
    public static class MethodCallFactory
    {
        // 缓存已编译的委托
        private static readonly ConcurrentDictionary<string, Delegate> _cache = new();

        /// <summary>
        /// 根据方法名创建对应的MethodCall对象。
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static MethodCall Create(string type)
        {
            if (!_cache.TryGetValue(type, out var func))
            {
                // 构建表达式树：new T()
                var t= GetCallType(type);
                var newExpr = Expression.New(t);
                var lambda = Expression.Lambda<Func<MethodCall>>(
                    newExpr
                );
                func = lambda.Compile();
                _cache[type] = func;
            }
            return ((Func<MethodCall>)func)();
        }
        /// <summary>
        /// 根据方法名获取对应的MethodCall类型。
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static Type GetCallType(string name)
        {
            try
            {
                var namefix = name + "Call";
                var fullName = "mooSQL.data.call." + namefix;

                var type = Type.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }
            catch (Exception e)
            {
                throw new NotImplementedException("尚不支持方法" + name + "的解析");
            }
            return null;
        }
    }
}
