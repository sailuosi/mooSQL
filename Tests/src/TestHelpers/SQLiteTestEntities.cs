using mooSQL.data;
using mooSQL.data.Mapping;
using System;

namespace mooSQL.Pure.Tests.TestHelpers
{
    /// <summary>
    /// SQLite 集成测试专用实体：用户
    /// </summary>
    [SooTable("moo_t_user")]
    [GenerateMaterializer]
    public class SQLiteTestUser
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
    /// SQLite 集成测试专用实体：订单（关联用户）
    /// </summary>
    [SooTable("moo_t_order")]
    [GenerateMaterializer]
    public class SQLiteTestOrder
    {
        [SooColumn("id", IsPrimaryKey = true)]
        public int Id { get; set; }

        [SooColumn("user_id")]
        public int UserId { get; set; }

        [SooColumn("order_no")]
        public string OrderNo { get; set; } = string.Empty;

        [SooColumn("amount")]
        public decimal Amount { get; set; }

        [SooColumn("status")]
        public int Status { get; set; }

        [SooColumn("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// SQLite 集成测试专用实体：商品（用于 GROUP BY / 聚合测试）
    /// </summary>
    [SooTable("moo_t_product")]
    [GenerateMaterializer]
    public class SQLiteTestProduct
    {
        [SooColumn("id", IsPrimaryKey = true)]
        public int Id { get; set; }

        [SooColumn("name")]
        public string Name { get; set; } = string.Empty;

        [SooColumn("category")]
        public string Category { get; set; } = string.Empty;

        [SooColumn("price")]
        public decimal Price { get; set; }

        [SooColumn("stock")]
        public int Stock { get; set; }
    }
}
