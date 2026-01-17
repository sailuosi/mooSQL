using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public class TreeListOutput<R>: TreeListOutput<R,object>
    {
        public List<TreeNodeOutput<R>> Nodes { get; set; }

        public int Count { get; set; }

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

    public class TreeNodeOutput<R> { 
        public R? Record { get; set; }
        public List<TreeNodeOutput<R>> Children { get; set; }

        public int Level { get; set; }

        public object PKValue { get; set; }

        public object FKValue { get; set; }

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


    public class TreeListOutput<R,K>
    {
        public List<TreeNodeOutput<R, K>> Nodes { get; set; }

        public int Count { get; set; }

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

    public class TreeNodeOutput<R,K>
    {
        public R? Record { get; set; }
        public List<TreeNodeOutput<R,K>> Children { get; set; }

        public int Level { get; set; }

        public K PKValue { get; set; }

        public K FKValue { get; set; }

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
