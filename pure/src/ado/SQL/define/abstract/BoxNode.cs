using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model
{
    /// <summary>
    /// 可以被括号包裹以嵌套的成员
    /// </summary>
    public class BoxNode<T>
    {
        /// <summary>
        /// 父盒子
        /// </summary>
        public BoxNode<T> parent;
        /// <summary>
        /// 内容元素
        /// </summary>
        public List<BoxNode<T>> children;

        /// <summary>
        /// 是否为盒子
        /// </summary>
        public bool isBox;
        /// <summary>
        /// 是否顶级
        /// </summary>
        public bool isTop;
        /// <summary>
        /// 否定盒子。会在最终的执行前增加not
        /// </summary>
        public bool isNot;
    }
}
