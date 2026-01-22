using System;
using System.Data;

namespace mooSQL.data
{

    /// <summary>
    /// 用于底层提供程序支持但需要指定已知 UdtTypeName 的数据类型的类型处理器
    /// </summary>
    public class UdtTypeHandler : ITypeParser
    {
        private readonly string udtTypeName;
        /// <summary>
        /// 使用指定的 <see cref="UdtTypeHandler"/> 创建 UdtTypeHandler 的新实例。
        /// </summary>
        /// <param name="udtTypeName">用户定义的类型名称。</param>
        public UdtTypeHandler(string udtTypeName)
        {
            if (string.IsNullOrEmpty(udtTypeName)) throw new ArgumentException("Cannot be null or empty", udtTypeName);
            this.udtTypeName = udtTypeName;
        }

        object ITypeParser.Parse(Type destinationType, object value)
        {
            return value is DBNull ? null : value;
        }


    }
    
}
