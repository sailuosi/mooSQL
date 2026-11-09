using FluentAssertions;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.data;
using mooSQL.data.Mapping;
using Xunit;

namespace mooSQL.Pure.Tests
{
    public class EntityAccessorTests
    {
        private static MooClient CreateClient()
        {
            var client = new MooClient();
            client.entityAnalyseFactory.register(new MooEntityAnalyser());
            return client;
        }

        [Fact]
        public void Unregistered_UsesReflection_NotStaticLookup()
        {
            var client = CreateClient();
            client.TryGetAccessor(typeof(TestUser), out _).Should().BeFalse();

            var user = new TestUser { Id = 7, Name = "Bob", Email = null };
            var accessor = EntityAccessorResolver.Resolve(client, typeof(TestUser));
            accessor.ReadValue(user, "Id").Should().Be(7);
            accessor.ReadValue(user, "Name").Should().Be("Bob");
            accessor.IsNull(user, "Email").Should().BeTrue();
        }

        [Fact]
        public void RegisterGeneratedAccessors_InstallsOntoClient()
        {
            var client = CreateClient();
            client.RegisterGeneratedAccessors();
            client.TryGetAccessor(typeof(TestUser), out var acc).Should().BeTrue();
            acc.Should().NotBeNull();

            var user = new TestUser { Id = 3, Name = "Ann", Email = null };
            acc!.ReadValue(user, "Id").Should().Be(3);
            acc.ReadValue(user, "Name").Should().Be("Ann");
            acc.IsNull(user, "Email").Should().BeTrue();

            acc.WriteValue(user, "Email", "a@b.c");
            user.Email.Should().Be("a@b.c");
            acc.IsNull(user, "Email").Should().BeFalse();
        }

        [Fact]
        public void RegisterGeneratedMaterializers_AlsoRegistersAccessors()
        {
            var client = CreateClient();
            client.RegisterGeneratedMaterializers();
            client.TryGetAccessor(typeof(SQLiteTestProduct), out var acc).Should().BeTrue();

            var p = new SQLiteTestProduct { Id = 1, Name = "X", Category = "C", Price = 2m, Stock = 3 };
            acc!.ReadValue(p, "Name").Should().Be("X");
            acc.WriteValue(p, "Stock", 9);
            p.Stock.Should().Be(9);
        }

        [Fact]
        public void ReflectionAndGenerated_AgreeOnReadWriteIsNull()
        {
            var client = CreateClient();
            client.RegisterGeneratedAccessors();
            client.TryGetAccessor(typeof(TestUser), out var generated).Should().BeTrue();

            var reflection = ReflectionEntityAccessor.For(typeof(TestUser));
            var user = new TestUser { Id = 11, Name = "Same", Email = null, Age = null, IsActive = true };

            foreach (var prop in new[] { "Id", "Name", "Email", "Age", "IsActive", "CreatedAt" })
            {
                var g = generated!.ReadValue(user, prop);
                var r = reflection.ReadValue(user, prop);
                g.Should().Be(r, because: prop);
                generated.IsNull(user, prop).Should().Be(reflection.IsNull(user, prop), because: prop);
            }

            generated!.WriteValue(user, "Name", "Next");
            reflection.ReadValue(user, "Name").Should().Be("Next");
        }

        [Fact]
        public void EntityColumn_Facade_UsesClientAccessor()
        {
            var client = CreateClient();
            client.RegisterGeneratedAccessors();
            var en = client.EntityCash.getEntityInfo(typeof(TestUser));
            var nameCol = en.Columns.Find(c => c.PropertyName == "Name");
            nameCol.Should().NotBeNull();

            var user = new TestUser { Name = "Facade" };
            nameCol!.GetEntityValue(user, client).Should().Be("Facade");
            nameCol.SetEntityValue(user, "Wired", client);
            user.Name.Should().Be("Wired");
            nameCol.IsEntityValueNull(user, client).Should().BeFalse();
        }
    }
}
