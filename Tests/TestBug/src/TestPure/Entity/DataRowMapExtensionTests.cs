using FluentAssertions;
using mooSQL.data;
using mooSQL.data.Mapping;
using System;
using System.Data;
using Xunit;

namespace mooSQL.Pure.Tests
{
    public class DataRowMapExtensionTests
    {
        private static DBInstance CreateDb()
        {
            var client = new MooClient();
            client.entityAnalyseFactory.register(new MooEntityAnalyser());
            return new DBInstance { client = client };
        }

        private static DataTable CreateTable(params (string Name, Type Type)[] columns)
        {
            var table = new DataTable();
            foreach (var col in columns)
                table.Columns.Add(col.Name, col.Type);
            return table;
        }

        [Fact]
        public void toEntity_NoAttributes_MatchesExactIgnoreCaseAndUnderscore()
        {
            var db = CreateDb();
            var table = CreateTable(
                ("user_id", typeof(int)),
                ("UserName", typeof(string)),
                ("ISACTIVE", typeof(bool)));
            var row = table.NewRow();
            row["user_id"] = 42;
            row["UserName"] = "bob";
            row["ISACTIVE"] = true;
            table.Rows.Add(row);

            var entity = db.toEntity<PlainUserDto>(row);

            entity.UserId.Should().Be(42);
            entity.UserName.Should().Be("bob");
            entity.IsActive.Should().BeTrue();
        }

        [Fact]
        public void toEntity_WithSooColumn_MatchesAttributeName()
        {
            var db = CreateDb();
            var table = CreateTable(
                ("uid", typeof(int)),
                ("uname", typeof(string)),
                ("created_at", typeof(DateTime)));
            var row = table.NewRow();
            row["uid"] = 7;
            row["uname"] = "alice";
            row["created_at"] = new DateTime(2024, 5, 1);
            table.Rows.Add(row);

            var entity = db.toEntity<AttributedUserDto>(row);

            entity.Id.Should().Be(7);
            entity.Name.Should().Be("alice");
            entity.CreatedAt.Should().Be(new DateTime(2024, 5, 1));
        }

        [Fact]
        public void toList_MapsAllRows()
        {
            var db = CreateDb();
            var table = CreateTable(("user_id", typeof(int)), ("UserName", typeof(string)));
            var r1 = table.NewRow();
            r1["user_id"] = 1;
            r1["UserName"] = "a";
            table.Rows.Add(r1);
            var r2 = table.NewRow();
            r2["user_id"] = 2;
            r2["UserName"] = "b";
            table.Rows.Add(r2);

            var list = db.toList<PlainUserDto>(table);

            list.Should().HaveCount(2);
            list[0].UserId.Should().Be(1);
            list[0].UserName.Should().Be("a");
            list[1].UserId.Should().Be(2);
            list[1].UserName.Should().Be("b");
        }

        [Fact]
        public void toEntity_NullRow_ReturnsDefault()
        {
            var db = CreateDb();
            db.toEntity<PlainUserDto>(null).Should().BeNull();
        }

        [Fact]
        public void toList_NullOrEmpty_ReturnsEmptyList()
        {
            var db = CreateDb();
            db.toList<PlainUserDto>(null).Should().BeEmpty();
            db.toList<PlainUserDto>(new DataTable()).Should().BeEmpty();
        }

        /// <summary>无 SooColumn，走属性名多策略匹配。</summary>
        private class PlainUserDto
        {
            public int UserId { get; set; }
            public string UserName { get; set; }
            public bool IsActive { get; set; }
        }

        [SooTable("attr_user")]
        private class AttributedUserDto
        {
            [SooColumn("uid", IsPrimaryKey = true)]
            public int Id { get; set; }

            [SooColumn("uname")]
            public string Name { get; set; }

            [SooColumn("created_at")]
            public DateTime CreatedAt { get; set; }
        }
    }
}
