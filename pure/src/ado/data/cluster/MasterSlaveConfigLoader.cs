using System;
using System.Collections.Generic;
using System.Xml;

namespace mooSQL.data.cluster
{
    /// <summary>
    /// 从 XML 加载主从组配置（概念格式见设计文档）。
    /// </summary>
    public static class MasterSlaveConfigLoader
    {
        public static void ApplyFromXml(DBInsCash cash, string configPath, MasterSlaveOptions options = null)
        {
            if (cash == null || string.IsNullOrWhiteSpace(configPath)) return;
            if (!System.IO.File.Exists(configPath)) return;

            var doc = new XmlDocument();
            doc.Load(configPath);
            var databases = doc.SelectNodes("//database");
            if (databases == null) return;

            foreach (XmlNode dbNode in databases)
            {
                var masterNode = dbNode.SelectSingleNode("master");
                if (masterNode == null) continue;

                var indexAttr = dbNode.Attributes?["index"]?.Value;
                if (!int.TryParse(indexAttr, out var masterPos)) continue;

                cash.configureGroup(masterPos, g =>
                {
                    var failover = masterNode.Attributes?["failover"]?.Value;
                    if (Enum.TryParse(failover, true, out FailoverMode fm))
                        g.failover(fm);

                    var slaveNodes = masterNode.SelectNodes("slave");
                    if (slaveNodes == null) return;

                    foreach (XmlNode slaveNode in slaveNodes)
                    {
                        var sIdx = slaveNode.Attributes?["index"]?.Value;
                        if (!int.TryParse(sIdx, out var sp)) continue;
                        g.addSlave(sp, s =>
                        {
                            s.ReadReplica = ParseBool(slaveNode.Attributes?["readReplica"]?.Value);
                            s.HotStandby = ParseBool(slaveNode.Attributes?["hotStandby"]?.Value);
                            s.DualWrite = ParseBool(slaveNode.Attributes?["dualWrite"]?.Value);
                            s.AsyncReplica = ParseBool(slaveNode.Attributes?["asyncReplica"]?.Value);
                            s.WriteEnabled = ParseBool(slaveNode.Attributes?["writeEnabled"]?.Value);
                            if (int.TryParse(slaveNode.Attributes?["weight"]?.Value, out var w))
                                s.Weight = w;
                            if (!s.ReadReplica && !s.HotStandby && !s.DualWrite && !s.AsyncReplica)
                                s.AsyncReplica = true;
                        });
                    }
                }, options);
            }
        }

        private static bool ParseBool(string v) =>
            v != null && (v.Equals("true", StringComparison.OrdinalIgnoreCase) || v == "1");
    }
}
