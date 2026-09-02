using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.auth
{
    public class ItemBuildable<T> where T : Childable
    {

        public List<T> Values = new List<T>();

        private Func<T, string>? onbuildIs;
        /// <summary>
        /// 执行多个指定范围的处理。
        /// </summary>
        private Func<List<T>, string>? onbuildManyIn;

        private Func<T, bool, string>? onbuildOne;

        public void reset() {

            this.onbuildIs = null;
            this.onbuildManyIn = null;
            this.onbuildOne = null;
        }


        /// <summary>
        /// 注册单条项条件（含是否包含下级等布尔语义）。
        /// </summary>
        /// <param name="builder">编织委托。</param>
        /// <returns>当前实例。</returns>
        public ItemBuildable<T> useOneBuilder(Func<T, bool, string> builder)
        {
            this.onbuildOne = builder;
            return this;
        }

        /// <summary>
        /// 注册 IN 列表条件编织器。
        /// </summary>
        /// <param name="builder">编织委托。</param>
        /// <returns>当前实例。</returns>
        public ItemBuildable<T> useInBuilder(Func<List<T>, string> builder)
        {
            this.onbuildManyIn = builder;
            return this;
        }
        /// <summary>
        /// 注册等值（IS）条件编织器。
        /// </summary>
        /// <param name="builder">编织委托。</param>
        /// <returns>当前实例。</returns>
        public ItemBuildable<T> useIsBuilder(Func<T, string> builder)
        {
            this.onbuildIs = builder;
            return this;
        }



        public bool build(List<string> wh)
        {
            if (this.Values == null || Values.Count == 0) return false;

            if (onbuildManyIn != null && Values.Count > 0)
            {
                var mval = onbuildManyIn(Values);
                if (!string.IsNullOrWhiteSpace(mval) && !wh.Contains(mval))
                {
                    wh.Add(mval);
                }
                return true;
            }

            foreach (var org in Values)
            {

                // 检查 单个适配器
                var res = "";
                if (onbuildIs != null)
                {
                    res = onbuildIs(org);
                    if (!string.IsNullOrWhiteSpace(res) && !wh.Contains(res))
                    {
                        wh.Add(res);
                    }
                    continue;
                }
                if (onbuildOne != null)
                {
                    res = onbuildOne(org, false);
                    if (!string.IsNullOrWhiteSpace(res) && !wh.Contains(res))
                    {
                        wh.Add(res);
                    }
                    continue;
                }
            }
            return true;
        }

        public void CopyBuilder(ItemBuildable<T> src)
        {

            if (this.onbuildOne == null && src.onbuildOne != null)
            {
                this.onbuildOne = src.onbuildOne;
            }

            if (this.onbuildIs == null && src.onbuildIs != null)
            {
                this.onbuildIs = src.onbuildIs;
            }
            if (this.onbuildManyIn == null && src.onbuildManyIn != null)
            {
                this.onbuildManyIn = src.onbuildManyIn;
            }


        }
    }
}
