using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 导航处理基类
    /// </summary>
    public class NavGuideBase
    {
        /// <summary>
        /// 执行器
        /// </summary>
        public SQLBuilder Builder {  get; set; }

    }

    /// <summary>
    /// 导航基础
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class NavGuideBase<T> : NavGuideBase { 
        
        /// <summary>
        /// 主列表
        /// </summary>
        public IEnumerable<T> MainList { get; set; }


        /// <summary>
        /// 导航器
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="mainList"></param>
        public NavGuideBase(SQLBuilder builder, IEnumerable<T> mainList)
        {
            this.Builder = builder;
            this.MainList = mainList;
        }
    }
}
