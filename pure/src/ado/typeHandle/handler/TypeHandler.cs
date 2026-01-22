using System;
using System.Data;

namespace mooSQL.data
{

    /// <summary>
    /// 简单类型处理器的基类
    /// </summary>
    /// <typeparam name="T">此处理器针对的 <see cref="Type"/>。</typeparam>
    public abstract class TypeHandler<T> : ITypeParser
    {
        /// <summary>
        /// 在命令执行前分配参数值
        /// </summary>
        /// <param name="parameter">要配置的参数</param>
        /// <param name="value">参数值</param>
        public abstract void SetValue(IDbDataParameter parameter, T value);

        /// <summary>
        /// 将数据库值解析回类型化值
        /// </summary>
        /// <param name="value">来自数据库的值</param>
        /// <returns>类型化值</returns>
        public abstract T Parse(object value);



        object ITypeParser.Parse(Type destinationType, object value)
        {
            return Parse(value);
        }
    }

    /// <summary>
    /// 基于字符串的简单类型处理器的基类
    /// </summary>
    /// <typeparam name="T">此处理器针对的 <see cref="Type"/>。</typeparam>
    public abstract class StringTypeHandler<T> : TypeHandler<T>
    {
        /// <summary>
        /// 将字符串解析为预期类型（字符串永远不会为 null）
        /// </summary>
        /// <param name="xml">要解析的字符串。</param>
        protected abstract T Parse(string xml);

        /// <summary>
        /// 将实例格式化为字符串（实例永远不会为 null）
        /// </summary>
        /// <param name="xml">要格式化的字符串。</param>
        protected abstract string Format(T xml);

        /// <summary>
        /// 在命令执行前分配参数值
        /// </summary>
        /// <param name="parameter">要配置的参数</param>
        /// <param name="value">参数值</param>
        public override void SetValue(IDbDataParameter parameter, T value)
        {
            parameter.Value = value == null ? (object)DBNull.Value : Format(value);
        }

        /// <summary>
        /// 将数据库值解析回类型化值
        /// </summary>
        /// <param name="value">来自数据库的值</param>
        /// <returns>类型化值</returns>
        public override T Parse(object value)
        {
            if (value is null || value is DBNull) return default;
            return Parse((string)value);
        }
    }
    
}
