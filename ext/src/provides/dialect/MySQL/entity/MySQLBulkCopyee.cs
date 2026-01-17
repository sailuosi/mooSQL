using DocumentFormat.OpenXml.Spreadsheet;
using MySqlConnector;
using Newtonsoft.Json.Linq;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    internal class MySQLBulkCopyee : DbBulkCopy
    {
        public MySqlBulkCopy _innerCopy;
        public MySQLBulkCopyee(DBInstance DB) : base(DB)
        {
            
        }

        private MySqlConnection sqlConnection;



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
                sqlConnection = (MySqlConnection)this.DB.dialect.getConnection();
                _innerCopy = new MySqlBulkCopy(sqlConnection);
            }

            _innerCopy.DestinationTableName= this.TargetTableName;
            _innerCopy.BulkCopyTimeout = this.TimeOut;
            _innerCopy.NotifyAfter =this.NotifyAfter;

            if (this.MapBag.Maps.Count > 0) {
                foreach (var map in this.MapBag.Maps) {

                    var fmap = new MySqlBulkCopyColumnMapping(map.srcIndex,map.tarName);

                    _innerCopy.ColumnMappings.Add(fmap);
                    
                }
            }
            if (sqlConnection.State != ConnectionState.Open) { 
                sqlConnection.Open();
            }
        }

        public BulkCopyResult RunCopy(Func<MySqlBulkCopy,MySqlBulkCopyResult> onRun) {
            
            try
            {
                prepareRun();
                var res= onRun(_innerCopy);
                return new BulkCopyResult()
                {
                    count = res.RowsInserted,
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
#if NET451
        public Task<BulkCopyResult> RunCopyAsync(Func<MySqlBulkCopy,Task<MySqlBulkCopyResult>> onRun)
        {

            try
            {
                prepareRun();

                var res = onRun(_innerCopy);
                Task<BulkCopyResult> r = res.ContinueWith(t => {

                    var br = new BulkCopyResult()
                    {
                        count = t.Result.RowsInserted,
                    };
                    return br;
                });
                return r;

            }
            catch (Exception ex)
            {
                var t= new BulkCopyResult()
                {
                    count = -1,
                };
                return Task.FromResult(t);
            }
            finally
            {
                if (this.AutoDispose)
                {
                    this.Dispose();
                }
            }
        }
#else
        public Task<BulkCopyResult> RunCopyAsync(Func<MySqlBulkCopy,ValueTask<MySqlBulkCopyResult>> onRun)

        {

            try
            {
                prepareRun();

                var res = onRun(_innerCopy);
                Task<BulkCopyResult> r = res.AsTask().ContinueWith(t => {

                    var br = new BulkCopyResult()
                    {
                        count = t.Result.RowsInserted,
                    };
                    return br;
                });
                return r;

            }
            catch (Exception ex)
            {
                var t= new BulkCopyResult()
                {
                    count = -1,
                };
                return Task.FromResult(t);
            }
            finally
            {
                if (this.AutoDispose)
                {
                    this.Dispose();
                }
            }
        }
#endif
        public override BulkCopyResult WriteToServer(DataRow[] rows)
        {
            return RunCopy((bc) =>
            {
                return bc.WriteToServer(rows, MapBag.Maps.Count);
            });
        }



        public override BulkCopyResult WriteToServer(DataTable table)
        {
            return RunCopy((bc) =>
            {
                return bc.WriteToServer(table);
            });
        }

        public override BulkCopyResult WriteToServer(IDataReader reader)
        {
            return RunCopy((bc) =>
            {
                return bc.WriteToServer(reader);
            });
        }

        public override Task<BulkCopyResult> WriteToServerAsync(DataTable table, CancellationToken token)
        {
            return RunCopyAsync((bc) =>
            {
                return bc.WriteToServerAsync(table, token);
            });
        }

        public override Task<BulkCopyResult> WriteToServerAsync(DataRow[] rows, CancellationToken token)
        {
            return RunCopyAsync((bc) =>
            {
                return bc.WriteToServerAsync(rows, MapBag.Maps.Count, token);
            });
        }
        public override Task<BulkCopyResult> WriteToServerAsync(IDataReader reader, CancellationToken token)
        {
            return RunCopyAsync((bc) =>
            {
                return bc.WriteToServerAsync(reader, token);
            });
        }
    }
}
