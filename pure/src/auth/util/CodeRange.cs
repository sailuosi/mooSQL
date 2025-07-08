// 基础功能说明：

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.auth
{
    /// <summary>
    /// 带有层次码的范围处理。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CodeRange<T> where T : Childable
    {
        /// <summary>
        /// 直接绑定不含下级的单位
        /// </summary>
        public List<T> bindValues = new List<T>();
        /// <summary>
        /// 包含下级的单位
        /// </summary>
        public List<T> containValues = new List<T>();
        /// <summary>
        /// 是否空
        /// </summary>
        public bool Empty
        {
            get
            {
                if (bindValues.Count > 0)
                {
                    return false;
                }
                if (containValues.Count > 0)
                {
                    return false;
                }
                return true;
            }
        }
        /// <summary>
        /// 清空注册的过滤器
        /// </summary>
        public void resetBuilder()
        {
            this.onbuildIs = null;
            this.onbuildLike = null;
            this.onbuildManyIn = null;
            this.onbuildOne = null;
            this.onbuildManyLike = null;
        }
        /// <summary>
        /// 获取所有已绑定的值
        /// </summary>
        /// <returns></returns>
        public List<T> getAllBind() { 
            var t= new HashSet<T>();
            foreach (var h in bindValues) {
                t.Add(h);
            }
            foreach (var h in containValues)
            {
                t.Add(h);
            }
            return t.ToList();
        }
        /// <summary>
        /// 添加一个绑定值，不包含下级
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public bool addBindValue(T val)
        {

            bool res = false;
            //如果当前编码是一个顶级码的子码，且顶级码是包含下级的，忽略它。
            foreach (var li in containValues)
            {
                if (val.isChildOf(li))
                {
                    return false;
                }
            }

            foreach (var li in bindValues)
            {
                if (val.isChildOf(li))
                {
                    return false;
                }
            }

            //反向检查，如果新增的编码，是现有编码的父编码，则移除现有编码

            for (int i = bindValues.Count - 1; i >= 0; i--)
            {
                //比如加 116， 则移除11601这样的子级
                var li = bindValues[i];
                if (li.isChildOf(val))
                {
                    bindValues.RemoveAt(i);
                }
            }

            //执行添加
            bindValues.Add(val);
            return res;
        }
        /// <summary>
        /// 添加一个值，包含下级
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool addContainValue(T value)
        {

            //添加包含下级时，不需要在直接绑定中查重
            bool res = false;
            //如果当前编码是一个顶级码的子码，且顶级码是包含下级的，忽略它。
            foreach (var li in containValues)
            {
                if (value.isChildOf(li))
                {
                    return false;
                }
            }

            //反向检查，如果新增的编码，是现有编码的父编码，则移除现有编码

            for (int i = containValues.Count - 1; i >= 0; i--)
            {
                //比如加 116， 则移除11601这样的子级
                var li = containValues[i];
                if (li.isChildOf(value))
                {
                    containValues.RemoveAt(i);
                }
            }
            //遍历直接绑定集合，如果是当前组织的子级，则移除它
            for (int i = bindValues.Count - 1; i >= 0; i--)
            {
                //比如加 116， 则移除11601这样的子级
                var li = bindValues[i];
                if (li.isChildOf(value))
                {
                    bindValues.RemoveAt(i);
                }
            }

            //执行添加
            containValues.Add(value);
            return res;
        }
        /// <summary>
        /// 添加一组绑定值
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public int addBindValue(List<T> list)
        {
            var res = 0;
            foreach (var li in list)
            {
                if (addBindValue(li))
                {
                    res++;
                }
            }
            return res;
        }
        /// <summary>
        /// 添加一组包含值
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public int addContainValue(List<T> list) {
            var res = 0;
            foreach (var li in list) {
                if (addContainValue(li)) { 
                    res++;
                }
            }
            return res;
        }


        /// <summary>
        /// 执行条件的编织
        /// </summary>
        /// <param name="wh"></param>
        /// <param name="doBuild"></param>
        /// <returns></returns>
        public List<string> buildWhere(List<string> wh, Func<T, bool, string> doBuild)
        {

            if (containValues.Count == 0 && bindValues.Count == 0) return wh;

            foreach (var org in containValues)
            {

                var res = doBuild(org, true);
                if (!string.IsNullOrWhiteSpace(res) && !wh.Contains(res))
                {
                    wh.Add(res);
                }
            }
            foreach (var org in bindValues)
            {

                var res = doBuild(org, false);
                if (!string.IsNullOrWhiteSpace(res) && !wh.Contains(res))
                {
                    wh.Add(res);
                }
            }
            return wh;
        }
        /// <summary>
        /// 执行条件编制，检查注册的编织器。
        /// </summary>
        /// <param name="wh"></param>
        /// <returns></returns>
        public List<string> buildWhere(List<string> wh)
        {

            if (containValues.Count == 0 && bindValues.Count == 0) return wh;
            if (onBuildAll != null) { 
                var t = onBuildAll(this);
                if (!string.IsNullOrWhiteSpace(t) && !wh.Contains(t)) { 
                    wh.Add(t);
                }            
            }

            if(this.onbuildManyLike != null && containValues.Count > 0)
            {
                var mval = onbuildManyLike(containValues);
                if (!string.IsNullOrWhiteSpace(mval) && !wh.Contains(mval)) { 
                    wh.Add(mval);
                }
                return wh;
            }
            foreach (var org in containValues)
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

            if (onbuildManyIn != null && bindValues.Count > 0)
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
            return wh;
        }


        private Func<CodeRange<T>, string> onBuildAll;

        /// <summary>
        /// 执行一个的条件处理。
        /// </summary>
        private Func<T, bool, string> onbuildOne;

        private Func<T, string> onbuildLike;

        private Func<T, string> onbuildIs;
        /// <summary>
        /// 执行多个指定范围的处理。
        /// </summary>
        private Func<List<T>, string> onbuildManyIn;

        private Func<List<T>, string> onbuildManyLike;

        public void CopyFunc(CodeRange<T> src) {
            if (this.onBuildAll == null && src.onBuildAll != null) { 
                this.onBuildAll = src.onBuildAll;
            }
            if (this.onbuildOne == null && src.onbuildOne != null)
            {
                this.onbuildOne = src.onbuildOne;
            }
            if (this.onbuildLike == null && src.onbuildLike != null)
            {
                this.onbuildLike = src.onbuildLike;
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

        public CodeRange<T> useAllBuilder(Func<CodeRange<T>, string> builder)
        {
            this.onBuildAll = builder;
            return this;
        }

        public CodeRange<T> useOneBuilder(Func<T, bool, string> builder)
        {
            this.onbuildOne = builder;
            return this;
        }
        public CodeRange<T> useInBuilder(Func<List<T>, string> builder)
        {
            this.onbuildManyIn = builder;
            return this;
        }
        public CodeRange<T> useLikeBuilder(Func<T, string> builder)
        {
            this.onbuildLike = builder;
            return this;
        }

        public CodeRange<T> useIsBuilder(Func<T, string> builder)
        {
            this.onbuildIs = builder;
            return this;
        }
        public CodeRange<T> useManyLikeBuilder(Func<List<T>, string> builder)
        {
            this.onbuildManyLike = builder;
            return this;
        }

        /// <summary>
        /// 检查某个层次码是否子码
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public bool checkInScope(T t)
        {
            foreach (var org in bindValues)
            {
                //是父编码，且前几位相同，返回
                if (org.isSame(t)) return true;
            }
            foreach (var org in containValues)
            {
                //是父编码，且前几位相同，返回
                if (t.isChildOf(org)) return true;
            }
            return false;
        }

        /// <summary>
        /// 获取所有的顶级组织节点值，注意，包含全部不在此判定中。因包含全部实质为无限大。
        /// </summary>
        /// <param name="getVal"></param>
        /// <returns></returns>
        public List<string> selectTopOrg(Func<T, string> getVal)
        {
            var wh = new List<string>();
            foreach (var org in containValues)
            {

                var res = getVal(org);
                if (!string.IsNullOrWhiteSpace(res) && !wh.Contains(res))
                {
                    wh.Add(res);
                }
            }
            foreach (var org in bindValues)
            {

                var res = getVal(org);
                if (!string.IsNullOrWhiteSpace(res) && !wh.Contains(res))
                {
                    wh.Add(res);
                }
            }
            return wh;
        }
    }
}