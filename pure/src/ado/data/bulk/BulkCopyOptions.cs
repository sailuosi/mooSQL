using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 类型 BulkCopyOptions。
    /// </summary>
    public class BulkCopyOptions
    {
        /// <summary>
        /// 字段 MaxBatchSize（int?）。
        /// </summary>
        public int? MaxBatchSize = default;

        /// <summary>
        /// 字段 BulkCopyTimeout（int?）。
        /// </summary>
        public int? BulkCopyTimeout = default;
        /// <summary>
        /// 字段 BulkCopyType（BulkCopyType）。
        /// </summary>
        public BulkCopyType BulkCopyType = default;
        /// <summary>
        /// 字段 CheckConstraints（bool?）。
        /// </summary>
        public bool? CheckConstraints = default;

        /// <summary>
        /// 字段 KeepIdentity（bool?）。
        /// </summary>
        public bool? KeepIdentity = default;

        /// <summary>
        /// 字段 TableLock（bool?）。
        /// </summary>
        public bool? TableLock = default;

        /// <summary>
        /// 字段 KeepNulls（bool?）。
        /// </summary>
        public bool? KeepNulls = default;

        /// <summary>
        /// 字段 FireTriggers（bool?）。
        /// </summary>
        public bool? FireTriggers = default;

        /// <summary>
        /// 字段 UseInternalTransaction（bool?）。
        /// </summary>
        public bool? UseInternalTransaction = default;

        /// <summary>
        /// 字段 ServerName（string?）。
        /// </summary>
        public string? ServerName = default;

        /// <summary>
        /// 字段 DatabaseName（string?）。
        /// </summary>
        public string? DatabaseName = default;

        /// <summary>
        /// 字段 SchemaName（string?）。
        /// </summary>
        public string? SchemaName = default;

        /// <summary>
        /// 字段 TableName（string?）。
        /// </summary>
        public string? TableName = default;
        /// <summary>
        /// 字段 TableOptions（TableOptions）。
        /// </summary>
        public TableOptions TableOptions = default;
        /// <summary>
        /// 字段 NotifyAfter（int）。
        /// </summary>
        public int NotifyAfter = default;
        /// <summary>
        /// 字段 RowsCopiedCallback（Action<BulkCopyRowsCopied>?）。
        /// </summary>
        public Action<BulkCopyRowsCopied>? RowsCopiedCallback = default;
        /// <summary>
        /// 字段 UseParameters（bool）。
        /// </summary>
        public bool UseParameters = default;

        /// <summary>
        /// 字段 MaxParametersForBatch（int?）。
        /// </summary>
        public int? MaxParametersForBatch = default;

        /// <summary>
        /// 字段 MaxDegreeOfParallelism（int?）。
        /// </summary>
        public int? MaxDegreeOfParallelism = default;

        /// <summary>
        /// 字段 WithoutSession（bool）。
        /// </summary>
        public bool WithoutSession = default;

        /// <summary>
        /// 初始化 BulkCopyOptions。
        /// </summary>
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