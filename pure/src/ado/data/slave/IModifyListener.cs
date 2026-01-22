using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.slave
{
    /// <summary>
    /// 啥？没听清！告诉我！
    /// </summary>
    public interface IModifyListener
    {
        /// <summary>
        /// 唯一编码，用于在注册时判重
        /// </summary>
        string Code { get; }

        /// <summary>
        /// 听！
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        string ListenTo(ModifyPara e);

    }

}
