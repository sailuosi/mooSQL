using System;
using mooSQL.data;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>
    /// 强类型 setXxx 语法糖：转换成功写入、失败忽略、AsDBNull 时 setToNull。
    /// </summary>
    public sealed class SQLBuilderSetTypedSugarTests
    {
        private static SQLBuilder NewBuilder() => TestDatabaseHelper.CreateSQLBuilder();

        [Fact]
        public void SetInt_Converts_String_And_Writes()
        {
            using var b = NewBuilder();
            b.setTable("t").setInt("age", "123");

            Assert.True(b.containSetColumn("age"));
            Assert.Equal(123, Convert.ToInt32(b.getSetedValue("age")));
        }

        [Fact]
        public void SetInt_Invalid_IsIgnored_ByDefault()
        {
            using var b = NewBuilder();
            b.setTable("t").setInt("age", "abc");

            Assert.False(b.containSetColumn("age"));
            Assert.Null(b.getSetedValue("age"));
        }

        [Fact]
        public void SetInt_Invalid_AsDBNull_Calls_SetToNull()
        {
            using var b = NewBuilder();
            b.configSetNull(UpdateSetNullOption.AsDBNull)
                .setTable("t")
                .setInt("age", "abc");

            Assert.True(b.containSetColumn("age"));
            Assert.Equal("NULL", b.getSetedValue("age")?.ToString(), ignoreCase: true);
        }

        [Fact]
        public void SetInt_Null_AsDBNull_Calls_SetToNull()
        {
            using var b = NewBuilder();
            b.configSetNull(UpdateSetNullOption.AsDBNull)
                .setTable("t")
                .setInt("age", null);

            Assert.True(b.containSetColumn("age"));
            Assert.Equal("NULL", b.getSetedValue("age")?.ToString(), ignoreCase: true);
        }

        [Fact]
        public void SetInt_DBNull_Always_SetToNull_Ignoring_Config()
        {
            using var b = NewBuilder();
            b.setTable("t").setInt("age", DBNull.Value);

            Assert.True(b.containSetColumn("age"));
            Assert.Equal("NULL", b.getSetedValue("age")?.ToString(), ignoreCase: true);
        }

        [Fact]
        public void SetLong_Converts_And_Writes()
        {
            using var b = NewBuilder();
            b.setTable("t").setLong("big", "9876543210");

            Assert.True(b.containSetColumn("big"));
            Assert.Equal(9876543210L, Convert.ToInt64(b.getSetedValue("big")));
        }

        [Fact]
        public void SetString_Converts_NonNull_Object()
        {
            using var b = NewBuilder();
            b.setTable("t").setString("name", 42);

            Assert.True(b.containSetColumn("name"));
            Assert.Equal("42", b.getSetedValue("name")?.ToString());
        }

        [Fact]
        public void SetGuid_Converts_String()
        {
            var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
            using var b = NewBuilder();
            b.setTable("t").setGuid("gid", id.ToString());

            Assert.True(b.containSetColumn("gid"));
            Assert.Equal(id, (Guid)b.getSetedValue("gid"));
        }
    }
}
