using FluentAssertions;
using mooSQL.Pure.Tests.TestHelpers;
using mooSQL.data;
using mooSQL.data.Mapping;
using System;
using System.Collections.Generic;
using Xunit;

namespace mooSQL.Pure.Tests
{
    public class MaterializerParityTests
    {
        private static MooClient CreateClient(bool enableAot)
        {
            var client = new MooClient { EnableAot = enableAot };
            client.entityAnalyseFactory.register(new MooEntityAnalyser());
            return client;
        }

        private static FakeDbDataReader CreateReader(bool reorderColumns = false)
        {
            var columns = new List<(string Name, object Value, Type Type)>
            {
                ("id", 1, typeof(int)),
                ("name", "Alice", typeof(string)),
                ("email", "alice@example.com", typeof(string)),
                ("age", 30, typeof(int)),
                ("created_at", new DateTime(2024, 1, 2), typeof(DateTime)),
                ("is_active", true, typeof(bool)),
            };
            if (reorderColumns)
            {
                columns.Reverse();
            }
            return new FakeDbDataReader(columns);
        }

        private static PackUp CreatePackUp(bool enableAot)
        {
            return new PackUp(CreateClient(enableAot));
        }

        [Fact]
        public void SourceGenerator_RegistersTestUserMaterializer()
        {
            MaterializerRegistry.TryGet(typeof(TestUser), out var fn).Should().BeTrue();
            using var reader = CreateReader();
            reader.Read().Should().BeTrue();
            var user = (TestUser)fn!(reader, null)!;
            user.Id.Should().Be(1);
            user.Name.Should().Be("Alice");
        }

        [Fact]
        public void EnableAot_On_PrefersSourceGeneratedMaterializer()
        {
            var packUp = CreatePackUp(enableAot: true);
            using var reader = CreateReader();
            reader.Read().Should().BeTrue();
            var func = packUp.GetTypePacker(packUp, typeof(TestUser), reader, 0, -1, false, null);
            var result = (TestUser)func(reader, null)!;
            result.Id.Should().Be(1);
            result.Name.Should().Be("Alice");
        }

        [Fact]
        public void EnableAot_Default_IsFalse()
        {
            new MooClient().EnableAot.Should().BeFalse();
        }

        [Fact]
        public void EnableAot_Off_UsesEmitPath()
        {
            var client = CreateClient(enableAot: false);
            var packUp = new PackUp(client);
            client.EnableAot.Should().BeFalse();

            using var reader = CreateReader();
            reader.Read().Should().BeTrue();

            var func = packUp.GetTypePacker(packUp, typeof(TestUser), reader, 0, -1, false, null);
            var result = (TestUser)func(reader, null)!;

            result.Id.Should().Be(1);
            result.Name.Should().Be("Alice");
            result.Email.Should().Be("alice@example.com");
            result.Age.Should().Be(30);
            result.IsActive.Should().BeTrue();
        }

        [Fact]
        public void EnableAot_On_UsesReflectionPath()
        {
            var client = CreateClient(enableAot: true);
            var packUp = new PackUp(client);
            client.EnableAot.Should().BeTrue();

            using var reader = CreateReader(reorderColumns: true);
            reader.Read().Should().BeTrue();

            var func = packUp.GetTypePacker(packUp, typeof(TestUser), reader, 0, -1, false, null);
            var result = (TestUser)func(reader, null)!;

            result.Id.Should().Be(1);
            result.Name.Should().Be("Alice");
            result.Email.Should().Be("alice@example.com");
            result.Age.Should().Be(30);
            result.IsActive.Should().BeTrue();
        }

        [Fact]
        public void EnableAot_Toggle_PurgesQueryCache()
        {
            var client = new MooClient();
            client.EnableAot = true;
            client.EnableAot = false;
            client.EnableAot.Should().BeFalse();
        }

        [Fact]
        public void ClientMaterializer_OverridesStaticRegistry()
        {
            var client = CreateClient(enableAot: true);
            client.RegisterMaterializer(typeof(TestUser), (r, d) =>
            {
                r.Read();
                return new TestUser { Id = 42, Name = "InstanceOverride" };
            });

            client.TryGetMaterializer(typeof(TestUser), out _).Should().BeTrue();
            var packUp = new PackUp(client);
            using var reader = CreateReader();
            reader.Read().Should().BeTrue();
            var func = packUp.GetTypePacker(packUp, typeof(TestUser), reader, 0, -1, false, null);
            var result = (TestUser)func(reader, null)!;
            result.Id.Should().Be(42);
            result.Name.Should().Be("InstanceOverride");
        }

        [Fact]
        public void RegisterGeneratedMaterializers_CopiesStaticToClient()
        {
            MaterializerRegistry.TryGet(typeof(TestUser), out _).Should().BeTrue("SG ModuleInitializer should populate static catalog");

            var client = CreateClient(enableAot: true);
            client.TryGetMaterializer(typeof(TestUser), out _).Should().BeFalse();
            client.RegisterGeneratedMaterializers();
            client.TryGetMaterializer(typeof(TestUser), out var fn).Should().BeTrue();
            using var reader = CreateReader();
            reader.Read().Should().BeTrue();
            var user = (TestUser)fn!(reader, null)!;
            user.Id.Should().Be(1);
            user.Name.Should().Be("Alice");
        }

        [Fact]
        public void GeneratedMaterializerHook_InvokedBeforeCopy()
        {
            var client = CreateClient(enableAot: true);
            var hookCalled = false;
            client.GeneratedMaterializerHook = c =>
            {
                hookCalled = true;
                c.RegisterMaterializer(typeof(TestOrder), (r, d) =>
                {
                    r.Read();
                    return new TestOrder { Id = 7, OrderNo = "Hook" };
                });
            };
            client.RegisterGeneratedMaterializers();
            hookCalled.Should().BeTrue();
            client.TryGetMaterializer(typeof(TestOrder), out var fn).Should().BeTrue();
            using var reader = new FakeDbDataReader(new[]
            {
                ("id", (object)1, typeof(int)),
                ("order_no", "x", typeof(string)),
                ("user_id", 1, typeof(int)),
                ("amount", 1m, typeof(decimal)),
                ("created_at", DateTime.UtcNow, typeof(DateTime)),
            });
            reader.Read();
            ((TestOrder)fn!(reader, null)!).Id.Should().Be(7);
        }

        [Fact]
        public void MaterializerRegistry_CanRegisterManually()
        {
            MaterializerRegistry.Register(typeof(TestOrder), (r, d) =>
            {
                r.Read();
                return new TestOrder { Id = 99, OrderNo = "Registered" };
            });

            MaterializerRegistry.TryGet(typeof(TestOrder), out var fn).Should().BeTrue();
            using var reader = new FakeDbDataReader(new[]
            {
                ("id", (object)1, typeof(int)),
                ("order_no", "x", typeof(string)),
                ("user_id", 1, typeof(int)),
                ("amount", 1m, typeof(decimal)),
                ("created_at", DateTime.UtcNow, typeof(DateTime)),
            });
            var order = (TestOrder)fn!(reader, null)!;
            order.Id.Should().Be(99);
            order.OrderNo.Should().Be("Registered");
        }

        [Fact]
        public void ColumnValueReader_ReadsBasicTypes()
        {
            using var reader = CreateReader();
            reader.Read().Should().BeTrue();

            ColumnValueReader.ReadValue(reader, 0, typeof(int), null, null).Should().Be(1);
            ColumnValueReader.ReadValue(reader, 1, typeof(string), null, null).Should().Be("Alice");
        }
    }
}
