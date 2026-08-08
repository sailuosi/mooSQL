
using mooSQL.data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestMooSQL.src;


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
            this.bulk = new BulkTable(tbname, position);
            this.batchSQL = DBTest.newBatchSQL(position);
        }

        public int position = 0;
    

    }
}
