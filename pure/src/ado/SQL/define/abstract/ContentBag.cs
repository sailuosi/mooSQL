using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model
{
    /// <summary>
    /// SQL的内容集合
    /// </summary>
    public class ContentBag<T>
    {

        public List<T> content;

        public string seprator;

        public ContentBag() { 
            this.content = new List<T>();
            this.seprator = ",";
        }

        public T this[int index] { 
            get { return this.content[index]; } set { this.content[index] = value; }
        }

        public void Clear() {
            if (content == null) {
                return;
            }
            content.Clear();
        }

        public int Count { 
            get {
                if (content == null) return 0;
                return this.content.Count; 
            }
        }

        public void Add(T item) {
            if (content == null) { 
                content = new List<T>();
            }
            if (content.Contains(item)) {
                return;
            }
            content.Add(item);
        }
    }
}
