using FluentAssertions;
using mooSQL.data;
using mooSQL.data.Mapping;
using mooSQL.data.model;
using mooSQL.Pure.Tests.TestHelpers;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace mooSQL.Pure.Tests
{
  /// <summary>
  /// EntityTranslator 删除切面（Before/Ready）在各删除路径上的覆盖测试。
  /// </summary>
  public class EntityTranslatorDeleteHookTests
  {
    private sealed class HookCounter
    {
      public int Before { get; set; }
      public int Ready { get; set; }
    }

    private sealed class SharedTranslatorClientFactory : DBClientFactory
    {
      private readonly EntityTranslator _translator;

      public SharedTranslatorClientFactory(EntityTranslator translator)
      {
        _translator = translator;
      }

      public override EntityTranslator getEntityTranslator() => _translator;
    }

    private static (EntityTranslator translator, HookCounter counter) CreateHookedTranslator()
    {
      var translator = new EntityTranslator();
      var counter = new HookCounter();
      translator.OnBeforeDeleteEntity((_, __, ___, ____) => counter.Before++);
      translator.OnReadyDeleteEntity((_, __, ___, ____) => counter.Ready++);
      return (translator, counter);
    }

    private sealed class TestUserEntityAnalyser : MooEntityAnalyser
    {
      public override EntityColumn ParseColumn(Type entity, PropertyInfo propertyInfo, mooSQL.data.EntityInfo entityInfo, EntityColumn entityColumn)
      {
        var column = base.ParseColumn(entity, propertyInfo, entityInfo, entityColumn);
        if (column == null)
        {
          return column;
        }
        foreach (SooColumnAttribute ca in propertyInfo.GetCustomAttributes(typeof(SooColumnAttribute)))
        {
          if (ca.HasIsPrimaryKey())
          {
            column.IsPrimarykey = ca.IsPrimaryKey;
          }
          if (!string.IsNullOrWhiteSpace(column.DbColumnName) && column.Kind == FieldKind.None)
          {
            column.Kind = FieldKind.Base;
          }
        }
        return column;
      }
    }

    private static void EnsureEntityParser(MooClient client)
    {
      client.entityAnalyseFactory.register(new TestUserEntityAnalyser());
    }

    private static SQLBuilder CreateBuilder(EntityTranslator translator)
    {
      var db = TestDatabaseHelper.CreateTestDBInstance();
      EnsureEntityParser(db.client);
      db.client.useClientFactory(new SharedTranslatorClientFactory(translator));
      return db.useSQL();
    }

    private static SQLBuilder CreateKit()
    {
      var db = TestDatabaseHelper.CreateTestDBInstance();
      EnsureEntityParser(db.client);
      return db.useSQL();
    }

    [Fact]
    public void PrepareDelete_GenericOverload_ShouldFireHooks()
    {
      var (translator, counter) = CreateHookedTranslator();
      var kit = CreateKit();
      var user = new TestUser { Id = 1 };

      translator.prepareDelete(kit, user);

      counter.Before.Should().Be(1);
      counter.Ready.Should().Be(1);
    }

    [Fact]
    public void PrepareDelete_TypeOverload_ShouldFireHooks()
    {
      var (translator, counter) = CreateHookedTranslator();
      var kit = CreateKit();
      var user = new TestUser { Id = 1 };

      translator.prepareDelete(kit, user, typeof(TestUser));

      counter.Before.Should().Be(1);
      counter.Ready.Should().Be(1);
    }

    [Fact]
    public void ToDelete_SingleEntity_ShouldFireHooks()
    {
      var (translator, counter) = CreateHookedTranslator();
      var kit = CreateBuilder(translator);
      var user = new TestUser { Id = 1 };

      var cmd = kit.toDelete(user);

      cmd.Should().NotBeNull();
      counter.Before.Should().Be(1);
      counter.Ready.Should().Be(1);
    }

    [Fact]
    public void ToDelete_BatchSinglePrimaryKey_ShouldFireHooksOnce()
    {
      var (translator, counter) = CreateHookedTranslator();
      var kit = CreateBuilder(translator);
      IEnumerable<TestUser> users = new List<TestUser>
      {
        new TestUser { Id = 1 },
        new TestUser { Id = 2 }
      };

      List<SQLCmd> cmds = kit.toDelete(users);

      cmds.Should().HaveCount(1);
      counter.Before.Should().Be(1);
      counter.Ready.Should().Be(1);
    }

    [Fact]
    public void RemoveById_ShouldFireHooks()
    {
      var (translator, counter) = CreateHookedTranslator();
      var kit = CreateBuilder(translator);
      var en = kit.DBLive.client.EntityCash.getEntityInfo(typeof(TestUser));
      var builder = kit.useSQL();

      kit.Client.Translator.prepareDeleteById(builder, en, 99);
      builder.toDelete();

      counter.Before.Should().Be(1);
      counter.Ready.Should().Be(1);
    }

    [Fact]
    public void RemoveByIds_ShouldFireHooksOnce()
    {
      var (translator, counter) = CreateHookedTranslator();
      var kit = CreateBuilder(translator);
      var ids = new ArrayList { 1, 2, 3 };

      var affected = kit.removeByIds<TestUser>(ids);

      affected.Should().BeGreaterOrEqualTo(-1);
      counter.Before.Should().Be(1);
      counter.Ready.Should().Be(1);
    }

    [Fact]
    public void PrepareDeleteById_ShouldFireHooks()
    {
      var (translator, counter) = CreateHookedTranslator();
      var kit = CreateKit();
      var en = kit.DBLive.client.EntityCash.getEntityInfo(typeof(TestUser));

      translator.prepareDeleteById(kit, en, 42);

      counter.Before.Should().Be(1);
      counter.Ready.Should().Be(1);
    }

    [Fact]
    public void Repository_DeleteById_ShouldFireHooks()
    {
      var (translator, counter) = CreateHookedTranslator();
      var db = TestDatabaseHelper.CreateTestDBInstance();
      EnsureEntityParser(db.client);
      _ = new SooRepository<TestUser>(db, translator);
      var kit = db.useSQL();
      var en = kit.DBLive.client.EntityCash.getEntityInfo(typeof(TestUser));

      translator.prepareDeleteById(kit, en, int.MaxValue - 1);
      kit.toDelete();

      counter.Before.Should().Be(1);
      counter.Ready.Should().Be(1);
    }

    [Fact]
    public void Repository_DeleteByIds_ShouldFireHooksOnce()
    {
      var (translator, counter) = CreateHookedTranslator();
      var db = TestDatabaseHelper.CreateTestDBInstance();
      EnsureEntityParser(db.client);
      _ = new SooRepository<TestUser>(db, translator);
      var kit = db.useSQL();
      var en = kit.DBLive.client.EntityCash.getEntityInfo(typeof(TestUser));

      translator.prepareDelete(kit, en, new[] { int.MaxValue - 2, int.MaxValue - 3 });
      kit.toDelete();

      counter.Before.Should().Be(1);
      counter.Ready.Should().Be(1);
    }
  }
}
