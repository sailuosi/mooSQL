using System;
using TORM;

namespace dbTest
{
    /// <summary>
    /// Core.ORM 专用实体（勿把 [OrmTable] 打在共享 TestEntity 上：其源码生成器会注入成员，干扰 CRL 等其它 ORM 映射）。
    /// </summary>
    [OrmTable(TableName = "TestEntity")]
    public partial class CoreOrmTestEntity
    {
        [OrmColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        public byte? F_Byte { get; set; }
        public short? F_Int16 { get; set; }
        public int? F_Int32 { get; set; }
        public long? F_Int64 { get; set; }
        public double? F_Double { get; set; }
        public float? F_Float { get; set; }
        public decimal? F_Decimal { get; set; }
        public bool F_Bool { get; set; }
        public DateTime? F_DateTime { get; set; }
        public string F_String { get; set; }
    }
}
