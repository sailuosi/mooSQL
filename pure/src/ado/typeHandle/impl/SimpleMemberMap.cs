using System;
using System.Reflection;

namespace mooSQL.data
{
    /// <summary>
    /// 表示目标参数、属性或字段到源 DataReader 列的简单成员映射
    /// </summary>
    internal sealed class SimpleMemberMap : IPropertyMap
    {
        /// <summary>
        /// 为简单属性映射创建实例
        /// </summary>
        /// <param name="columnName">DataReader 列名</param>
        /// <param name="property">目标属性</param>
        public SimpleMemberMap(string columnName, PropertyInfo property)
        {
            ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
            Property = property ?? throw new ArgumentNullException(nameof(property));
        }

        /// <summary>
        /// 为简单字段映射创建实例
        /// </summary>
        /// <param name="columnName">DataReader 列名</param>
        /// <param name="field">目标字段</param>
        public SimpleMemberMap(string columnName, FieldInfo field)
        {
            ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
            Field = field ?? throw new ArgumentNullException(nameof(field));
        }

        /// <summary>
        /// 为简单构造函数参数映射创建实例
        /// </summary>
        /// <param name="columnName">DataReader 列名</param>
        /// <param name="parameter">目标构造函数参数</param>
        public SimpleMemberMap(string columnName, ParameterInfo parameter)
        {
            ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
            Parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
        }

        /// <summary>
        /// DataReader 列名
        /// </summary>
        public string ColumnName { get; }

        /// <summary>
        /// 目标成员类型
        /// </summary>
        public Type MemberType => Field?.FieldType ?? Property?.PropertyType ?? Parameter?.ParameterType;

        /// <summary>
        /// 目标属性
        /// </summary>
        public PropertyInfo Property { get; }

        /// <summary>
        /// 目标字段
        /// </summary>
        public FieldInfo Field { get; }

        /// <summary>
        /// 目标构造函数参数
        /// </summary>
        public ParameterInfo Parameter { get; }
    }
}
