using System;
using System.ComponentModel.DataAnnotations;
using mooSQL.data;
using mooSQL.data.Mapping;

namespace mooSQL.Pure.Tests.TestHelpers
{
    /// <summary>
    /// 测试用的实体类
    /// </summary>
    [SooTable("test_users")]
    [GenerateMaterializer]
    public class TestUser
    {
        [SooColumn("id", IsPrimaryKey = true)]
        public int Id { get; set; }

        [SooColumn("name")]
        public string Name { get; set; } = string.Empty;

        [SooColumn("email")]
        public string Email { get; set; } = string.Empty;

        [SooColumn("age")]
        public int? Age { get; set; }

        [SooColumn("created_at")]
        public DateTime CreatedAt { get; set; }

        [SooColumn("is_active")]
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// 枚举默认整型映射测试用状态。
    /// </summary>
    public enum TestMenuTypeEnum
    {
        Dir = 0,
        Menu = 1,
        Btn = 2,
    }

    /// <summary>
    /// 含默认枚举列（无自定义 DataType）与显式 VarChar 枚举列。
    /// </summary>
    [SooTable("test_enum_menu")]
    public class TestEnumMenu
    {
        [SooColumn("id", IsPrimaryKey = true)]
        public int Id { get; set; }

        /// <summary>无 DataType：建表与 Clip 应走底层 int。</summary>
        [SooColumn("type")]
        public TestMenuTypeEnum Type { get; set; }

        /// <summary>显式字符串：应保持 varchar，Clip 绑枚举名。</summary>
        [SooColumn("status", DataType = DataFam.VarChar, Length = 32)]
        public TestMenuTypeEnum Status { get; set; }
    }

    /// <summary>
    /// 测试用的订单实体类
    /// </summary>
    [SooTable("test_orders")]
    public class TestOrder
    {
        [SooColumn("id", IsPrimaryKey = true)]
        public int Id { get; set; }

        [SooColumn("user_id")]
        public int UserId { get; set; }

        [SooColumn("order_no")]
        public string OrderNo { get; set; } = string.Empty;

        [SooColumn("amount")]
        public decimal Amount { get; set; }

        [SooColumn("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
