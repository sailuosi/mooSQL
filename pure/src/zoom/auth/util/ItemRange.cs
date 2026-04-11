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

        /// <summary>已绑定的项集合（去重）。</summary>
        public List<T> bindValues = new List<T>();

        /// <summary>
        /// 添加一项，若已存在相同项则忽略。
        /// </summary>
        /// <param name="item">项。</param>
        /// <returns>新增返回 1，已存在返回 0。</returns>
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

        /// <summary>是否没有任何绑定项。</summary>
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

        /// <summary>
        /// 从另一范围复制尚未设置的编织器委托。
        /// </summary>
        /// <param name="src">源对象。</param>
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

        /// <summary>清空已注册的 SQL 编织器委托。</summary>
        public void resetBuilder()
        {
            this.onbuildOne = null;
            this.onbuildManyIn = null;
        }

        /// <summary>注册单条项条件编织器。</summary>
        /// <param name="builder">编织委托。</param>
        /// <returns>当前实例。</returns>
        public ItemRange<T> useOneBuilder(Func<T, string> builder)
        {
            this.onbuildOne = builder;
            return this;
        }
        /// <summary>注册 IN 列表条件编织器。</summary>
        /// <param name="builder">编织委托。</param>
        /// <returns>当前实例。</returns>
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