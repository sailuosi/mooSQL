using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public class BulkCopyOptions
    {
        public int? MaxBatchSize = default;

        public int? BulkCopyTimeout = default;
        public BulkCopyType BulkCopyType = default;
        public bool? CheckConstraints = default;

        public bool? KeepIdentity = default;

        public bool? TableLock = default;

        public bool? KeepNulls = default;

        public bool? FireTriggers = default;

        public bool? UseInternalTransaction = default;

        public string? ServerName = default;

        public string? DatabaseName = default;

        public string? SchemaName = default;

        public string? TableName = default;
        public TableOptions TableOptions = default;
        public int NotifyAfter = default;
        public Action<BulkCopyRowsCopied>? RowsCopiedCallback = default;
        public bool UseParameters = default;

        public int? MaxParametersForBatch = default;

        public int? MaxDegreeOfParallelism = default;

        public bool WithoutSession = default;

        public BulkCopyOptions()
        {
        }

        BulkCopyOptions(BulkCopyOptions original)
        {
            MaxBatchSize = original.MaxBatchSize;
            BulkCopyTimeout = original.BulkCopyTimeout;
            BulkCopyType = original.BulkCopyType;
            CheckConstraints = original.CheckConstraints;
            KeepIdentity = original.KeepIdentity;
            TableLock = original.TableLock;
            KeepNulls = original.KeepNulls;
            FireTriggers = original.FireTriggers;
            UseInternalTransaction = original.UseInternalTransaction;
            ServerName = original.ServerName;
            DatabaseName = original.DatabaseName;
            SchemaName = original.SchemaName;
            TableName = original.TableName;
            TableOptions = original.TableOptions;
            NotifyAfter = original.NotifyAfter;
            RowsCopiedCallback = original.RowsCopiedCallback;
            UseParameters = original.UseParameters;
            MaxParametersForBatch = original.MaxParametersForBatch;
            MaxDegreeOfParallelism = original.MaxDegreeOfParallelism;
            WithoutSession = original.WithoutSession;
        }


    }
}
