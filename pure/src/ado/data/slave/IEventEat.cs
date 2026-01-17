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

        string Eat(ModifyPara e);
    }
}
