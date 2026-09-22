using Dm;
using System;
using System.Data;
using System.Data.Common;

namespace mooSQL.data
{
    /// <summary>
    /// 达梦 BulkCopy 封装（Dm.DmBulkCopy）。
    /// </summary>
    internal class DMBulkCopyee : DbBulkCopy
    {
        public DmBulkCopy _innerCopy;
        private DmConnection sqlConnection;

        public DMBulkCopyee(DBInstance DB) : base(DB)
        {
        }

        public override void Dispose()
        {
            if (_innerCopy != null)
                _innerCopy = null;

            if (sqlConnection != null)
            {
                if (sqlConnection.State != ConnectionState.Closed)
                    sqlConnection.Close();
                sqlConnection.Dispose();
                sqlConnection = null;
            }
        }

        private void prepareRun()
        {
            if (_innerCopy == null)
            {
                sqlConnection = (DmConnection)DB.dialect.getConnection();
                _innerCopy = new DmBulkCopy(sqlConnection);
            }

            _innerCopy.DestinationTableName = TargetTableName;
            _innerCopy.BulkCopyTimeout = TimeOut;
            _innerCopy.NotifyAfter = NotifyAfter;

            if (MapBag.Maps.Count > 0)
            {
                foreach (var map in MapBag.Maps)
                {
                    var fmap = new DmBulkCopyColumnMapping(map.srcIndex, map.tarName);
                    _innerCopy.ColumnMappings.Add(fmap);
                }
            }

            if (sqlConnection.State != ConnectionState.Open)
                sqlConnection.Open();
        }

        public BulkCopyResult RunCopy(Func<DmBulkCopy, int> onRun)
        {
            try
            {
                prepareRun();
                var res = onRun(_innerCopy);
                return new BulkCopyResult { count = res };
            }
            catch
            {
                return new BulkCopyResult { count = -1 };
            }
            finally
            {
                if (AutoDispose)
                    Dispose();
            }
        }

        public override BulkCopyResult WriteToServer(DataRow[] rows)
            => RunCopy(bc =>
            {
                bc.WriteToServer(rows);
                return rows.Length;
            });

        public override BulkCopyResult WriteToServer(DataTable table)
            => RunCopy(bc =>
            {
                bc.WriteToServer(table);
                return table.Rows.Count;
            });

        public override BulkCopyResult WriteToServer(IDataReader reader)
            => RunCopy(bc =>
            {
                if (reader is DbDataReader dbReader)
                    bc.WriteToServer(dbReader);
                else
                    throw new NotSupportedException("达梦 BulkCopy 需要 DbDataReader。");
                return reader.RecordsAffected;
            });
    }
}
