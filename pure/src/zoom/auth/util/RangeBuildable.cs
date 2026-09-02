using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.auth
{
    public class RangeBuildable<T> where T : Childable
    {
        public List<T> Values = new List<T>();

        public Func<List<T>, string>? onbuildManyLike;

        public Func<List<T>, string>? onbuildLikesByPK;

        public Func<T, string>? onbuildLike;

        public Func<T, string>? onbuildLikeByPK;

        public Func<T, bool, string>? onbuildOne;

        public void reset() { 
            this.onbuildManyLike=null;
            this.onbuildLikesByPK = null;
            this.onbuildLike = null;
            this.onbuildLikeByPK = null;
            this.onbuildOne = null;
        }

        public bool add(T value)
        {

            //添加包含下级时，不需要在直接绑定中查重
            bool res = false;
            //如果当前编码是一个顶级码的子码，且顶级码是包含下级的，忽略它。
            foreach (var li in Values)
            {
                if (value.isChildOf(li))
                {
                    return false;
                }
            }

            //反向检查，如果新增的编码，是现有编码的父编码，则移除现有编码

            for (int i = Values.Count - 1; i >= 0; i--)
            {
                //比如加 116， 则移除11601这样的子级
                var li = Values[i];
                if (li.isChildOf(value))
                {
                    Values.RemoveAt(i);
                }
            }

            //执行添加
            Values.Add(value);
            return true;
        }



        /// <summary>
        /// 返回bool标识是否执行了构建，如果返回false,则需要再运行剩余的构建器
        /// </summary>
        /// <param name="wh"></param>
        /// <returns></returns>
        public bool build(List<string> wh)
        {
            if (Values == null || Values.Count == 0)
            {
                return false;
            }

            if (this.onbuildManyLike != null)
            {
                var mval = onbuildManyLike(Values);
                if (!string.IsNullOrWhiteSpace(mval) && !wh.Contains(mval))
                {
                    wh.Add(mval);
                }
                return true;
            }

            //在没有单个处理器的情况下，检查保底的多个处理器
            if (onbuildLike == null && this.onbuildLikesByPK != null)
            {
                var val = this.onbuildLikesByPK(this.Values);
                if (!string.IsNullOrWhiteSpace(val) && !wh.Contains(val))
                {
                    wh.Add(val);
                }
                return true;
            }

            foreach (var org in Values)
            {

                // 检查 单个适配器
                var res = "";
                if (onbuildLike != null)
                {
                    res = onbuildLike(org);
                    if (!string.IsNullOrWhiteSpace(res) && !wh.Contains(res))
                    {
                        wh.Add(res);
                    }
                    continue;
                }
                if (onbuildLikeByPK != null)
                {
                    res = onbuildLikeByPK(org);
                    if (!string.IsNullOrWhiteSpace(res) && !wh.Contains(res))
                    {
                        wh.Add(res);
                    }
                    continue;
                }

                if (onbuildOne != null)
                {
                    res = onbuildOne(org, true);
                    if (!string.IsNullOrWhiteSpace(res) && !wh.Contains(res))
                    {
                        wh.Add(res);
                    }
                    continue;
                }

            }
            return true;
        }

        /// <summary>
        /// 注册 LIKE/前缀 类条件编织器。
        /// </summary>
        /// <param name="builder">编织委托。</param>
        /// <returns>当前实例。</returns>
        public RangeBuildable<T> useLikeBuilder(Func<T, string> builder)
        {
            this.onbuildLike = builder;
            return this;
        }
        /// <summary>
        /// 非code版本的包含权限构造器
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public RangeBuildable<T> useLikePKBuilder(Func<T, string> builder)
        {
            this.onbuildLikeByPK = builder;
            return this;
        }
        /// <summary>
        /// 多个处理
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public RangeBuildable<T> useLikesPKBuilder(Func<List<T>, string> builder)
        {
            this.onbuildLikesByPK = builder;
            return this;
        }

        /// <summary>
        /// 注册多值 LIKE 组合条件编织器。
        /// </summary>
        /// <param name="builder">编织委托。</param>
        /// <returns>当前实例。</returns>
        public RangeBuildable<T> useManyLikeBuilder(Func<List<T>, string> builder)
        {
            this.onbuildManyLike = builder;
            return this;
        }

        /// <summary>
        /// 注册单条项条件（含是否包含下级等布尔语义）。
        /// </summary>
        /// <param name="builder">编织委托。</param>
        /// <returns>当前实例。</returns>
        public RangeBuildable<T> useOneBuilder(Func<T, bool, string> builder)
        {
            this.onbuildOne = builder;
            return this;
        }

        public void CopyBuilder(RangeBuildable<T> src)
        {

            if (this.onbuildOne == null && src.onbuildOne != null)
            {
                this.onbuildOne = src.onbuildOne;
            }
            if (this.onbuildLike == null && src.onbuildLike != null)
            {
                this.onbuildLike = src.onbuildLike;
            }

            if (this.onbuildLikeByPK == null && src.onbuildLikeByPK != null)
            {
                this.onbuildLikeByPK = src.onbuildLikeByPK;
            }
            if (this.onbuildLikesByPK == null && src.onbuildLikesByPK != null)
            {
                this.onbuildLikesByPK = src.onbuildLikesByPK;
            }

        }
    }
}
