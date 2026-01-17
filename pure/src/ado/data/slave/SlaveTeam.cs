using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.slave
{
    /// <summary>
    /// 主从功能团队
    /// </summary>
    public class SlaveTeam:IModifyListener
    {

        /// <summary>
        /// 代表库的主从关系
        /// </summary>
        public Dictionary<int, TeamHeader> teams = new Dictionary<int, TeamHeader>();


        /// <summary>
        /// 检查某个连接位是否拥有从库
        /// </summary>
        /// <param name="teamId"></param>
        /// <returns></returns>
        public bool hasTeam(int teamId) { 
            if(teams.ContainsKey(teamId)==false) return false;
            if (teams[teamId].Empty) return false;
            return true;
        }

        public string ListenTo(ModifyPara e)
        {

            if (e.type != ModifyEventType.Modify) return null;

            var pos = e.position;
            if (hasTeam(pos) == false) {
                return null;
            }

            var team= teams[pos];
            team.Eat(e);

            return "invoked";
        }


        /// <summary>
        /// 注册从库，重复时覆盖，暴露给业务侧使用
        /// </summary>
        /// <param name="position"></param>
        /// <param name="slaves"></param>
        public void sign(int position, List<DBInstance> slaves, Func<SQLCmd, IEventEat, string> onErr=null) { 
            
            if(slaves==null|| slaves.Count == 0) return;
            
            var tar=SlaveFactory.createTeam(position, slaves);

            teams[position] = tar;
        }
    }
}
