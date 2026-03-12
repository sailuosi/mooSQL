using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 字段映射集合
    /// </summary>
    public class DbBulkFieldMapBag
    {
        /// <summary>源列到目标列的映射列表。</summary>
        public List<DbBulkFieldMap> Maps = new List<DbBulkFieldMap>();

        public DbBulkFieldMapBag Add(DbBulkFieldMap tar)
        {
          
            Maps.Add(tar);
            return this;
        }

        public DbBulkFieldMapBag Add(string src,string tar)
        {
            var tarMap = new DbBulkFieldMap()
            {
                type= BulkMapType.name,
                srcName = src,
                tarName = tar,
            };
            Maps.Add(tarMap);
            return this;
        }
        public DbBulkFieldMapBag Add(int src, int tar)
        {
            var tarMap = new DbBulkFieldMap()
            {
                type= BulkMapType.index,
                srcIndex = src,
                tarIndex = tar,
            };
            Maps.Add(tarMap);
            return this;
        }


    }
}
