using System;
using System.Data;

namespace mooSQL.data
{

    /// <summary>
    /// 实现本接口，以自定义参数类型和值解析
    /// </summary>
    public interface ITypeParser
    {

        /// <summary>
        /// 将一个数据库值转换为指定类型
        /// </summary>
        /// <param name="value">数据库执行</param>
        /// <param name="destinationType">要转换为的类型</param>
        /// <returns>The typed value</returns>
        object Parse(Type destinationType, object value);
    }
    
}
