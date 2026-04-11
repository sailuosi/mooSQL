using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model
{
    /// <summary>
    /// 结合盒模型和链接对象模块的盒子，即它要么是一个盒子，要么是成员为链接对象。
    /// </summary>
    public class LinkBox<T, PreT, subT>
    {
        /// <summary>初始化空子节点列表。</summary>
        public LinkBox(){
            this.children = new List<LinkBox<T, PreT, subT>>();
        }
        /// <summary>
        /// 父盒子
        /// </summary>
        public LinkBox<T, PreT, subT> parent;
        /// <summary>
        /// 内容元素
        /// </summary>
        public List<LinkBox<T, PreT, subT>> children;

        /// <summary>整棵结构的根节点引用。</summary>
        public LinkBox<T, PreT, subT> root;

        /// <summary>当前焦点节点（遍历/构建游标）。</summary>
        public LinkBox<T, PreT, subT> focus;
        /// <summary>
        /// 是否为盒子
        /// </summary>
        public bool isBox;

        /// <summary>叶子节点承载的值（非盒子时有效）。</summary>
        public T value;

        /// <summary>
        /// 是否顶级
        /// </summary>
        public bool isTop;
        /// <summary>
        /// 否定盒子。会在最终的执行前增加not
        /// </summary>
        public bool isNot;
        /// <summary>
        /// 前缀
        /// </summary>
        private PreT _prefix;
        /// <summary>
        /// 后缀
        /// </summary>
        private subT _subfix;

        /// <summary>链接前缀（叶子节点）。</summary>
        public PreT Prefix
        {
            get {
                if (this.isBox ) {
                    return default;
                }
                return this._prefix;
            }        
        }
        /// <summary>链接后缀（叶子节点）。</summary>
        public subT Subfix
        {
            get
            {
                if (this.isBox)
                {
                    return default;
                }
                return this._subfix;
            }
        }


        /// <summary>
        /// 创建子节点并下移焦点（进入更深层）。
        /// </summary>
        /// <param name="prefix">链接前缀。</param>
        /// <param name="subfix">链接后缀。</param>
        public LinkBox<T, PreT, subT> sink(PreT prefix, subT subfix )
        {
            var tar = new LinkBox<T, PreT, subT>();
            tar.root = this.root;
            tar.isTop = false;
            tar.parent = this.focus;
            tar._prefix = prefix;
            tar._subfix = subfix;
            focus.children.Add(tar);
            this.focus = tar;
            return tar;
        }
        /// <summary>
        /// 从当前盒子中出来，并进入到上级盒子中，最多可到顶级。
        /// </summary>
        public LinkBox<T, PreT, subT> rise()
        {
            if (focus.isTop) return focus;

            focus = focus.parent;
            return focus;
        }

        /// <summary>在焦点下添加叶子子节点。</summary>
        public LinkBox<T, PreT, subT> add(T item,PreT prefix,subT subfix)
        {

            var tar= new LinkBox<T, PreT, subT>();
            tar._prefix = prefix;
            tar._subfix= subfix;
            tar.isBox = false;
            tar.value = item;
            tar.root = this.root;
            tar.parent= this.focus;
            focus.children.Add(tar);
            return focus;
        }

        /// <summary>对当前节点执行访问委托。</summary>
        public R Visit<R>(Func<LinkBox<T, PreT, subT>, R> onVisit) {
            return onVisit(this);
        }

    }
}
