using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model
{
    /// <summary>
    /// 可定义别名的类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Nickable<T>
    {
        /// <summary>主体对象。</summary>
        public T Body { get; set; }

        /// <summary>别名或显示名。</summary>
        public virtual string NickName { get; set; }
    }
}
