
#if NET451
using System.Data.SqlClient;

using System;
using System.Collections.Generic;
using System.Data;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPOI.SS.Formula.Functions;

namespace mooSQL.data
{
    internal class MSSQBulkCopyee : DbBulkCopy
    {
        public MSSQBulkCopyee(DBInstance DB) : base(DB)
        {

        }

        public override void Dispose()
        {
            if (_innerCopy != null)
            {
                _innerCopy = null;
            }
            if (sqlConnection != null)
            {
                if (sqlConnection.State != ConnectionState.Closed)
                {
                    sqlConnection.Close();
                }
                sqlConnection.Dispose();
            }
        }


        public SqlBulkCopy _innerCopy;

        private SqlConnection sqlConnection;

        private void prepareRun()
        {
            if (this._innerCopy == null)
            {
                sqlConnection = (SqlConnection)this.DB.dialect.getConnection();
                _innerCopy = new SqlBulkCopy(sqlConnection);
            }

            _innerCopy.DestinationTableName = this.TargetTableName;
            _innerCopy.BulkCopyTimeout = this.TimeOut;
            _innerCopy.NotifyAfter = this.NotifyAfter;

            if (this.MapBag.Maps.Count > 0)
            {
                foreach (var map in this.MapBag.Maps)
                {

                    var fmap = new SqlBulkCopyColumnMapping(map.srcIndex, map.tarName);

                    _innerCopy.ColumnMappings.Add(fmap);

                }
            }
            if (sqlConnection.State != ConnectionState.Open)
            {
                sqlConnection.Open();
            }
        }
        public BulkCopyResult RunCopy(Func<SqlBulkCopy,int> onRun)
        {

            try
            {
                prepareRun();
                var r= onRun(_innerCopy);
                return new BulkCopyResult()
                {
                    count = r,
                };
            }
            catch (Exception ex)
            {
                return new BulkCopyResult()
                {
                    count = -1,
                };
            }
            finally
            {
                if (this.AutoDispose)
                {
                    this.Dispose();
                }
            }
        }
        public Task<BulkCopyResult> RunCopyAsync(Func<SqlBulkCopy,Task<int>> onRun)
        {

            try
            {
                prepareRun();

                var res = onRun(_innerCopy);
                Task<BulkCopyResult> r = res.ContinueWith(t => {

                    var br = new BulkCopyResult()
                    {
                        count = t.Result,
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

        public override Task<BulkCopyResult> WriteToServerAsync(DataTable table, CancellationToken token)
        {
            return RunCopyAsync((bc) =>
            {
                var t= bc.WriteToServerAsync(table, token);
                return t.ContinueWith(t => { 
                    return table.Rows.Count;
                });
            });
        }

        public override Task<BulkCopyResult> WriteToServerAsync(DataRow[] rows, CancellationToken token)
        {
            return RunCopyAsync((bc) =>
            {
                var t = bc.WriteToServerAsync(rows, token);
                return t.ContinueWith(t => {
                    return rows.Length;
                });
            });
        }
        public override Task<BulkCopyResult> WriteToServerAsync(IDataReader reader, CancellationToken token)
        {
            return RunCopyAsync((bc) =>
            {
                var t = bc.WriteToServerAsync(reader, token);
                return t.ContinueWith(t => {
                    return reader.RecordsAffected;
                });
            });
        }
    }
}
#endif