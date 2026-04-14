using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 导航保存（单层）：在已有主实体列表与 <see cref="SooUnitOfWork"/> 上，对顶层集合执行批量插入、更新或 Save，并可向下收集子实体形成多级保存链。
    /// </summary>
    /// <typeparam name="T">顶层实体类型。</typeparam>
    public class NavGuideSave<T>:NavGuideBase<T>
    {
        /// <summary>
        /// 使用 SQL 构建器与主列表初始化；后续需设置 <see cref="UOW"/> 再调用插入/更新/保存或 <see cref="commit"/>。
        /// </summary>
        /// <param name="builder">SQL 构建器（与仓储/工作单元同一上下文）。</param>
        /// <param name="mainList">顶层实体集合。</param>
        public NavGuideSave(SQLBuilder builder, IEnumerable<T> mainList) : base(builder, mainList)
        {
        }


        /// <summary>
        /// 工作单元，用于将各层 <c>InsertRange</c> / <c>UpdateRange</c> / <c>SaveRange</c> 操作纳入同一事务提交。
        /// </summary>
        public SooUnitOfWork UOW {  get; set; }
        /// <summary>
        /// 可由业务扩展记录的保存相关计数（库内未强制递增，默认由调用方维护语义）。
        /// </summary>
        public int SaveCount { get; set; }
        /// <summary>
        /// 从当前 <see cref="NavGuideBase{T}.MainList"/> 中逐条提取子实体并扁平合并，得到仅针对子层级的 <see cref="NavGuideSave{T, Child}"/>，便于对下一层执行插入/更新/保存。
        /// </summary>
        /// <typeparam name="Child">子实体类型。</typeparam>
        /// <param name="findChild">从单个主实体取出其子实体序列（可为空序列）。</param>
        /// <returns>绑定同一 <see cref="UOW"/> 与主列表、但操作目标为子集合的导航保存对象。</returns>
        public NavGuideSave<T,Child> collect<Child>(Func<T, IEnumerable<Child>> findChild) {

            var res = new List<Child>();
            foreach (var li in MainList) {
                var child = findChild(li);
                res.AddRange(child);
            }
            var tar = new NavGuideSave<T,Child>(this, res);
            return tar;
        }


        /// <summary>
        /// 提交工作单元，持久化此前入队的插入/更新/保存操作。
        /// </summary>
        /// <returns>工作单元提交返回值（通常为受影响行数或内部约定结果）。</returns>
        public int commit() {
            return this.UOW.Commit();
        }
        /// <summary>
        /// 将当前顶层 <see cref="NavGuideBase{T}.MainList"/> 加入工作单元的插入队列。
        /// </summary>
        /// <returns>当前实例，用于链式调用。</returns>
        public virtual NavGuideSave<T> insert() {
            this.UOW.InsertRange(this.MainList);
            return this;
        }
        /// <summary>
        /// 将当前顶层 <see cref="NavGuideBase{T}.MainList"/> 加入工作单元的更新队列。
        /// </summary>
        /// <returns>当前实例，用于链式调用。</returns>
        public virtual NavGuideSave<T> update()
        {
            this.UOW.UpdateRange(this.MainList);
            return this;
        }
        /// <summary>
        /// 将当前顶层 <see cref="NavGuideBase{T}.MainList"/> 加入工作单元的 Save 队列（按实体状态插入或更新）。
        /// </summary>
        /// <returns>当前实例，用于链式调用。</returns>
        public virtual NavGuideSave<T> save()
        {
            this.UOW.SaveRange(this.MainList);
            return this;
        }
    }
    /// <summary>
    /// 导航保存（主-子两层）：在单层 <see cref="NavGuideSave{T}"/> 基础上增加 <see cref="Children"/>，子层 <c>insert</c>/<c>update</c>/<c>save</c> 针对子集合，并可继续 <see cref="collectNext"/> 向第三层收集。
    /// </summary>
    /// <typeparam name="T">顶层实体类型。</typeparam>
    /// <typeparam name="Child">第二层（子）实体类型。</typeparam>
    public class NavGuideSave<T, Child> : NavGuideSave<T>
    {
        /// <summary>
        /// 同时指定顶层列表与已收集的子实体集合（子集合通常由 <see cref="NavGuideSave{T}.collect{Child}"/> 得到）。
        /// </summary>
        /// <param name="builder">SQL 构建器。</param>
        /// <param name="mainList">顶层实体集合。</param>
        /// <param name="children">第二层实体扁平列表。</param>
        public NavGuideSave(SQLBuilder builder, IEnumerable<T> mainList, IEnumerable<Child> children) : base(builder, mainList)
        {
            this.Children = children;
        }
        /// <summary>
        /// 由上一层导航保存对象复制构建器、主列表与工作单元，仅替换子层数据集合。
        /// </summary>
        /// <param name="src">来源导航保存对象（共享 <see cref="NavGuideSave{T}.UOW"/>）。</param>
        /// <param name="children">第二层实体集合。</param>
        public NavGuideSave(NavGuideSave<T> src, IEnumerable<Child> children) : base(src.Builder, src.MainList)
        {
            this.UOW=src.UOW;
            this.Children = children;
        }
        /// <summary>
        /// 第二层（子级）实体集合，供本层 <c>insert</c>/<c>update</c>/<c>save</c> 使用。
        /// </summary>
        public IEnumerable<Child> Children { get; set; }

        /// <summary>
        /// 从当前 <see cref="Children"/> 中逐条提取更下一层实体并合并，生成三层导航保存对象。
        /// </summary>
        /// <typeparam name="R">第三层实体类型。</typeparam>
        /// <param name="findChild">从单个子实体取出其下级序列。</param>
        /// <returns>包含顶层、子层、孙层数据集与工作单元的导航保存对象。</returns>
        public NavGuideSave<T,Child, R> collectNext<R>(Func<Child, IEnumerable<R>> findChild)
        {

            var res = new List<R>();
            foreach (var li in Children)
            {
                var child = findChild(li);
                res.AddRange(child);
            }
            var tar = new NavGuideSave<T, Child,R>(this,res);
            return tar;
        }

        /// <summary>
        /// 将当前 <see cref="Children"/> 加入工作单元的插入队列（隐藏基类对主列表的插入语义）。
        /// </summary>
        /// <returns>当前实例，用于链式调用。</returns>
        public new NavGuideSave<T, Child> insert()
        {
            this.UOW.InsertRange(this.Children);
            return this;
        }
        /// <summary>
        /// 将当前 <see cref="Children"/> 加入工作单元的更新队列。
        /// </summary>
        /// <returns>当前实例，用于链式调用。</returns>
        public new NavGuideSave<T, Child> update()
        {
            this.UOW.UpdateRange(this.Children);
            return this;
        }
        /// <summary>
        /// 将当前 <see cref="Children"/> 加入工作单元的 Save 队列。
        /// </summary>
        /// <returns>当前实例，用于链式调用。</returns>
        public new NavGuideSave<T,Child> save()
        {
            this.UOW.SaveRange(this.Children);
            return this;
        }

    }

    /// <summary>
    /// 导航保存（主-子-孙三层）：在两层基础上增加第三层数据集 <see cref="GrandSon"/>，本类上的 <c>insert</c>/<c>update</c>/<c>save</c> 仅作用于第三层；可通过 <see cref="thenNext"/> 在孙级上继续向下扩展。
    /// </summary>
    /// <typeparam name="T">顶层实体类型。</typeparam>
    /// <typeparam name="Child">第二层实体类型。</typeparam>
    /// <typeparam name="GradSon">第三层（孙级）实体类型。</typeparam>
    public class NavGuideSave<T, Child, GradSon> : NavGuideSave<T, Child>
    {
        /// <summary>
        /// 指定顶层、子层与孙层三个扁平集合。
        /// </summary>
        /// <param name="builder">SQL 构建器。</param>
        /// <param name="mainList">顶层实体集合。</param>
        /// <param name="children">第二层实体集合。</param>
        /// <param name="sons">第三层实体集合。</param>
        public NavGuideSave(SQLBuilder builder, IEnumerable<T> mainList, IEnumerable<Child> children,IEnumerable<GradSon> sons) : base(builder, mainList, children)
        {
            this.GrandSon = sons;
        }
        /// <summary>
        /// 由两层导航保存对象扩展第三层数据，并继承工作单元。
        /// </summary>
        /// <param name="src">来源对象（含顶层与子层数据）。</param>
        /// <param name="sons">第三层实体集合。</param>
        public NavGuideSave(NavGuideSave<T, Child> src, IEnumerable<GradSon> sons) : base(src.Builder, src.MainList, src.Children)
        {
            
            this.UOW = src.UOW;
            this.GrandSon = sons;
            
        }
        /// <summary>
        /// 第三层（孙级）实体集合。
        /// </summary>
        public IEnumerable<GradSon> GrandSon { get; set; }

        /// <summary>
        /// 将当前 <see cref="GrandSon"/> 加入工作单元的插入队列。
        /// </summary>
        /// <returns>当前实例，用于链式调用。</returns>
        public new NavGuideSave<T, Child, GradSon> insert()
        {
            this.UOW.InsertRange(this.GrandSon);
            return this;
        }
        /// <summary>
        /// 将当前 <see cref="GrandSon"/> 加入工作单元的更新队列。
        /// </summary>
        /// <returns>当前实例，用于链式调用。</returns>
        public new NavGuideSave<T, Child, GradSon> update()
        {
            this.UOW.UpdateRange(this.GrandSon);
            return this;
        }
        /// <summary>
        /// 将当前 <see cref="GrandSon"/> 加入工作单元的 Save 队列。
        /// </summary>
        /// <returns>当前实例，用于链式调用。</returns>
        public new NavGuideSave<T, Child, GradSon> save()
        {
            this.UOW.SaveRange(this.GrandSon);
            return this;
        }
        /// <summary>
        /// 在第三层集合上继续向下收集第四层实体，并返回新的三层导航保存对象（顶层类型仍为 <typeparamref name="T"/>，中间层变为原第三层类型、最下层为新类型 <typeparamref name="Another"/>）。
        /// </summary>
        /// <typeparam name="Another">第四层实体类型。</typeparam>
        /// <param name="findChild">从单个孙级实体取出其下级序列。</param>
        /// <returns>重新绑定数据集后的导航保存对象。</returns>
        public NavGuideSave<T, GradSon,Another> thenNext<Another>(Func<GradSon, IEnumerable<Another>> findChild)
        {

            var res = new List<Another>();
            foreach (var li in GrandSon)
            {
                var child = findChild(li);
                res.AddRange(child);
            }
            var tar = new NavGuideSave<T, GradSon, Another>(this.Builder,this.MainList,this.GrandSon, res);
            return tar;
        }
    }
}
