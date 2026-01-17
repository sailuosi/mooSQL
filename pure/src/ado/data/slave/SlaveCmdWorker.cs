using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace mooSQL.data.slave
{
    /// <summary>
    ///  打工人，只能吃苦。
    /// </summary>
    public class SlaveCmdWorker:IEventEat
    {

        public SlaveCmdWorker() {
            Ticker = new System.Timers.Timer(200);
            Ticker.Elapsed += EatingSQL;
            Ticker.AutoReset = true;
        }

        /// <summary>
        /// 待执行的任务
        /// </summary>
        public Queue<SQLCmd> cmds =new Queue<SQLCmd>();
        public DBInstance DB;
        /// <summary>
        /// 发生异常时的动作，由业务侧自定义
        /// </summary>
        public Func<SQLCmd, IEventEat, string> errFunction;

        private System.Timers.Timer Ticker;

        public bool stoped = true;

        /// <summary>
        /// 吃苦！准备要消化掉某个事件
        /// </summary>
        /// <param name="misery"></param>
        /// <returns></returns>
        public string Eat(ModifyPara misery) {

            cmds.Enqueue(misery.cmd);

            if (stoped) { 
                stoped = false;
                Ticker.Enabled = true;
            }

            return "goted.";
        }

        private void reportErr(SQLCmd cmd) {
            if (errFunction != null) { 
                errFunction(cmd,this);
            }
        }

        private void EatingSQL(object source, ElapsedEventArgs e) {
            
            Ticker.Enabled = false;

            if (cmds.Count == 0) { 
                stoped=true;
                return;
            }
            var cmd = cmds.Dequeue();
            try { 

                DB.ExeNonQuery(cmd);

                //执行完毕

            }
            catch(Exception err) {
                reportErr(cmd);
            }
            //准备干下一个工作
            Ticker.Enabled = true;

        }
    }
}
