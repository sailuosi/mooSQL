
#if NET461_OR_GREATER || NET5_0_OR_GREATER


using Microsoft.Data.SqlClient;


using System;
using System.Collections.Generic;
using System.Data;

using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public BulkCopyResult RunCopy(Action<SqlBulkCopy> onRun)
        {

            try
            {
                prepareRun();
                onRun(_innerCopy);
                return new BulkCopyResult()
                {
                    count = _innerCopy.RowsCopied,
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
        public Task<BulkCopyResult> RunCopyAsync(Func<SqlBulkCopy,Task> onRun)

        {

            try
            {
                prepareRun();

                var tar= onRun(_innerCopy);
                var t= tar.ContinueWith(t =>
                {
                    return new BulkCopyResult()
                    {
                        count = _innerCopy.RowsCopied,
                    };
                });
                return t;

            }
            catch (Exception ex)
            {
                var t = new BulkCopyResult()
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
            });
        }



        public override BulkCopyResult WriteToServer(DataTable table)
        {
            return RunCopy((bc) =>
            {
                bc.WriteToServer(table);
            });
        }

        public override BulkCopyResult WriteToServer(IDataReader reader)
        {
            return RunCopy((bc) =>
            {
                bc.WriteToServer(reader);
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
                return bc.WriteToServerAsync(rows, token);
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
#endif