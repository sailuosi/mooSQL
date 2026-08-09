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
            // C2 试点：仅 LiveCount==0 的全静态槽句
            if (template.LiveCount != 0)
                return false;
            if (template.StaticSlots == null)
                return false;

            var values = HarvestStaticValues(template.StaticSlots);
            if (values == null)
                return false;

            var ps = new Paras();
            var prefix = _inner.expression != null ? _inner.expression.paraPrefix : "@";
            for (int i = 0; i < template.StaticSlots.Length; i++)
            {
                var slot = template.StaticSlots[i];
                ps.AddByPrefix(slot.NameInTemplate, values[i], prefix);
            }

            cmd = new SQLCmd(template.ShellSql, ps);
            cmd.type = QueryType.Select;
            cmd.TargetTable = _inner.current != null ? (_inner.current.tableName ?? "") : "";
            cmd.signal = _inner.Signal;
            return true;
        }

        /// <summary>按模板槽序从 WhereKeyValStep 收值；对不齐则返回 null（回退冷路径）。</summary>
        private object[] HarvestStaticValues(StaticSlot[] slots)
        {
            var byId = new Dictionary<int, object>();
            var steps = _steps;
            for (int i = 0; i < steps.Count; i++)
            {
                var wk = steps[i] as WhereKeyValStep;
                if (wk == null || wk.StaticSlotId == null) continue;
                byId[wk.StaticSlotId.Value] = wk.Value;
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

        private void TryStoreScriptTemplate(string key, ISooCache holder, SQLCmd cmd)
        {
            if (holder == null || cmd == null || string.IsNullOrEmpty(key)) return;
            if (!TryBuildScriptTemplate(cmd, out var template)) return;
            if (holder.ContainsKey(key)) return;
            holder.Add(key, template);
        }

        /// <summary>
        /// 冷路径收录：仅当全部静态参均为 ms_s* 槽且无 Live 时入缓存（未改造 API 不参与）。
        /// </summary>
        private bool TryBuildScriptTemplate(SQLCmd cmd, out ScriptTemplate template)
        {
            template = null;
            if (cmd.para == null) return false;
            if (cmd.para.DelayParas != null && cmd.para.DelayParas.Count > 0)
                return false;

            var slots = new List<StaticSlot>();
            var steps = _steps;
            for (int i = 0; i < steps.Count; i++)
            {
                var wk = steps[i] as WhereKeyValStep;
                if (wk == null || wk.StaticSlotId == null) continue;
                slots.Add(new StaticSlot
                {
                    SlotId = wk.StaticSlotId.Value,
                    NameInTemplate = StaticSlotMarks.FormatName(wk.StaticSlotId.Value)
                });
            }

            if (slots.Count == 0) return false;
            if (cmd.para.Count != slots.Count) return false;

            for (int i = 0; i < slots.Count; i++)
            {
                if (cmd.para.GetParameter(slots[i].NameInTemplate) == null)
                    return false;
            }

            template = new ScriptTemplate
            {
                ShellSql = cmd.sql,
                StaticSlots = slots.ToArray(),
                LiveCount = 0,
                ParaSeed = _inner.paraSeed,
                OrchestrationHash = OrchestrationHash
            };
            return true;
        }
    }
}
