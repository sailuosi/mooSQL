using System;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// Apart：在 SQLBuilder 编排磁带上录制 / 快照 / 重放 <see cref="IStep"/>。
    /// </summary>
    public partial class PrepareSQLBuilder
    {
        /// <summary>record() 时的磁带起点下标；null 表示未在录制。</summary>
        private int? _recordStart;

        /// <summary>
        /// 开启编排录制：其后入队的步骤可被 <see cref="stop"/> 截为碎片，并从当前磁带移除（不污染父链）。
        /// </summary>
        /// <example>
        /// var seg = kit.record().where("status", 1).stop();
        /// kit.select("*").from("users").useApart(seg).toSelect();
        /// </example>
        public override SQLBuilder record()
        {
            if (_recordStart != null)
                throw new InvalidOperationException("Already in record(); call stop() first.");
            _recordStart = _steps.Count;
            return this;
        }

        /// <summary>
        /// 结束 <see cref="record"/>：截取录制区间步骤为 <see cref="SQLApart"/>，并从当前磁带移除。
        /// </summary>
        public override SQLApart stop()
        {
            if (_recordStart == null)
                throw new InvalidOperationException("Call record() before stop().");

            int start = _recordStart.Value;
            _recordStart = null;
            if (start < 0)
                start = 0;
            if (start > _steps.Count)
                start = _steps.Count;

            int count = _steps.Count - start;
            var captured = new List<IStep>(count);
            for (int i = start; i < _steps.Count; i++)
                captured.Add(_steps[i]);

            if (count > 0)
                _steps.RemoveRange(start, count);

            SyncNextStaticSlotFromTape();
            _dirty = _steps.Count > 0;
            return new SQLApart(captured, ResolveApartDbType());
        }

        /// <summary>
        /// 将当前编排磁带快照为可复用碎片（浅拷贝步骤列表；步骤实例与磁带共享直至 clear）。
        /// </summary>
        public override SQLApart toApart()
        {
            return new SQLApart(CopySteps(), ResolveApartDbType());
        }

        /// <summary>
        /// 将碎片步骤按序重绑静态槽后入队到当前编排（合并追加）。
        /// </summary>
        public override SQLBuilder useApart(SQLApart apart)
        {
            if (apart == null)
                throw new ArgumentNullException(nameof(apart));
            EnsureApartDbCompatible(apart);

            var steps = apart.Steps;
            for (int i = 0; i < steps.Count; i++)
                EnqueueApartStep(steps[i]);
            return this;
        }

        /// <summary>clear/reset 时结束未完成的 record。</summary>
        private void ClearApartRecording()
        {
            _recordStart = null;
        }

        private DataBaseType ResolveApartDbType()
        {
            if (_inner?.DBLive?.config != null)
                return _inner.DBLive.config.dbType;
            return DataBaseType.MSSQL;
        }

        private void EnsureApartDbCompatible(SQLApart apart)
        {
            var target = ResolveApartDbType();
            if (apart.SourceDbType != target)
                throw new ApartIncompatibleException(apart.SourceDbType, target);
        }

        /// <summary>Apart 重放：按目标门面重新分配 StaticSlot，再入队或物化 Apply。</summary>
        private void EnqueueApartStep(IStep step)
        {
            if (step == null)
                return;

            RebindApartStaticSlot(step);

            if (_materializing)
            {
                step.Apply(this);
                return;
            }

            _steps.Add(step);
            if (_deferredEnabled)
                _dirty = true;
            else
            {
                step.Apply(this);
                _dirty = false;
            }
        }

        private void SyncNextStaticSlotFromTape()
        {
            int max = -1;
            for (int i = 0; i < _steps.Count; i++)
            {
                var ss = _steps[i] as IStaticSlotStep;
                if (ss?.StaticSlotId != null && ss.StaticSlotId.Value > max)
                    max = ss.StaticSlotId.Value;
            }
            _nextStaticSlot = max + 1;
        }

        private void RebindApartStaticSlot(IStep step)
        {
            var whereSeed = CurrentWhereGroupSeed;
            var setKey = CurrentSetGroupKey;
            var paraSeed = CurrentParaSeed;

            if (step is WhereKeyValStep wk)
            {
                wk.TryAssignStaticSlot(_paraRule, ref _opened, ref _nextStaticSlot, paraSeed, whereSeed);
                return;
            }
            if (step is WhereKeyValOpParamedStep wo)
            {
                wo.TryAssignStaticSlot(_paraRule, ref _opened, ref _nextStaticSlot, paraSeed, whereSeed);
                return;
            }
            if (step is WhereKeyCompareStep wc)
            {
                wc.TryAssignStaticSlot(_paraRule, ref _opened, ref _nextStaticSlot, paraSeed, whereSeed);
                return;
            }
            if (step is SetUstringobjectStep su)
            {
                su.TryAssignStaticSlot(_paraRule, ref _opened, ref _nextStaticSlot, paraSeed, setKey);
                return;
            }
            if (step is SetUstringobjectboolStep sub)
            {
                sub.TryAssignStaticSlot(_paraRule, ref _opened, ref _nextStaticSlot, paraSeed, setKey);
                return;
            }
            if (step is SetIstringobjectStep si)
            {
                si.TryAssignStaticSlot(_paraRule, ref _opened, ref _nextStaticSlot, paraSeed, setKey);
                return;
            }
            if (step is SetIstringobjectboolStep sib)
            {
                sib.TryAssignStaticSlot(_paraRule, ref _opened, ref _nextStaticSlot, paraSeed, setKey);
                return;
            }
            if (step is SetstringobjectboolTypeboolboolStep st)
            {
                st.TryAssignStaticSlot(_paraRule, ref _opened, ref _nextStaticSlot, paraSeed, setKey);
                return;
            }
            if (step is SetstringstringintStep ss)
            {
                ss.TryAssignStaticSlot(_paraRule, ref _opened, ref _nextStaticSlot, paraSeed, setKey);
            }
        }
    }
}
