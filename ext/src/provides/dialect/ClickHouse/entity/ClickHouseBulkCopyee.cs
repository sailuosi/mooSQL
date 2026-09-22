#if NET6_0_OR_GREATER
using ClickHouse.Driver.ADO;
using ClickHouse.Driver.Copy;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// ClickHouse 批量写入：封装 <see cref="ClickHouseBulkCopy"/>（同步包装 async API）。
    /// </summary>
    public class ClickHouseBulkCopyee : DbBulkCopy
    {
        public ClickHouseBulkCopyee(DBInstance db) : base(db) { }

        public override BulkCopyResult WriteToServer(DataTable table)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (table.Rows.Count == 0) return new BulkCopyResult { count = 0 };
            using var reader = table.CreateDataReader();
            return WriteReader(reader, table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray());
        }

        public override BulkCopyResult WriteToServer(DataRow[] rows)
        {
            if (rows == null || rows.Length == 0)
                return new BulkCopyResult { count = 0 };
            var table = rows[0].Table.Clone();
            foreach (var r in rows)
                table.ImportRow(r);
            return WriteToServer(table);
        }

        public override BulkCopyResult WriteToServer(IDataReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            return WriteReader(reader, null);
        }

        BulkCopyResult WriteReader(IDataReader reader, string[] columnNames)
        {
            if (string.IsNullOrWhiteSpace(TargetTableName))
                throw new InvalidOperationException("TargetTableName 未设置。");

            return WriteCore(async conn =>
            {
                var bulk = columnNames != null && columnNames.Length > 0
                    ? new ClickHouseBulkCopy(conn)
                    {
                        DestinationTableName = TargetTableName,
                        BatchSize = BatchSize > 0 ? BatchSize : 1000,
                        ColumnNames = columnNames
                    }
                    : new ClickHouseBulkCopy(conn)
                    {
                        DestinationTableName = TargetTableName,
                        BatchSize = BatchSize > 0 ? BatchSize : 1000
                    };

                using (bulk)
                {
                    await bulk.InitAsync().ConfigureAwait(false);
                    await bulk.WriteToServerAsync(reader).ConfigureAwait(false);
                    return new BulkCopyResult { count = (int)Math.Min(int.MaxValue, bulk.RowsWritten) };
                }
            });
        }

        BulkCopyResult WriteCore(Func<ClickHouseConnection, Task<BulkCopyResult>> work)
        {
            var conn = DB.dialect.getConnection() as ClickHouseConnection
                ?? new ClickHouseConnection(DB.config.DBConnectStr);
            var owned = conn.State == ConnectionState.Closed;
            try
            {
                if (owned) conn.Open();
                return work(conn).GetAwaiter().GetResult();
            }
            finally
            {
                if (owned)
                {
                    conn.Close();
                    conn.Dispose();
                }
            }
        }
    }
}
#endif
