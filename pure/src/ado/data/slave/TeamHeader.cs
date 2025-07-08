using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.slave
{
    /// <summary>
    /// 从执行器中介
    /// </summary>
    public class TeamHeader:IEventEat
    {

        public int position;

        public List<IEventEat> members = new List<IEventEat>();

        public bool Empty { 
            get { return members.Count == 0; }
        }

        /// <summary>
        /// 吃掉事件
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public string Eat(ModifyPara e) {

            var res= new StringBuilder();
            foreach (var member in members) { 
                res.Append(member.Eat(e));
                
            }
            return res.ToString();
        }


    }
}
