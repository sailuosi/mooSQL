using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model
{
    /// <summary>
    /// 链接集合，第一个添加的是首位，后续添加的必须带有链接信息。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LinkedBag<T>
    {

        public string typeCode;

        public List<LinkedItem<T>> items;

        public LinkedBag() { 
            this.items = new List<LinkedItem<T>>();
        }

    }
    /// <summary>
    /// 链集合成员项
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LinkedItem<T> {
        /// <summary>
        /// 链接字符串
        /// </summary>
        public string link;
        /// <summary>
        /// 是否第一个
        /// </summary>
        public bool isFirst;
        /// <summary>
        /// 索引
        /// </summary>
        public int index;
        /// <summary>
        /// 内容
        /// </summary>
        public T item;



    }
}
