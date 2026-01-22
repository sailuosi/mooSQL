using System;
using System.Collections.Concurrent;
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

        private BlockingCollection<ModifyPara> bc;

        public bool Empty { 
            get { return members.Count == 0; }
        }

        private bool working = false;
        private readonly object _lockObject = new object();
        /// <summary>
        /// 吃掉事件
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public string Eat(ModifyPara e) {
            lock (_lockObject) { 
                if (bc == null) { 
                    bc = new BlockingCollection<ModifyPara>();
                }
                bc.Add(e);
                bc.CompleteAdding();
                startWorking();            
            }

            return "1";
        }
        /// <summary>
        /// 做工作汇报
        /// </summary>
        public string Report() {
            lock (_lockObject)
            { 
                if (this.working) {
                    return string.Empty;
                }
                if( bc!=null && bc.Count>0)
                {
                    startWorking();
                    return "working started.";
                }
                if ( bc != null && bc.Count == 0)
                {
                    bc.Dispose();
                    return "work is end";
                }            
            }

            return string.Empty;
        }

        private void startWorking()
        {
            if (working) { return; }
            working = true;
            Task.Run(() =>
            {
                doSlaveWrite();
            });
        }
        private string doSlaveWrite()
        {
            var res = new StringBuilder();
            foreach(var para in bc.GetConsumingEnumerable())
            {
                foreach (var member in members)
                {
                    res.Append(member.Eat(para));
                }
            }
            lock (_lockObject) {
                working = false;
            }
                
            return res.ToString();
        }

    }
}
