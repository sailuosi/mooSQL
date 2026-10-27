using System;
using LiteOrm.Common;

namespace dbTest
{
    /// <summary>
    /// LiteOrm 专用实体，映射共享表 TestEntity（独立类型，避免特性污染其它 ORM 适配器）。
    /// </summary>
    [Table("TestEntity")]
    public class LiteOrmTestEntity
    {
        [Column("Id", IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        [Column("F_Byte")]
        public byte? F_Byte { get; set; }
        [Column("F_Int16")]
        public short? F_Int16 { get; set; }
        [Column("F_Int32")]
        public int? F_Int32 { get; set; }
        [Column("F_Int64")]
        public long? F_Int64 { get; set; }
        [Column("F_Double")]
        public double? F_Double { get; set; }
        [Column("F_Float")]
        public float? F_Float { get; set; }
        [Column("F_Decimal")]
        public decimal? F_Decimal { get; set; }
        [Column("F_Bool")]
        public bool F_Bool { get; set; }
        [Column("F_DateTime")]
        public DateTime? F_DateTime { get; set; }
        [Column("F_String")]
        public string F_String { get; set; }
    }

    [Table("TestEntityItem")]
    public class LiteOrmTestEntityItem
    {
        [Column("TestEntityId")]
        public int TestEntityId { get; set; }
        [Column("Name")]
        public string Name { get; set; }
    }
}
