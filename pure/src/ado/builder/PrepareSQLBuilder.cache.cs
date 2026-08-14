using System.Collections.Generic;
using mooSQL.data.model;

namespace mooSQL.data
{
    /// <summary>
    /// 执行模板缓存：与 StepBuilder 结果缓存共用 <see cref="StepBuilder.cacheHolder"/>
    ///（setCacheHolder → Client.Cache → 默认 HashCache）。
    /// </summary>
    public partial class PrepareSQLBuilder
    {
        /// <summary>
        /// 新 <see cref="SQLBuilder"/> 实例的模板缓存默认开关。
        /// TEMP: 业务大测可保持 true；性能测试（如 dbTest）可在进程启动时设为 false。
        /// </summary>
        public static bool DefaultUseScriptTemplateCache { get; set; } = true;

        // TEMP: 默认跟随 DefaultUseScriptTemplateCache；测完请把静态默认改回 false。
        private bool _scriptTemplateCacheEnabled = DefaultUseScriptTemplateCache;

        private int _scriptTemplateCacheHits;
        private int _scriptTemplateCacheMisses;

        /// <summary>本次门面实例的模板缓存命中次数（单测/诊断）。</summary>
        public override int ScriptTemplateCacheHits => _scriptTemplateCacheHits;

        /// <summary>本次门面实例的模板缓存未命中次数（单测/诊断）。</summary>
        public override int ScriptTemplateCacheMisses => _scriptTemplateCacheMisses;

        /// <summary>
        /// 启用/关闭执行模板缓存。
        /// TEMP: 当前静态默认开启便于业务测试；正式默认应为关闭。
        /// </summary>
        public override SQLBuilder useScriptTemplateCache(bool enabled = true)
        {
            _scriptTemplateCacheEnabled = enabled;
            return this;
        }

        /// <summary>toSelect：可选冷热分流。</summary>
        public override SQLCmd toSelect()
        {
            return ToCached(
                ScriptCacheKey.BuildKindToSelect,
                QueryType.Select,
                () => _inner.toSelect());
        }

        /// <summary>toInsert：可选冷热分流。</summary>
        public override SQLCmd toInsert()
        {
            return ToCached(
                ScriptCacheKey.BuildKindToInsert,
                QueryType.Insert,
                () => _inner.toInsert());
        }

        /// <summary>toUpdate：可选冷热分流。</summary>
        public override SQLCmd toUpdate()
        {
            return ToCached(
                ScriptCacheKey.BuildKindToUpdate,
                QueryType.Update,
                () => _inner.toUpdate());
        }

        /// <summary>toDelete：可选冷热分流。</summary>
        public override SQLCmd toDelete()
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
                _scriptTemplateCacheHits++;
                _dirty = false;
                return hot;
            }

            _scriptTemplateCacheMisses++;
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
                if (string.IsNullOrEmpty(slotStep.StaticSlotName))
                    return false;
                slots.Add(new StaticSlot
                {
                    SlotId = slotStep.StaticSlotId.Value,
                    NameInTemplate = slotStep.StaticSlotName
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
