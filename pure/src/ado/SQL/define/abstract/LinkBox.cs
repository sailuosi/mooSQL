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

        public LinkBox<T, PreT, subT> root;

        public LinkBox<T, PreT, subT> focus;
        /// <summary>
        /// 是否为盒子
        /// </summary>
        public bool isBox;

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

        public PreT Prefix
        {
            get {
                if (this.isBox ) {
                    return default;
                }
                return this._prefix;
            }        
        }
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
        /// 创建一个新盒子，并进入其中
        /// </summary>
        /// <param name="connector"></param>
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

        public R Visit<R>(Func<LinkBox<T, PreT, subT>, R> onVisit) {
            return onVisit(this);
        }

    }
}
