using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.Mapping
{
    /// <summary>
    /// 实体类解析抽象工厂
    /// </summary>
    public interface IEntityAnalyseFactory
    {
        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="entityAnalyser"></param>
        /// <returns></returns>
        IEntityAnalyseFactory register(IEntityAnalyser entityAnalyser);

        /// <summary>
        /// 解析
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        EntityInfo doAnalyse(Type entityType);
    }
}
