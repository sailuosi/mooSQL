using System;
using System.Reflection;

namespace mooSQL.data
{

    /// <summary>
    /// 实现本接口以改变默认的读取字段到属性的映射
    /// </summary>
    public interface ITypeMap
    {
        /// <summary>
        /// 查找最适合的构造器
        /// </summary>
        /// <param name="names">DataReader column names</param>
        /// <param name="types">DataReader column types</param>
        /// <returns>Matching constructor or default one</returns>
        ConstructorInfo FindConstructor(string[] names, Type[] types,PackUp deserializer);


        /// <summary>
        /// Gets mapping for constructor parameter
        /// </summary>
        /// <param name="constructor">Constructor to resolve</param>
        /// <param name="columnName">DataReader column name</param>
        /// <returns>Mapping implementation</returns>
        IPropertyMap GetConstructorParameter(ConstructorInfo constructor, string columnName);

        /// <summary>
        /// Gets member mapping for column
        /// </summary>
        /// <param name="columnName">DataReader column name</param>
        /// <returns>Mapping implementation</returns>
        IPropertyMap GetMember(string columnName);
    }
    
}
