using System;
using System.Collections.Generic;

namespace mooSQL.data.cluster
{
  public class GroupBuilder
  {
    private readonly MasterSlaveGroup _group;
    private readonly DBInsCash _cash;
    private readonly MasterSlaveOptions _defaults;
    private bool _failoverSet;
    private bool _readPolicySet;
    private bool _readFallbackSet;
    private bool _autoReadReplicaSet;

    internal GroupBuilder(DBInsCash cash, int masterPosition, MasterSlaveOptions defaults = null)
    {
      _cash = cash;
      _defaults = defaults;
      _group = new MasterSlaveGroup
      {
        GroupId = masterPosition,
        Master = cash.getInstance(masterPosition)
      };
      if (defaults != null)
      {
        _group.FailoverMode = defaults.DefaultFailover;
        _group.ReadPolicy = defaults.DefaultReadPolicy;
        _group.ReadFallbackToMaster = defaults.ReadFallbackToMaster;
      }
    }

    public GroupBuilder master(int position)
    {
      _group.GroupId = position;
      _group.Master = _cash.getInstance(position);
      return this;
    }

    public GroupBuilder failover(FailoverMode mode)
    {
      _group.FailoverMode = mode;
      _failoverSet = true;
      return this;
    }

    public GroupBuilder readPolicy(ReadRoutePolicy policy)
    {
      _group.ReadPolicy = policy;
      _readPolicySet = true;
      return this;
    }

    public GroupBuilder readFallbackToMaster(bool enabled = true)
    {
      _group.ReadFallbackToMaster = enabled;
      _readFallbackSet = true;
      return this;
    }

    public GroupBuilder autoReadReplica(bool enabled = true)
    {
      _group.AutoReadReplica = enabled;
      _autoReadReplicaSet = true;
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

    internal MasterSlaveGroup Build()
    {
      if (_defaults != null)
      {
        if (!_failoverSet) _group.FailoverMode = _defaults.DefaultFailover;
        if (!_readPolicySet) _group.ReadPolicy = _defaults.DefaultReadPolicy;
        if (!_readFallbackSet) _group.ReadFallbackToMaster = _defaults.ReadFallbackToMaster;
      }
      return _group;
    }
  }
}
