using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 分页输出结果，Items表示当前页的数据，Total表示总记录数，PageSize和PageNum表示分页信息
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PageOutput<T>
    {
        public IEnumerable<T> Items { get; set; } = default!;

        public int Total { get; set; }

        public int PageSize { get; set; }

        public int PageNum { get; set; }
        /// <summary>
        /// 汇总行
        /// </summary>
        public Dictionary<string, object> Summary { get; set; }
    }
    /// <summary>
    /// 分页输出支持动态属性的版本
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PageSumOutput<T>:Dictionary<string,object>
    {
        public IEnumerable<T> Items {
            get { 
                return this["Items"] as IEnumerable<T> ?? new List<T>();
            }
            set { 
                this["Items"] = value;
            }
        }
        public int Total { 
            get { 
                return this["Total"] as int? ?? 0;
            }
            set { 
                this["Total"] = value;
            }
        }
        public int PageSize { 
            get { 
                return this["PageSize"] as int? ?? 0;
            }
            set { 
                this["PageSize"] = value;
            }
        }
        public int PageNum { 
            get { 
                return this["PageNum"] as int? ?? 0;
            }
            set { 
                this["PageNum"] = value;
            }
        }
    }
}
