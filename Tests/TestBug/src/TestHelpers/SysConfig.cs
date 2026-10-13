using System;
using mooSQL.data;
using mooSQL.data.Mapping;

namespace mooSQL.Pure.Tests.TestHelpers
{
    /// <summary>
    /// DDL 类型覆盖实体：用于 preview 建表 SQL 跨方言核验。
    /// </summary>
    [SooTable("HH_SysConfig")]
    public class SysConfig
    {
        [SooColumn("Id", IsPrimaryKey = true, CanBeNull = false)]
        public long Id { get; set; }

        [SooColumn("OrderNo", CanBeNull = true)]
        public int? OrderNo { get; set; }

        [SooColumn("Code", Length = 64, CanBeNull = true)]
        public string Code { get; set; } = string.Empty;

        [SooColumn("Enabled", CanBeNull = false)]
        public bool Enabled { get; set; }

        [SooColumn("Amount", DataType = DataFam.Decimal, Precision = 18, Scale = 2, CanBeNull = true)]
        public decimal? Amount { get; set; }

        [SooColumn("CreatedAt", CanBeNull = true)]
        public DateTime? CreatedAt { get; set; }

        [SooColumn("RowId", CanBeNull = true)]
        public Guid? RowId { get; set; }

        [SooColumn("Remark", DataType = DataFam.Text, CanBeNull = true)]
        public string Remark { get; set; } = string.Empty;

        [SooColumn("Payload", DataType = DataFam.Json, CanBeNull = true)]
        public string Payload { get; set; } = string.Empty;

        [SooColumn("Meta", DataType = DataFam.BinaryJson, CanBeNull = true)]
        public string Meta { get; set; } = string.Empty;

        [SooColumn("BlobData", DataType = DataFam.Binary, CanBeNull = true)]
        public byte[] BlobData { get; set; } = Array.Empty<byte>();

        [SooColumn("Embedding", DataType = DataFam.Vector, Length = 8, CanBeNull = true)]
        public string Embedding { get; set; } = string.Empty;
    }
}
