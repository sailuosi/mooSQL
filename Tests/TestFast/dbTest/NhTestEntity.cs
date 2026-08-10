using System;
using NPoco;

namespace dbTest
{
    /// <summary>
    /// NHibernate 专用实体（属性需 virtual 才能走默认代理；勿改共享 TestEntity）。
    /// </summary>
    [TableName("TestEntity")]
    [PrimaryKey("Id")]
    public class NhTestEntity
    {
        public virtual int Id { get; set; }
        public virtual byte? F_Byte { get; set; }
        public virtual short? F_Int16 { get; set; }
        public virtual int? F_Int32 { get; set; }
        public virtual long? F_Int64 { get; set; }
        public virtual double? F_Double { get; set; }
        public virtual float? F_Float { get; set; }
        public virtual decimal? F_Decimal { get; set; }
        public virtual bool F_Bool { get; set; }
        public virtual DateTime? F_DateTime { get; set; }
        public virtual string F_String { get; set; }
    }
}
