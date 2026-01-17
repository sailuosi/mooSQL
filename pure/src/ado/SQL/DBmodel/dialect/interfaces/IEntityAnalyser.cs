/*
 * 为不同的实体关系定义，提供标准接口，以供其个性化自己的操作
 * 比如：efcore特性的实体，按照其特性进行解析；sqlsugar类似；UCML这种继承和自带字段信息属性的，直接从属性中取；
 * 因此，抽象需在支持特性注解的基础上，也支持直接从实体本身上获取映射关系。
 * 在实现上，内置本类自带特性、支持efcore/sqlsugar这2种极为常见的3方库，同时考虑支持UCML的特性
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.Mapping
{
    /// <summary>
    /// 实体基础信息解析基础接口
    /// </summary>
    public interface IEntityAnalyser
    {
        /// <summary>
        /// 能否解析
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        bool CanParse(Type type);
        /// <summary>
        /// 解析某个实体的基础信息
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        EntityInfo ParseEntity(Type entity, EntityInfo info);

        /// <summary>
        /// 解析某个实体的字段关系
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="propertyInfo"></param>
        /// <param name="entityInfo"></param>
        /// <param name="entityColumn"></param>
        /// <returns></returns>
        EntityColumn ParseColumn(Type entity,PropertyInfo propertyInfo,EntityInfo entityInfo, EntityColumn entityColumn);

        /// <summary>
        /// 是否作为失败回滚使用
        /// </summary>
        bool FailBacked { get; }
    }
}
