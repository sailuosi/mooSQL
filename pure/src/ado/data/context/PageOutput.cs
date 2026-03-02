using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 翻页查询结果，Items表示当前页的数据，Total表示总记录数，PageSize和PageNum表示分页信息
    /// </summary>
    public class PagedDataTable
    {
        public DataTable Items { get; set; } = default!;

        public int Total { get; set; }

        public int PageSize { get; set; }

        public int PageNum { get; set; }
    }
    /// <summary>
    /// 支持汇总数据的分页结果，Items中包含分页数据和汇总数据，Total表示分页数据的总记录数，PageSize和PageNum表示分页信息
    /// </summary>
    public class PagedSumDataTable:Dictionary<string,object>
    {
        public DataTable Items {
            get { 
                return this["Items"] as DataTable ?? new DataTable();
            }
            set { 
                this["Items"] = value;
            }
        }

        public int? Total { 
            get { 
                return this["Total"] as int?;
            }
            set { 
                this["Total"] = value;
            }
        }

        public int? PageSize { 
            get { 
                return this["PageSize"] as int?;
            }
            set { 
                this["PageSize"] = value;
            }
        }

        public int? PageNum { 
            get { 
                return this["PageNum"] as int?;
            }
            set { 
                this["PageNum"] = value;
            }
        }
    }
}
