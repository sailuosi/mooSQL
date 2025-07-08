using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public class TreeListOutput<R>
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
}
