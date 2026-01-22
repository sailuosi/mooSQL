using System;
using System.Reflection;

namespace mooSQL.data
{

    /// <summary>
    /// 自定义属性映射
    /// </summary>
    public interface IPropertyMap
    {
        /// <summary>
        /// 来源字段名
        /// </summary>
        string ColumnName { get; }

        /// <summary>
        /// 目标类型
        /// </summary>
        Type MemberType { get; }

        /// <summary>
        /// 目标属性
        /// </summary>
        PropertyInfo Property { get; }

        /// <summary>
        /// 目标字段
        /// </summary>
        FieldInfo Field { get; }

        /// <summary>
        /// 目标构造器参数
        /// </summary>
        ParameterInfo Parameter { get; }
    }
    
}
