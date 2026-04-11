using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 树列表
    /// </summary>
    /// <typeparam name="R"></typeparam>
    public class TreeListOutput<R>: TreeListOutput<R,object>
    {
        /// <summary>
        /// 节点
        /// </summary>
        public List<TreeNodeOutput<R>> Nodes { get; set; }

        /// <summary>
        /// 遍历
        /// </summary>
        /// <typeparam name="T2"></typeparam>
        /// <param name="domapping"></param>
        /// <returns></returns>
        public List<T2> map<T2>(Func<TreeNodeOutput<R>, T2, T2> domapping) { 
            var result = new List<T2>();
            if (this.Nodes != null) { 
                foreach (var node in this.Nodes) { 
                    result.Add(node.map(default(T2),domapping));
                }
            }
            return result;
        }
    }
    /// <summary>
    /// 树节点
    /// </summary>
    /// <typeparam name="R"></typeparam>
    public class TreeNodeOutput<R> { 
        /// <summary>
        /// 行数据
        /// </summary>
        public R? Record { get; set; }
        /// <summary>
        /// 下级
        /// </summary>
        public List<TreeNodeOutput<R>> Children { get; set; }
        /// <summary>
        /// 深度
        /// </summary>
        public int Level { get; set; }
        /// <summary>
        /// 主键
        /// </summary>
        public object PKValue { get; set; }
        /// <summary>
        /// 外键
        /// </summary>
        public object FKValue { get; set; }
        /// <summary>
        /// 遍历
        /// </summary>
        /// <typeparam name="T2"></typeparam>
        /// <param name="parent"></param>
        /// <param name="domapping"></param>
        /// <returns></returns>
        public T2 map<T2>(T2 parent, Func<TreeNodeOutput<R>,T2,T2> domapping) { 
            var me= domapping(this,parent);
            if (this.Children != null) { 
                foreach (var child in this.Children) { 
                    child.map(me,domapping);
                }
            }
            return me;
        }
    }

    /// <summary>
    /// 树列表输出
    /// </summary>
    /// <typeparam name="R"></typeparam>
    /// <typeparam name="K"></typeparam>
    public class TreeListOutput<R,K>
    {
        /// <summary>
        /// 节点
        /// </summary>
        public List<TreeNodeOutput<R, K>> Nodes { get; set; }
        /// <summary>
        /// 计数
        /// </summary>
        public int Count { get; set; }
        /// <summary>
        /// 遍历
        /// </summary>
        /// <typeparam name="T2"></typeparam>
        /// <param name="domapping"></param>
        /// <returns></returns>
        public List<T2> map<T2>(Func<TreeNodeOutput<R, K>, T2, T2> domapping)
        {
            var result = new List<T2>();
            if (domapping == null) { 
                return result;
            }
            if (this.Nodes != null)
            {
                foreach (var node in this.Nodes)
                {
                    var t = node.map(default(T2), domapping);
                    if(t != null) result.Add(t);
                }
            }
            return result;
        }
        /// <summary>
        /// 塌缩为列表
        /// </summary>
        /// <returns></returns>
        public TreeListOutput<R> collapse() {
            var cnodes = this.map<TreeNodeOutput<R>>((row,parent) =>
            {
                var t = new TreeNodeOutput<R>()
                {
                    Record = row.Record,
                    Level = row.Level,
                    PKValue = row.PKValue,
                    FKValue = row.FKValue,
                    Children= new List<TreeNodeOutput<R>>()
                };
                if (parent != null) { 
                    parent.Children.Add(t);
                }
                return t;
            });
            return new TreeListOutput<R>
            {
                Nodes = cnodes,
            };
        }
    }
    /// <summary>
    /// 树节点
    /// </summary>
    /// <typeparam name="R"></typeparam>
    /// <typeparam name="K"></typeparam>
    public class TreeNodeOutput<R,K>
    {
        /// <summary>
        /// 行记录
        /// </summary>
        public R? Record { get; set; }
        /// <summary>
        /// 子级
        /// </summary>
        public List<TreeNodeOutput<R,K>> Children { get; set; }
        /// <summary>
        /// 层
        /// </summary>
        public int Level { get; set; }
        /// <summary>
        /// 主键
        /// </summary>
        public K PKValue { get; set; }
        /// <summary>
        /// 外键
        /// </summary>
        public K FKValue { get; set; }
        /// <summary>
        /// 遍历
        /// </summary>
        /// <typeparam name="T2"></typeparam>
        /// <param name="parent"></param>
        /// <param name="domapping"></param>
        /// <returns></returns>
        public T2 map<T2>(T2 parent, Func<TreeNodeOutput<R,K>, T2, T2> domapping)
        {
            var me = domapping(this, parent);
            if (this.Children != null)
            {
                foreach (var child in this.Children)
                {
                    child.map(me, domapping);
                }
            }
            return me;
        }


    }
}
