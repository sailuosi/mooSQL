using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 导航保存
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class NavGuideSave<T>:NavGuideBase<T>
    {
        /// <summary>
        /// 导航保存
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="mainList"></param>
        public NavGuideSave(SQLBuilder builder, IEnumerable<T> mainList) : base(builder, mainList)
        {
        }


        /// <summary>
        /// 工作单元
        /// </summary>
        public SooUnitOfWork UOW {  get; set; }
        /// <summary>
        /// 保存计数
        /// </summary>
        public int SaveCount { get; set; }
        /// <summary>
        /// 收集顶层的下一层
        /// </summary>
        /// <typeparam name="Child"></typeparam>
        /// <param name="findChild"></param>
        /// <returns></returns>
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
        /// 提交
        /// </summary>
        /// <returns></returns>
        public int commit() {
            return this.UOW.Commit();
        }
        /// <summary>
        /// 执行插入顶
        /// </summary>
        /// <returns></returns>
        public virtual NavGuideSave<T> insert() {
            this.UOW.InsertRange(this.MainList);
            return this;
        }
        /// <summary>
        /// 更新顶层
        /// </summary>
        /// <returns></returns>
        public virtual NavGuideSave<T> update()
        {
            this.UOW.UpdateRange(this.MainList);
            return this;
        }
        /// <summary>
        /// 保存顶层
        /// </summary>
        /// <returns></returns>
        public virtual NavGuideSave<T> save()
        {
            this.UOW.SaveRange(this.MainList);
            return this;
        }
    }
    /// <summary>
    /// 主子两层导航保存
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="Child"></typeparam>
    public class NavGuideSave<T, Child> : NavGuideSave<T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="mainList"></param>
        /// <param name="children"></param>
        public NavGuideSave(SQLBuilder builder, IEnumerable<T> mainList, IEnumerable<Child> children) : base(builder, mainList)
        {
            this.Children = children;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="src"></param>
        /// <param name="children"></param>
        public NavGuideSave(NavGuideSave<T> src, IEnumerable<Child> children) : base(src.Builder, src.MainList)
        {
            this.UOW=src.UOW;
            this.Children = children;
        }
        /// <summary>
        /// 第二层数据集
        /// </summary>
        public IEnumerable<Child> Children { get; set; }

        /// <summary>
        /// 继续收集下一层
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="findChild"></param>
        /// <returns></returns>
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
        /// 执行插入
        /// </summary>
        /// <returns></returns>
        public new NavGuideSave<T, Child> insert()
        {
            this.UOW.InsertRange(this.Children);
            return this;
        }
        /// <summary>
        /// 更新第二层
        /// </summary>
        /// <returns></returns>
        public new NavGuideSave<T, Child> update()
        {
            this.UOW.UpdateRange(this.Children);
            return this;
        }
        /// <summary>
        /// 保存第二层
        /// </summary>
        /// <returns></returns>
        public new NavGuideSave<T,Child> save()
        {
            this.UOW.SaveRange(this.Children);
            return this;
        }

    }

    /// <summary>
    /// 三层导航保存
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="Child"></typeparam>
    /// <typeparam name="GradSon"></typeparam>
    public class NavGuideSave<T, Child, GradSon> : NavGuideSave<T, Child>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="mainList"></param>
        /// <param name="children"></param>
        /// <param name="sons"></param>
        public NavGuideSave(SQLBuilder builder, IEnumerable<T> mainList, IEnumerable<Child> children,IEnumerable<GradSon> sons) : base(builder, mainList, children)
        {
            this.GrandSon = sons;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="src"></param>
        /// <param name="sons"></param>
        public NavGuideSave(NavGuideSave<T, Child> src, IEnumerable<GradSon> sons) : base(src.Builder, src.MainList, src.Children)
        {
            
            this.UOW = src.UOW;
            this.GrandSon = sons;
            
        }
        /// <summary>
        /// 第三层数据集
        /// </summary>
        public IEnumerable<GradSon> GrandSon { get; set; }

        /// <summary>
        /// 执行插入第三层
        /// </summary>
        /// <returns></returns>
        public new NavGuideSave<T, Child, GradSon> insert()
        {
            this.UOW.InsertRange(this.GrandSon);
            return this;
        }
        /// <summary>
        /// 更新第三层
        /// </summary>
        /// <returns></returns>
        public new NavGuideSave<T, Child, GradSon> update()
        {
            this.UOW.UpdateRange(this.GrandSon);
            return this;
        }
        /// <summary>
        /// 保存第三层
        /// </summary>
        /// <returns></returns>
        public new NavGuideSave<T, Child, GradSon> save()
        {
            this.UOW.SaveRange(this.GrandSon);
            return this;
        }
        /// <summary>
        /// 继续下一层
        /// </summary>
        /// <typeparam name="Another"></typeparam>
        /// <param name="findChild"></param>
        /// <returns></returns>
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
