using FluentAssertions;
using mooSQL.data.Mapping;
using Xunit;

namespace mooSQL.Pure.Tests
{
    public class EntityNameConvertTests
    {
        [Theory]
        [InlineData("CreatedAt", "created_at")]
        [InlineData("createdAt", "created_at")]
        [InlineData("UserID", "user_id")]
        [InlineData("created_at", "created_at")]
        [InlineData("", "")]
        // null 由空串覆盖；避免 xUnit1012
        public void ToSnakeCase(string input, string expected)
        {
            EntityNameConvert.ToSnakeCase(input).Should().Be(expected);
        }

        [Theory]
        [InlineData("created_at", "CreatedAt")]
        [InlineData("createdAt", "CreatedAt")]
        [InlineData("CreatedAt", "CreatedAt")]
        [InlineData("", "")]
        public void ToPascalCase(string input, string expected)
        {
            EntityNameConvert.ToPascalCase(input).Should().Be(expected);
        }

        [Theory]
        [InlineData("CreatedAt", "createdAt")]
        [InlineData("created_at", "createdAt")]
        [InlineData("createdAt", "createdAt")]
        public void ToCamelCase(string input, string expected)
        {
            EntityNameConvert.ToCamelCase(input).Should().Be(expected);
        }
    }
}
