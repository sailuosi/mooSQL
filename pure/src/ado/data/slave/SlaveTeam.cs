using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

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
        protected Dictionary<int, TeamHeader> teams = new Dictionary<int, TeamHeader>();



        /// <summary>
        /// 信令
        /// </summary>
        public string Signal { get; set; }
        /// <summary>
        /// 唯一编码
        /// </summary>
        public string Code { get; set; }
        private System.Timers.Timer Ticker;
        /// <summary>
        /// 创建无信令的队伍
        /// </summary>
        /// <param name="code"></param>
        public SlaveTeam(string code) {
            this.Code = code;
            //半分钟执行一次检查
            Ticker = new System.Timers.Timer(30000);
            Ticker.Elapsed += EatingSQL;
            Ticker.AutoReset = true;
        }
        /// <summary>
        /// 定时检查工作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EatingSQL(object sender, ElapsedEventArgs e)
        {
            foreach(var team in teams.Values)
            {
                team.Report();
            }
        }
        /// <summary>
        /// 创建含有信令的队伍
        /// </summary>
        /// <param name="code"></param>
        /// <param name="signal"></param>
        public SlaveTeam(string code,string signal)
        {
            this.Code = code;
            this.Signal = signal;
        }
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
        /// <summary>
        /// 监听修改事件
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public string ListenTo(ModifyPara e)
        {

            if (e.type != ModifyEventType.Modify) return null;

            var pos = e.position;
            if (hasTeam(pos) == false) {
                return string.Empty;
            }
            if (!string.IsNullOrWhiteSpace(this.Signal)) {
                if (e.cmd.signal != this.Signal) {
                    return string.Empty;
                }
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
        public void sign(int position, List<DBInstance> slaves, Func<ModifyPara, IEventEat, string> onErr=null) { 
            
            if(slaves==null|| slaves.Count == 0) return;
            
            var tar=SlaveFactory.createTeam(position, slaves);

            teams[position] = tar;
        }
    }
}
