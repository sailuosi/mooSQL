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

        /// <summary>
        /// 构造函数。
        /// </summary>
        public SlaveCmdWorker() {

        }


        /// <summary>
        /// 属性 DBLive（DBInstance）。
        /// </summary>
        public DBInstance DBLive{ get; set; }
        /// <summary>
        /// 发生异常时的动作，由业务侧自定义
        /// </summary>
        public Func<ModifyPara, IEventEat, string> errFunction;



        /// <summary>
        /// 字段 stoped（bool）。
        /// </summary>
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

        /// <summary>
        /// Report 方法（返回 string）。
        /// </summary>
        public string Report()
        {
            return "";
        }
    }
}