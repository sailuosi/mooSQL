using FreeSql.DataAnnotations;
using mooSQL.data;
using NPoco;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dbTest
{
    [SooTable("TestEntity")]
    [TableName("TestEntity")]
    [PrimaryKey("Id")]
    public class TestEntity
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        [FreeSql.DataAnnotations.Column(IsIdentity =true)]
        [SooColumn("Id", IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        [SooColumn("F_Byte")]
        public byte? F_Byte { get; set; }
        [SooColumn("F_Int16")]
        public Int16? F_Int16 { get; set; }
        [SooColumn("F_Int32")]
        public int? F_Int32 { get; set; }
        [SooColumn("F_Int64")]
        public long? F_Int64 { get; set; }
        [SooColumn("F_Double")]
        public double? F_Double { get; set; }
        [SooColumn("F_Float")]
        public float? F_Float { get; set; }
        [SooColumn("F_Decimal")]
        public decimal? F_Decimal { get; set; }
        [SooColumn("F_Bool")]
        public bool F_Bool { get; set; }
        [SooColumn("F_DateTime")]
        public DateTime? F_DateTime { get; set; }
        //public Guid? F_Guid { get; set; }
        [SooColumn("F_String")]
        public string F_String { get; set; }
    }
    public class TestEntity2
    {
        public int Id { get; set; }
        public byte? F_Byte { get; set; }
        public Int16? F_Int16 { get; set; }
        public int? F_Int32 { get; set; }
        public long? F_Int64 { get; set; }
        public double? F_Double { get; set; }
        public float? F_Float { get; set; }
        public decimal? F_Decimal { get; set; }
        public bool F_Bool { get; set; }
        public DateTime? F_DateTime { get; set; }
        //public Guid? F_Guid { get; set; }
        public string F_String { get; set; }
    }
    public enum MType
    {
        a, b
    }

    public class TestEntityItem
    {
        public int TestEntityId { get; set; }
        public string Name { get; set; }
    }
    public class testJsonColumn
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        [FreeSql.DataAnnotations.Column(IsIdentity = true)]
        public int Id { get; set; }
        [SugarColumn(IsJson = true)]
        [JsonMap]
        public TestEntityItem EntityItem { get; set; }
    }
    public class testJsonColumnDto
    {
        public int Id2 { get; set; }
        public TestEntityItem EntityItem { get; set; }
    }
}
