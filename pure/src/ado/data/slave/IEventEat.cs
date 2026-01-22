using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.slave
{
    /// <summary>
    /// 吃掉吃掉！消化事件
    /// </summary>
    public interface IEventEat
    {
        /// <summary>
        /// 执行吃东西
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        string Eat(ModifyPara e);
        /// <summary>
        /// 汇报工作成果
        /// </summary>
        /// <returns></returns>
        string Report();
    }
}
