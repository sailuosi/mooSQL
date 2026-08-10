using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using mooSQL.data;

namespace mooSQL.data.richRepo.tracking
{
    /// <summary>
    /// 实体修改追踪门面（WeakTable 附着，POCO 友好）。
    /// </summary>
    public static class EntityTracking
    {
        sealed class TrackingState
        {
            public EntitySnapshot Snapshot;
            public EntityChangeBag Bag;
            public TrackingOptions Options;
        }

        static readonly ConditionalWeakTable<object, TrackingState> States =
            new ConditionalWeakTable<object, TrackingState>();

        /// <summary>开始追踪（原始值快照）。</summary>
        public static EntitySnapshot Begin<T>(T entity, EntityInfo meta = null, TrackingOptions opt = null)
            where T : class
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            var state = States.GetValue(entity, _ => new TrackingState());
            state.Options = opt ?? state.Options ?? new TrackingOptions();
            if (meta == null)
                throw new ArgumentNullException(nameof(meta), "Begin 需要 EntityInfo（由仓储传入 En）。");
            state.Snapshot = new EntitySnapshot(entity, meta, state.Options);
            return state.Snapshot;
        }

        /// <summary>批量开始追踪。</summary>
        public static void BeginRange<T>(IEnumerable<T> entities, EntityInfo meta, TrackingOptions opt = null)
            where T : class
        {
            if (entities == null) return;
            foreach (var e in entities)
            {
                if (e != null) Begin(e, meta, opt);
            }
        }

        /// <summary>获取或创建手动脏字段袋。</summary>
        public static EntityChangeBag GetOrCreateBag(object entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (entity is ITrackedEntity tracked && tracked.ChangeBag != null)
                return tracked.ChangeBag;
            var state = States.GetValue(entity, _ => new TrackingState());
            if (state.Bag == null) state.Bag = new EntityChangeBag();
            return state.Bag;
        }

        /// <summary>是否已追踪（有快照或脏袋）。</summary>
        public static bool IsTracked(object entity)
        {
            if (entity == null) return false;
            if (entity is ITrackedEntity te && te.ChangeBag != null && te.ChangeBag.IsModified)
                return true;
            return States.TryGetValue(entity, out var state)
                   && (state.Snapshot != null || (state.Bag != null && state.Bag.IsModified));
        }

        /// <summary>是否有快照（用于 Update 默认脏路径判断）。</summary>
        public static bool HasSnapshot(object entity)
        {
            return entity != null && States.TryGetValue(entity, out var state) && state.Snapshot != null;
        }

        /// <summary>
        /// 取脏字段：Bag 非空优先；否则 Snapshot Diff；皆无返回空字典。
        /// </summary>
        public static IReadOnlyDictionary<string, object> GetDirtyMembers(object entity)
        {
            if (entity == null) return Empty;
            if (entity is ITrackedEntity te && te.ChangeBag != null && te.ChangeBag.IsModified)
                return te.ChangeBag.GetChanges();

            if (!States.TryGetValue(entity, out var state))
                return Empty;

            if (state.Bag != null && state.Bag.IsModified)
                return state.Bag.GetChanges();

            if (state.Snapshot != null)
                return state.Snapshot.GetDiff();

            return Empty;
        }

        /// <summary>获取附着的 TrackingOptions（无则 null）。</summary>
        public static TrackingOptions GetOptions(object entity)
        {
            if (entity != null && States.TryGetValue(entity, out var state))
                return state.Options;
            return null;
        }

        /// <summary>更新成功后接受变更。</summary>
        public static void AcceptChanges(object entity)
        {
            if (entity == null) return;
            if (entity is ITrackedEntity te)
                te.ChangeBag?.Clear();
            if (!States.TryGetValue(entity, out var state)) return;
            state.Bag?.Clear();
            state.Snapshot?.AcceptChanges();
        }

        /// <summary>解除追踪。</summary>
        public static void Detach(object entity)
        {
            if (entity == null) return;
            // net451/462 的 ConditionalWeakTable 无 Remove；清空状态即可
            if (States.TryGetValue(entity, out var state))
            {
                state.Snapshot = null;
                state.Bag = null;
                state.Options = null;
            }
        }

        static readonly IReadOnlyDictionary<string, object> Empty =
            new Dictionary<string, object>(StringComparer.Ordinal);
    }
}
