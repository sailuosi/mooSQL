using System;

namespace dbTest
{
    /// <summary>Core.ORM Anonymous 场景投影 DTO（该库无法 materialize 匿名类型）。</summary>
    public class CoreOrmAnonymousDto
    {
        public int Id { get; set; }
        public float? F_Float { get; set; }
        public bool F_Bool { get; set; }
        public DateTime? F_DateTime { get; set; }
        public decimal? F_Decimal { get; set; }
        public double? F_Double { get; set; }
        public long? F_Int64 { get; set; }
    }
}
