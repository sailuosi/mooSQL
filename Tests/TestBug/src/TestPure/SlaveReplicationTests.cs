using FluentAssertions;
using mooSQL.data;
using mooSQL.data.slave;
using mooSQL.Pure.Tests.TestHelpers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace mooSQL.Pure.Tests
{
  public class SlaveReplicationTests
  {
    private sealed class RecordingEat : IEventEat
    {
      public ConcurrentBag<ModifyPara> Received { get; } = new ConcurrentBag<ModifyPara>();

      public string Eat(ModifyPara e)
      {
        Received.Add(e);
        return "ok";
      }

      public string Report() => string.Empty;
    }

    [Fact]
    public void SlaveFactory_createTeam_registers_workers()
    {
      var master = TestDatabaseHelper.CreateTestDBInstance();
      var slave = TestDatabaseHelper.CreateTestDBInstance();

      var team = SlaveFactory.createTeam(0, new List<DBInstance> { slave });

      team.members.Should().HaveCount(1);
      team.Empty.Should().BeFalse();
    }

    [Fact]
    public void SlaveTeam_signal_filter_blocks_mismatched_commands()
    {
      var team = new SlaveTeam("t1", "order-sync");
      var slave = TestDatabaseHelper.CreateTestDBInstance();
      team.sign(0, new List<DBInstance> { slave });

      var matched = team.ListenTo(new ModifyPara
      {
        type = ModifyEventType.Modify,
        position = 0,
        cmd = new SQLCmd("UPDATE t SET x=1", null) { signal = "order-sync" }
      });

      var blocked = team.ListenTo(new ModifyPara
      {
        type = ModifyEventType.Modify,
        position = 0,
        cmd = new SQLCmd("UPDATE t SET x=1", null) { signal = "other" }
      });

      matched.Should().Be("invoked");
      blocked.Should().BeEmpty();
    }

    [Fact]
    public void TeamHeader_dispatches_modify_to_members()
    {
      var header = new TeamHeader { position = 0 };
      var recorder = new RecordingEat();
      header.members.Add(recorder);

      header.Eat(new ModifyPara
      {
        type = ModifyEventType.Modify,
        position = 0,
        cmd = new SQLCmd("INSERT INTO t VALUES(1)", null)
      });

      WaitUntil(() => recorder.Received.Count > 0, 3000);

      recorder.Received.Should().HaveCount(1);
      recorder.Received.Should().ContainSingle(p => p.cmd.sql.Contains("INSERT"));
    }

    private static void WaitUntil(System.Func<bool> condition, int timeoutMs)
    {
      var deadline = System.Environment.TickCount + timeoutMs;
      while (!condition() && System.Environment.TickCount < deadline)
      {
        Thread.Sleep(20);
      }
    }
  }
}
