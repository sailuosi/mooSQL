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
        private bool _scriptTemplateCacheEnabled;

        /// <summary>本次门面实例的模板缓存命中次数（单测/诊断）。</summary>
        public int ScriptTemplateCacheHits { get; private set; }

        /// <summary>本次门面实例的模板缓存未命中次数（单测/诊断）。</summary>
        public int ScriptTemplateCacheMisses { get; private set; }

        /// <summary>
        /// 启用/关闭 toSelect 执行模板缓存（默认关闭，不影响既有行为）。
        /// 存储走与 setCache / query 相同的 cacheHolder。
        /// </summary>
        public SQLBuilder useScriptTemplateCache(bool enabled = true)
        {
            _scriptTemplateCacheEnabled = enabled;
            return this;
        }

        /// <summary>toSelect：可选冷热分流；未启用时等价于 runBuild + Inner.toSelect。</summary>
        public SQLCmd toSelect()
        {
            if (!_scriptTemplateCacheEnabled)
            {
                runBuild();
                return _inner.toSelect();
            }

            var key = BuildScriptCacheKey(ScriptCacheKey.BuildKindToSelect);
            var holder = _inner.cacheHolder;
            var cached = holder.Get<ScriptTemplate>(key);
            if (cached != null && TryBindHotSelect(cached, out var hot))
            {
                ScriptTemplateCacheHits++;
                _dirty = false;
                return hot;
            }

            ScriptTemplateCacheMisses++;
            runBuild();
            var cmd = _inner.toSelect();
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

        private bool TryBindHotSelect(ScriptTemplate template, out SQLCmd cmd)
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
            cmd.type = QueryType.Select;
            cmd.TargetTable = _inner.current != null ? (_inner.current.tableName ?? "") : "";
            cmd.signal = _inner.Signal;
            return true;
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
        /// 按磁带序 CollectBind Live；复现 ifs 门控（Where 类步消费 opened）。
        /// 未实现 <see cref="ILiveBindStep"/> 的 Live 源会导致个数对不齐并回退冷路径。
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
                    if (step.Kind == StepKind.Where)
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
        /// 未改造静态 API（旧 wp 名）或无法 Collect 的 Live 源不入缓存。
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

            // 热路径须能 Collect 出相同个数，否则不收录（避免必 miss）
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
    }
}
