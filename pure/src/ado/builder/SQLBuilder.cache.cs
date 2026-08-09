using System;
using System.Collections.Generic;
using mooSQL.data.model;

namespace mooSQL.data
{
    /// <summary>
    /// 执行模板缓存：与 StepBuilder 结果缓存共用 <see cref="StepBuilder.cacheHolder"/>
    ///（setCacheHolder → Client.Cache → 默认 HashCache）。
    /// </summary>
    public partial class SQLBuilder
    {
        // TEMP: 业务侧大测临时默认开启；测完请改回 false。
        private bool _scriptTemplateCacheEnabled = true;

        /// <summary>本次门面实例的模板缓存命中次数（单测/诊断）。</summary>
        public int ScriptTemplateCacheHits { get; private set; }

        /// <summary>本次门面实例的模板缓存未命中次数（单测/诊断）。</summary>
        public int ScriptTemplateCacheMisses { get; private set; }

        /// <summary>
        /// 启用/关闭执行模板缓存。
        /// TEMP: 当前默认开启便于业务测试；正式默认应为关闭。
        /// </summary>
        public SQLBuilder useScriptTemplateCache(bool enabled = true)
        {
            _scriptTemplateCacheEnabled = enabled;
            return this;
        }

        /// <summary>toSelect：可选冷热分流。</summary>
        public SQLCmd toSelect()
        {
            return ToCached(
                ScriptCacheKey.BuildKindToSelect,
                QueryType.Select,
                () => _inner.toSelect());
        }

        /// <summary>toInsert：可选冷热分流。</summary>
        public SQLCmd toInsert()
        {
            return ToCached(
                ScriptCacheKey.BuildKindToInsert,
                QueryType.Insert,
                () => _inner.toInsert());
        }

        /// <summary>toUpdate：可选冷热分流。</summary>
        public SQLCmd toUpdate()
        {
            return ToCached(
                ScriptCacheKey.BuildKindToUpdate,
                QueryType.Update,
                () => _inner.toUpdate());
        }

        /// <summary>toDelete：可选冷热分流。</summary>
        public SQLCmd toDelete()
        {
            return ToCached(
                ScriptCacheKey.BuildKindToDelete,
                QueryType.Delete,
                () => _inner.toDelete());
        }

        private SQLCmd ToCached(string buildKind, QueryType queryType, System.Func<SQLCmd> coldBuild)
        {
            if (!_scriptTemplateCacheEnabled)
            {
                runBuild();
                return coldBuild();
            }

            var key = BuildScriptCacheKey(buildKind);
            var holder = _inner.cacheHolder;
            var cached = holder.Get<ScriptTemplate>(key);
            if (cached != null && TryBindHot(cached, queryType, out var hot))
            {
                ScriptTemplateCacheHits++;
                _dirty = false;
                LogScriptTemplateHit(key, cached, hot);
                return hot;
            }

            ScriptTemplateCacheMisses++;
            runBuild();
            var cmd = coldBuild();
            TryStoreScriptTemplate(key, holder, cmd);
            return cmd;
        }

        private string BuildScriptCacheKey(string buildKind)
        {
            var dbType = DataBaseType.MSSQL;
            if (_inner.DBLive != null && _inner.DBLive.config != null)
                dbType = _inner.DBLive.config.dbType;
            return ScriptCacheKey.Format(OrchestrationHash, dbType, buildKind, _inner.paraSeed);
        }

        /// <summary>TEMP: 控制台打印命中详情（壳 SQL + StaticSlot 桥接键值 + Live 占位）。</summary>
        private void LogScriptTemplateHit(string key, ScriptTemplate template, SQLCmd hot)
        {
            var slotN = template.StaticSlots != null ? template.StaticSlots.Length : 0;
            Console.WriteLine(
                "[moo.st HIT] key={0} hits={1} slots={2} live={3} type={4} table={5}",
                key,
                ScriptTemplateCacheHits,
                slotN,
                template.LiveCount,
                hot != null ? hot.type.ToString() : "",
                hot != null ? (hot.TargetTable ?? "") : "");
            Console.WriteLine("[moo.st HIT] sql={0}", hot != null ? (hot.sql ?? "") : "");

            if (template.StaticSlots != null && hot != null && hot.para != null)
            {
                for (int i = 0; i < template.StaticSlots.Length; i++)
                {
                    var slot = template.StaticSlots[i];
                    var name = slot.NameInTemplate ?? "";
                    var p = hot.para.GetParameter(name);
                    var val = p != null ? p.val : null;
                    Console.WriteLine(
                        "[moo.st HIT] slot[{0}] id={1} key={2} val={3}",
                        i,
                        slot.SlotId,
                        name,
                        FormatLogValue(val));
                }
            }

            if (template.LiveCount > 0 && hot != null && hot.para != null && hot.para.DelayParas != null)
            {
                for (int i = 0; i < hot.para.DelayParas.Count; i++)
                {
                    var lp = hot.para.DelayParas[i];
                    Console.WriteLine(
                        "[moo.st HIT] live[{0}] ph={1} type={2}",
                        i,
                        lp != null ? lp.PlaceHolder : "",
                        lp != null ? lp.GetType().Name : "");
                }
            }
        }

        private static string FormatLogValue(object val)
        {
            if (val == null) return "<null>";
            if (val == DBNull.Value) return "<DBNull>";
            var s = val.ToString() ?? "";
            if (s.Length > 200)
                return s.Substring(0, 200) + "...(len=" + s.Length + ")";
            return s;
        }

        private bool TryBindHot(ScriptTemplate template, QueryType queryType, out SQLCmd cmd)
        {
            cmd = null;
            if (template == null || string.IsNullOrEmpty(template.ShellSql))
                return false;
            if (template.StaticSlots == null)
                return false;

            var values = HarvestStaticValues(template.StaticSlots);
            if (values == null)
                return false;

            var lives = CollectLiveParas();
            if (lives == null || lives.Count != template.LiveCount)
                return false;

            var ps = new Paras();
            var prefix = _inner.expression != null ? _inner.expression.paraPrefix : "@";
            for (int i = 0; i < template.StaticSlots.Length; i++)
            {
                var slot = template.StaticSlots[i];
                ps.AddByPrefix(slot.NameInTemplate, values[i], prefix);
            }
            for (int i = 0; i < lives.Count; i++)
                ps.AddDelayPara(lives[i]);

            cmd = new SQLCmd(template.ShellSql, ps);
            cmd.type = queryType;
            cmd.TargetTable = HarvestTargetTable();
            cmd.signal = _inner.Signal;
            return true;
        }

        private string HarvestTargetTable()
        {
            string table = null;
            var steps = _steps;
            for (int i = 0; i < steps.Count; i++)
            {
                var st = steps[i] as SetTablestringStep;
                if (st != null && !string.IsNullOrEmpty(st.TableName))
                    table = st.TableName;
            }
            if (!string.IsNullOrEmpty(table))
                return table;
            if (_inner.current != null)
                return _inner.current.tableName ?? "";
            return "";
        }

        /// <summary>按模板槽序从 <see cref="IStaticSlotStep"/> 收值；对不齐则返回 null（回退冷路径）。</summary>
        private object[] HarvestStaticValues(StaticSlot[] slots)
        {
            if (slots.Length == 0)
                return new object[0];

            var byId = new Dictionary<int, object>();
            var steps = _steps;
            for (int i = 0; i < steps.Count; i++)
            {
                var slotStep = steps[i] as IStaticSlotStep;
                if (slotStep == null || slotStep.StaticSlotId == null) continue;
                byId[slotStep.StaticSlotId.Value] = slotStep.StaticSlotValue;
            }

            var values = new object[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                if (!byId.TryGetValue(slots[i].SlotId, out var val))
                    return null;
                values[i] = val;
            }
            return values;
        }

        /// <summary>
        /// 按磁带序 CollectBind Live；复现 ifs 门控（Where/Set 类步消费 opened）。
        /// </summary>
        private List<IDelayPara> CollectLiveParas()
        {
            var list = new List<IDelayPara>();
            var opened = true;
            var steps = _steps;
            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                if (step == null) continue;

                var ifs = step as IfsboolStep;
                if (ifs != null)
                {
                    opened = ifs.IsPass;
                    continue;
                }

                if (!opened)
                {
                    if (step.Kind == StepKind.Where || step.Kind == StepKind.Set)
                        opened = true;
                    continue;
                }

                var liveStep = step as ILiveBindStep;
                if (liveStep == null) continue;
                var para = liveStep.CollectLive(this);
                if (para != null)
                    list.Add(para);
            }
            return list;
        }

        private void TryStoreScriptTemplate(string key, ISooCache holder, SQLCmd cmd)
        {
            if (holder == null || cmd == null || string.IsNullOrEmpty(key)) return;
            if (!TryBuildScriptTemplate(cmd, out var template)) return;
            if (holder.ContainsKey(key)) return;
            holder.Add(key, template);
        }

        /// <summary>
        /// 冷路径收录：静态参须全为 ms_s* 槽；可含 Live（壳未 Resolve）。
        /// </summary>
        private bool TryBuildScriptTemplate(SQLCmd cmd, out ScriptTemplate template)
        {
            template = null;
            if (cmd.para == null) return false;

            var liveCount = cmd.para.DelayParas != null ? cmd.para.DelayParas.Count : 0;

            var slots = new List<StaticSlot>();
            var steps = _steps;
            for (int i = 0; i < steps.Count; i++)
            {
                var slotStep = steps[i] as IStaticSlotStep;
                if (slotStep == null || slotStep.StaticSlotId == null) continue;
                slots.Add(new StaticSlot
                {
                    SlotId = slotStep.StaticSlotId.Value,
                    NameInTemplate = StaticSlotMarks.FormatName(slotStep.StaticSlotId.Value)
                });
            }

            if (slots.Count == 0 && liveCount == 0) return false;
            if (cmd.para.Count != slots.Count) return false;

            for (int i = 0; i < slots.Count; i++)
            {
                if (cmd.para.GetParameter(slots[i].NameInTemplate) == null)
                    return false;
            }

            var shell = cmd.sql ?? "";
            for (int i = 0; i < liveCount; i++)
            {
                if (shell.IndexOf(LiveParaMarks.Format(i)) < 0)
                    return false;
            }

            if (liveCount > 0)
            {
                var collected = CollectLiveParas();
                if (collected == null || collected.Count != liveCount)
                    return false;
            }

            template = new ScriptTemplate
            {
                ShellSql = shell,
                StaticSlots = slots.ToArray(),
                LiveCount = liveCount,
                ParaSeed = _inner.paraSeed,
                OrchestrationHash = OrchestrationHash
            };
            return true;
        }

        /// <summary>删除前守卫：磁带上是否存在 Where 步（近似内核 wherePart.Count）。</summary>
        internal bool HasWhereStepForDelete()
        {
            var steps = _steps;
            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i] != null && steps[i].Kind == StepKind.Where)
                    return true;
            }
            return false;
        }
    }
}
