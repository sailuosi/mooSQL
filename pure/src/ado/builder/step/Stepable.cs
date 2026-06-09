using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 步骤化的基类，步骤具有记录、重放功能。
    /// </summary>
    public class Stepable<T>
    {
        /// <summary>
        /// 步骤记录
        /// </summary>
        public List<T> steps;

        /// <summary>
        /// 现在是否记录
        /// </summary>
        public bool recordNow;
        /// <summary>
        /// 
        /// </summary>
        public Stepable(bool recordEvery)
        {

            this.steps = new List<T>();
            this.recordNow = false;
        }
        /// <summary>
        /// 开始
        /// </summary>
        public void start() {
            this.recordNow = true;
        }
        /// <summary>
        /// 停止
        /// </summary>
        public void stop() {
            this.recordNow = false;
        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Stepable<T> add(T item) {

            if (this.recordNow == true)
            {
                this.steps.Add(item);
            }
            return this;
        }
        /// <summary>
        /// 添加一个
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Stepable<T> add(Func<T> item)
        {
            if (!this.recordNow) { 
                return this;
            }
            var t = item();

            if (this.recordNow == true)
            {
                this.steps.Add(t);
            }
            return this;
        }
        /// <summary>
        /// 清空
        /// </summary>
        public void clear() { 
            this.steps.Clear();
        }
    }
}
