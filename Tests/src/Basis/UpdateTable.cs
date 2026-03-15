using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data;

namespace HHNY.NET.Core
{
     //批量更新功能 datatable.update
    public class UpdateTable: EditTable
    {

        public int position = 0;

        public UpdateTable(string tableName, string selectStr, int position) :base(tableName,selectStr)
        {
            this.selectStr = selectStr;
            this.tableName = tableName;
            this.keyColName = tableName + "OID";
            this.db = DBCash.GetDBInstance(position);
            //this.init();
        }
        public UpdateTable(string tableName,int position):base(tableName) 
        {
            this.selectStr = string.Format("select * from {0}", tableName);
            this.tableName = tableName;
            this.keyColName = tableName + "OID";
            this.db = DBCash.GetDBInstance(position);
            //this.init();
        }


 
  

    }
}
