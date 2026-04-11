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
        /// <summary>内部元素列表。</summary>
        public List<T> content;

        /// <summary>元素之间的分隔符（默认逗号）。</summary>
        public string seprator;

        /// <summary>初始化空集合与默认分隔符。</summary>
        public ContentBag() { 
            this.content = new List<T>();
            this.seprator = ",";
        }

        /// <summary>按索引读写元素。</summary>
        public T this[int index] { 
            get { return this.content[index]; } set { this.content[index] = value; }
        }

        /// <summary>清空集合。</summary>
        public void Clear() {
            if (content == null) {
                return;
            }
            content.Clear();
        }

        /// <summary>元素个数（列表为 null 时视为 0）。</summary>
        public int Count { 
            get {
                if (content == null) return 0;
                return this.content.Count; 
            }
        }

        /// <summary>添加不重复项。</summary>
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
