// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.auth
{
    /// <summary>
    /// 范围集合
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ItemRange<T> where T : Samable
    {

        public List<T> bindValues = new List<T>();

        public int add(T item)
        {
            foreach (T man in this.bindValues)
            {
                if (man.isSame(item))
                {
                    return 0;
                }
            }
            bindValues.Add(item);
            return 1;
        }

        public bool Empty
        {
            get {
                if (bindValues.Count == 0) {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 执行一个的条件处理。
        /// </summary>
        private Func<T, string> onbuildOne;
        /// <summary>
        /// 执行多个指定范围的处理。
        /// </summary>
        private Func<List<T>, string> onbuildManyIn;

        public void CopyFunc(ItemRange<T> src)
        {

            if (this.onbuildOne == null && src.onbuildOne != null)
            {
                this.onbuildOne = src.onbuildOne;
            }

            if (this.onbuildManyIn == null && src.onbuildManyIn != null)
            {
                this.onbuildManyIn = src.onbuildManyIn;
            }
        }

        public void resetBuilder()
        {
            this.onbuildOne = null;
            this.onbuildManyIn = null;
        }

        public ItemRange<T> useOneBuilder(Func<T, string> builder)
        {
            this.onbuildOne = builder;
            return this;
        }
        public ItemRange<T> useInBuilder(Func<List<T>, string> builder)
        {
            this.onbuildManyIn = builder;
            return this;
        }

        /// <summary>
        /// 执行条件编制，检查注册的编织器。
        /// </summary>
        /// <param name="wh"></param>
        /// <returns></returns>
        public List<string> buildWhere(List<string> wh)
        {

            if (bindValues.Count == 0) return wh;



            if (onbuildManyIn != null)
            {
                var mval = onbuildManyIn(bindValues);
                if (!string.IsNullOrWhiteSpace(mval) && !wh.Contains(mval))
                {
                    wh.Add(mval);
                }
                return wh;
            }

            foreach (var org in bindValues)
            {

                // 检查 单个适配器
                var res = "";
                if (onbuildOne != null)
                {
                    res = onbuildOne(org);
                    if (!string.IsNullOrWhiteSpace(res) && !wh.Contains(res))
                    {
                        wh.Add(res);
                    }
                    continue;
                }
            }
            return wh;
        }
    }
}