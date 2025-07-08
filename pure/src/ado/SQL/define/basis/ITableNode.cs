using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model
{


    /// <summary>
    /// 代表一个表的SQL表达式
    /// </summary>
    public interface ITableNode : ISQLNode
    {

        /// <summary>
        /// 表的名称
        /// </summary>
        string Name { get; }
        /// <summary>
        /// 全部字段
        /// </summary>
        FieldWord All { get; }
        int SourceID { get; }
        /// <summary>
        /// 表类型
        /// </summary>
        SqlTableType SqlTableType { get; }
        /// <summary>
        /// 获取注解
        /// </summary>
        /// <param name="allIfEmpty"></param>
        /// <returns></returns>
        IList<IExpWord>? GetKeys(bool allIfEmpty);
    }

}
