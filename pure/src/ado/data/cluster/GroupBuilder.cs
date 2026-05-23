using System;
using System.Collections.Generic;

namespace mooSQL.data.cluster
{
  public class GroupBuilder
  {
    private readonly MasterSlaveGroup _group;
    private readonly DBInsCash _cash;

    internal GroupBuilder(DBInsCash cash, int masterPosition)
    {
      _cash = cash;
      _group = new MasterSlaveGroup
      {
        GroupId = masterPosition,
        Master = cash.getInstance(masterPosition),
        ActiveMaster = cash.getInstance(masterPosition)
      };
    }

    public GroupBuilder master(int position)
    {
      _group.GroupId = position;
      _group.Master = _cash.getInstance(position);
      _group.ActiveMaster = _group.Master;
      return this;
    }

    public GroupBuilder failover(FailoverMode mode)
    {
      _group.FailoverMode = mode;
      return this;
    }

    public GroupBuilder readPolicy(ReadRoutePolicy policy)
    {
      _group.ReadPolicy = policy;
      return this;
    }

    public GroupBuilder readFallbackToMaster(bool enabled = true)
    {
      _group.ReadFallbackToMaster = enabled;
      return this;
    }

    public GroupBuilder addSlave(int position, Action<SlaveMember> configure = null)
    {
      var mem = new SlaveMember { Position = position, Instance = _cash.getInstance(position) };
      configure?.Invoke(mem);
      _group.Slaves.Add(mem);
      return this;
    }

    public GroupBuilder enableDualWrite(params int[] slavePositions)
    {
      foreach (var p in slavePositions)
      {
        addSlave(p, s => { s.DualWrite = true; s.WriteEnabled = true; });
      }
      return this;
    }

    internal MasterSlaveGroup Build() => _group;
  }
}
