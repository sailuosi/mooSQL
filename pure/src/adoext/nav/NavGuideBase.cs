using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 实体导航（查询加载、层级保存）的轻量基类，持有用于生成 SQL 的 <see cref="SQLBuilder"/>。
    /// </summary>
    public class NavGuideBase
    {
        /// <summary>
        /// 当前关联的 SQL 构建器，用于子类发起查询或沿用同一数据库上下文。
        /// </summary>
        public SQLBuilder Builder {  get; set; }

    }

    /// <summary>
    /// 带主实体集合的导航基类，作为多级导航与导航保存的起点数据容器。
    /// </summary>
    /// <typeparam name="T">主实体（或当前层级实体）类型。</typeparam>
    public class NavGuideBase<T> : NavGuideBase { 
        
        /// <summary>
        /// 当前导航层级对应的主数据列表（例如一次查询得到的主表行集合）。
        /// </summary>
        public IEnumerable<T> MainList { get; set; }


        /// <summary>
        /// 使用 SQL 构建器与主列表初始化导航上下文。
        /// </summary>
        /// <param name="builder">SQL 构建器，需与目标库、方言一致。</param>
        /// <param name="mainList">本层级要参与导航填充或保存的实体集合。</param>
        public NavGuideBase(SQLBuilder builder, IEnumerable<T> mainList)
        {
            this.Builder = builder;
            this.MainList = mainList;
        }
    }
}
