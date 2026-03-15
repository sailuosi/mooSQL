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
