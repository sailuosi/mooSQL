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

        public T Body { get; set; }

        public virtual string NickName { get; set; }
    }
}
