
using mooSQL.data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace HHNY.NET.Core
{
    /// <summary>
    /// 插入时使用BulkTable,更新时使用 batchsql
    /// </summary>
    public class MatchBulk: BulkBatchBase
    {
        public MatchBulk(string tbname, int position)
        {
            tableName = tbname;
            //keyCol = tbname + "OID";
            this.position = position;
            this.bulk = DBCash.newBulk(tbname, position);
            this.batchSQL = DBCash.newBatchSQL(position);
        }

        public int position = 0;
    

    }
}
