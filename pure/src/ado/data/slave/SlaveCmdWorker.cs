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

        }


        public DBInstance DBLive{ get; set; }
        /// <summary>
        /// 发生异常时的动作，由业务侧自定义
        /// </summary>
        public Func<ModifyPara, IEventEat, string> errFunction;



        public bool stoped = true;

        /// <summary>
        /// 吃苦！准备要消化掉某个事件
        /// </summary>
        /// <param name="misery"></param>
        /// <returns></returns>
        public string Eat(ModifyPara misery) {
            EatingSQL(misery);

            return "goted.";
        }

        private void EatingSQL(ModifyPara para) {
            

            try { 

                DBLive.ExeNonQuery(para.cmd);
                //执行完毕
            }
            catch(Exception err) {
                if (errFunction != null)
                {
                    errFunction(para, this);
                }
            }


        }

        public string Report()
        {
            return "";
        }
    }
}
