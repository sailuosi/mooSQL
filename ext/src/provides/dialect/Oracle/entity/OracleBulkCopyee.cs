
#if NET6_0_OR_GREATER

using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    internal class OracleBulkCopyee : DbBulkCopy
    {
        public OracleBulkCopy _innerCopy;
        public OracleBulkCopyee(DBInstance DB) : base(DB)
        {
            
        }

        private OracleConnection sqlConnection;



        public override void Dispose()
        {
            if (_innerCopy != null) {
                _innerCopy = null;
            }
            if (sqlConnection != null ) {
                if (sqlConnection.State != ConnectionState.Closed)
                {
                    sqlConnection.Close();
                }
                sqlConnection.Dispose();
            }
        }

        private void prepareRun() {
            if (this._innerCopy == null) {
                sqlConnection = (OracleConnection)this.DB.dialect.getConnection();
                _innerCopy = new OracleBulkCopy(sqlConnection);
            }

            _innerCopy.DestinationTableName= this.TargetTableName;
            _innerCopy.BulkCopyTimeout = this.TimeOut;
            _innerCopy.NotifyAfter =this.NotifyAfter;

            if (this.MapBag.Maps.Count > 0) {
                foreach (var map in this.MapBag.Maps) {

                    var fmap = new OracleBulkCopyColumnMapping(map.srcIndex,map.tarName);

                    _innerCopy.ColumnMappings.Add(fmap);
                    
                }
            }
            if (sqlConnection.State != ConnectionState.Open)
            {
                sqlConnection.Open();
            }
        }

        public BulkCopyResult RunCopy(Func<OracleBulkCopy, int> onRun) {
            
            try
            {
                prepareRun();
                var res= onRun(_innerCopy);
                return new BulkCopyResult()
                {
                    count = res
                }; 
            }
            catch (Exception ex) {
                return new BulkCopyResult()
                {
                    count = -1,
                };
            }
            finally
            {
                if (this.AutoDispose) { 
                    this.Dispose();
                }
            }
        }



        public override BulkCopyResult WriteToServer(DataRow[] rows)
        {
            return RunCopy((bc) =>
            {
                bc.WriteToServer(rows);
                return rows.Length;
            });
        }



        public override BulkCopyResult WriteToServer(DataTable table)
        {
            return RunCopy((bc) =>
            {
                bc.WriteToServer(table);
                return table.Rows.Count;
            });
        }

        public override BulkCopyResult WriteToServer(IDataReader reader)
        {
            return RunCopy((bc) =>
            {
                 bc.WriteToServer(reader);
                return reader.RecordsAffected;
            });
        }


    }
}
#endif