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
        //public List<T> bindValues = new List<T>();

        public ItemBuildable<T> bindRange = new ItemBuildable<T>();

        /// <summary>
        /// 包含下级的单位
        /// </summary>
        //public List<T> containValues = new List<T>();
        public RangeBuildable<T> containRange = new RangeBuildable<T>();
        /// <summary>
        /// 是否空
        /// </summary>
        public bool Empty
        {
            get
            {
                if (bindRange.Values.Count > 0)
                {
                    return false;
                }
                if (containRange.Values.Count > 0)
                {
                    return false;
                }
                return true;
            }
        }
        public List<T> containValues
        {
            get { return containRange.Values; }
        }

        public List<T> bindValues
        {
            get { return bindRange.Values; }
        }

        /// <summary>
        /// 清空注册的过滤器
        /// </summary>
        public void resetBuilder()
        {
            this.bindRange.reset();
            this.containRange.reset();
        }
        /// <summary>
        /// 获取所有已绑定的值
        /// </summary>
        /// <returns></returns>
        public List<T> getAllBind() { 
            var t= new HashSet<T>();
            foreach (var h in bindRange.Values) {
                t.Add(h);
            }
            foreach (var h in containRange.Values)
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
            foreach (var li in containRange.Values)
            {
                if (val.isChildOf(li))
                {
                    return false;
                }
            }
            //直接绑定时，不作子级检查
            //foreach (var li in bindValues)
            //{
            //    if (val.isChildOf(li))
            //    {
            //        return false;
            //    }
            //}

            ////反向检查，如果新增的编码，是现有编码的父编码，则移除现有编码

            //for (int i = bindValues.Count - 1; i >= 0; i--)
            //{
            //    //比如加 116， 则移除11601这样的子级
            //    var li = bindValues[i];
            //    if (li.isChildOf(val))
            //    {
            //        bindValues.RemoveAt(i);
            //    }
            //}

            //执行添加
            bindRange.Values.Add(val);
            return true;
        }
        /// <summary>
        /// 添加一个值，包含下级
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool addContainValue(T value)
        {

            //添加包含下级时，不需要在直接绑定中查重
            var added = this.containRange.add(value);
            if (!added) return false;
            //遍历直接绑定集合，如果是当前组织的子级，则移除它
            for (int i = bindRange.Values.Count - 1; i >= 0; i--)
            {
                //比如加 116， 则移除11601这样的子级
                var li = bindRange.Values[i];
                if (li.isChildOf(value))
                {
                    bindRange.Values.RemoveAt(i);
                }
            }
            return true;
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

            if (containRange.Values.Count == 0 && bindRange.Values.Count == 0) return wh;

            foreach (var org in containRange.Values)
            {

                var res = doBuild(org, true);
                if (!string.IsNullOrWhiteSpace(res) && !wh.Contains(res))
                {
                    wh.Add(res);
                }
            }
            foreach (var org in bindRange.Values)
            {

                var res = doBuild(org, false);
                if (!string.IsNullOrWhiteSpace(res) && !wh.Contains(res))
                {
                    wh.Add(res);
                }
            }
            return wh;
        }

        //private List<string> buildContainWhere(List<string> wh) {
        //    if (containValues == null || containValues.Count == 0)
        //    {
        //        return wh;
        //    }

        //    if (this.onbuildManyLike != null)
        //    {
        //        var mval = onbuildManyLike(containValues);
        //        if (!string.IsNullOrWhiteSpace(mval) && !wh.Contains(mval))
        //        {
        //            wh.Add(mval);
        //        }
        //        return wh;
        //    }

        //    //在没有单个处理器的情况下，检查保底的多个处理器
        //    if (onbuildLike == null && this.onbuildLikesByPK != null) {
        //        var val = this.onbuildLikesByPK(this.containValues);
        //        if (!string.IsNullOrWhiteSpace(val) && !wh.Contains(val))
        //        {
        //            wh.Add(val);
        //        }
        //        return wh;
        //    }

        //    foreach (var org in containValues)
        //    {

        //        // 检查 单个适配器
        //        var res = "";
        //        if (onbuildLike != null)
        //        {
        //            res = onbuildLike(org);
        //            if (!string.IsNullOrWhiteSpace(res) && !wh.Contains(res))
        //            {
        //                wh.Add(res);
        //            }
        //            continue;
        //        }
        //        if (onbuildLikeByPK != null)
        //        {
        //            res = onbuildLikeByPK(org);
        //            if (!string.IsNullOrWhiteSpace(res) && !wh.Contains(res))
        //            {
        //                wh.Add(res);
        //            }
        //            continue;
        //        }

        //        if (onbuildOne != null)
        //        {
        //            res = onbuildOne(org, true);
        //            if (!string.IsNullOrWhiteSpace(res) && !wh.Contains(res))
        //            {
        //                wh.Add(res);
        //            }
        //            continue;
        //        }

        //    }
        //    return wh;
        //}

        //private List<string> buildBindWhere(List<string> wh) {
        //    if (this.bindValues == null || bindValues.Count == 0) return wh;

        //    if (onbuildManyIn != null && bindValues.Count > 0)
        //    {
        //        var mval = onbuildManyIn(bindValues);
        //        if (!string.IsNullOrWhiteSpace(mval) && !wh.Contains(mval))
        //        {
        //            wh.Add(mval);
        //        }
        //        return wh;
        //    }

        //    foreach (var org in bindValues)
        //    {

        //        // 检查 单个适配器
        //        var res = "";
        //        if (onbuildIs != null)
        //        {
        //            res = onbuildIs(org);
        //            if (!string.IsNullOrWhiteSpace(res) && !wh.Contains(res))
        //            {
        //                wh.Add(res);
        //            }
        //            continue;
        //        }
        //        if (onbuildOne != null)
        //        {
        //            res = onbuildOne(org, false);
        //            if (!string.IsNullOrWhiteSpace(res) && !wh.Contains(res))
        //            {
        //                wh.Add(res);
        //            }
        //            continue;
        //        }
        //    }
        //    return wh;
        //}

        /// <summary>
        /// 执行条件编制，检查注册的编织器。
        /// </summary>
        /// <param name="wh"></param>
        /// <returns></returns>
        public List<string> buildWhere(List<string> wh)
        {

            if (this.Empty) return wh;
            if (onBuildAll != null) { 
                var t = onBuildAll(this);
                if (!string.IsNullOrWhiteSpace(t) && !wh.Contains(t)) { 
                    wh.Add(t);
                }
            }

            this.containRange.build(wh);

            this.bindRange.build(wh);

            return wh;
        }


        private Func<CodeRange<T>, string>? onBuildAll;

        /// <summary>
        /// 执行一个的条件处理。
        /// </summary>
        //private Func<T, bool, string>? onbuildOne;

        //private Func<T, string>? onbuildLike;

        //private Func<T, string>? onbuildLikeByPK;

        //private Func<T, string>? onbuildIs;
        /// <summary>
        /// 执行多个指定范围的处理。
        /// </summary>
        //private Func<List<T>, string>? onbuildManyIn;

        //private Func<List<T>, string>? onbuildManyLike;

        //private Func<List<T>, string>? onbuildLikesByPK;

        /// <summary>
        /// 从另一范围对象复制尚未设置的委托（编织器），用于默认分组拷贝配置。
        /// </summary>
        /// <param name="src">源范围对象。</param>
        public void CopyFunc(CodeRange<T> src) {
            if (this.onBuildAll == null && src.onBuildAll != null) { 
                this.onBuildAll = src.onBuildAll;
            }

            this.bindRange.CopyBuilder(src.bindRange);
            this.containRange.CopyBuilder(src.containRange);

        }

        /// <summary>
        /// 注册“整体范围”条件编织器（传入当前 CodeRange 实例生成 SQL）。
        /// </summary>
        /// <param name="builder">编织委托。</param>
        /// <returns>当前实例。</returns>
        public CodeRange<T> useAllBuilder(Func<CodeRange<T>, string> builder)
        {
            this.onBuildAll = builder;
            return this;
        }

        /// <summary>
        /// 注册单条项条件（含是否包含下级等布尔语义）。
        /// </summary>
        /// <param name="builder">编织委托。</param>
        /// <returns>当前实例。</returns>
        public CodeRange<T> useOneBuilder(Func<T, bool, string> builder)
        {
            this.bindRange.useOneBuilder(builder);
            this.containRange.useOneBuilder(builder);
            return this;
        }
        /// <summary>
        /// 注册 IN 列表条件编织器。
        /// </summary>
        /// <param name="builder">编织委托。</param>
        /// <returns>当前实例。</returns>
        public CodeRange<T> useInBuilder(Func<List<T>, string> builder)
        {
            this.bindRange.useInBuilder(builder);
            return this;
        }
        /// <summary>
        /// 注册等值（IS）条件编织器。
        /// </summary>
        /// <param name="builder">编织委托。</param>
        /// <returns>当前实例。</returns>
        public CodeRange<T> useIsBuilder(Func<T, string> builder)
        {
            this.bindRange.useIsBuilder(builder);
            return this;
        }
        /// <summary>
        /// 注册 LIKE/前缀 类条件编织器。
        /// </summary>
        /// <param name="builder">编织委托。</param>
        /// <returns>当前实例。</returns>
        public CodeRange<T> useLikeBuilder(Func<T, string> builder)
        {
            this.containRange.useLikeBuilder(builder);
            return this;
        }
        /// <summary>
        /// 非code版本的包含权限构造器
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public CodeRange<T> useLikePKBuilder(Func<T, string> builder)
        {
            this.containRange.useLikePKBuilder(builder);
            return this;
        }
        /// <summary>
        /// 多个处理
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public CodeRange<T> useLikesPKBuilder(Func<List<T>, string> builder)
        {
            this.containRange.useLikesPKBuilder(builder);
            return this;
        }
        


        /// <summary>
        /// 注册多值 LIKE 组合条件编织器。
        /// </summary>
        /// <param name="builder">编织委托。</param>
        /// <returns>当前实例。</returns>
        public CodeRange<T> useManyLikeBuilder(Func<List<T>, string> builder)
        {
            this.containRange.useManyLikeBuilder(builder);
            return this;
        }

        /// <summary>
        /// 检查某个层次码是否子码
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public bool checkInScope(T t)
        {
            foreach (var org in bindRange.Values)
            {
                //是父编码，且前几位相同，返回
                if (org.isSame(t)) return true;
            }
            foreach (var org in containRange.Values)
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
            foreach (var org in containRange.Values)
            {

                var res = getVal(org);
                if (!string.IsNullOrWhiteSpace(res) && !wh.Contains(res))
                {
                    wh.Add(res);
                }
            }
            foreach (var org in bindRange.Values)
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