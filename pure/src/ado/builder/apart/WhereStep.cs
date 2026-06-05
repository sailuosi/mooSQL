using System;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// WhereCollection 主干 API 调用类型，用于碎片录制与重放。
    /// </summary>
    public enum WhereStepKind
    {
        Sink,
        SinkNot,
        Rise,
        And,
        Or,
        Not,
        AddFrag
    }

    /// <summary>
    /// 一条 where 构建步骤记录。
    /// </summary>
    public sealed class WhereStep
    {
        public WhereStepKind Kind { get; }
        public string Connector { get; }
        public WhereFrag Frag { get; }

        private WhereStep(WhereStepKind kind, string connector, WhereFrag frag)
        {
            Kind = kind;
            Connector = connector ?? "AND";
            Frag = frag;
        }

        public static WhereStep OfSink(string connector) =>
            new WhereStep(WhereStepKind.Sink, connector, null);

        public static WhereStep OfSinkNot(string connector) =>
            new WhereStep(WhereStepKind.SinkNot, connector, null);

        public static WhereStep OfRise() =>
            new WhereStep(WhereStepKind.Rise, null, null);

        public static WhereStep OfAnd() =>
            new WhereStep(WhereStepKind.And, null, null);

        public static WhereStep OfOr() =>
            new WhereStep(WhereStepKind.Or, null, null);

        public static WhereStep OfNot() =>
            new WhereStep(WhereStepKind.Not, null, null);

        public static WhereStep OfAddFrag(WhereFrag frag) =>
            new WhereStep(WhereStepKind.AddFrag, null, CloneFragForReplay(frag));

        /// <summary>
        /// 深拷贝步骤列表（AddFrag 含 WhereFrag 快照）。
        /// </summary>
        public static List<WhereStep> CloneList(IEnumerable<WhereStep> source)
        {
            var list = new List<WhereStep>();
            if (source == null) return list;
            foreach (var step in source)
            {
                if (step.Kind == WhereStepKind.AddFrag && step.Frag != null)
                    list.Add(OfAddFrag(step.Frag));
                else if (step.Kind == WhereStepKind.Sink)
                    list.Add(OfSink(step.Connector));
                else if (step.Kind == WhereStepKind.SinkNot)
                    list.Add(OfSinkNot(step.Connector));
                else if (step.Kind == WhereStepKind.Rise)
                    list.Add(OfRise());
                else if (step.Kind == WhereStepKind.And)
                    list.Add(OfAnd());
                else if (step.Kind == WhereStepKind.Or)
                    list.Add(OfOr());
                else if (step.Kind == WhereStepKind.Not)
                    list.Add(OfNot());
            }
            return list;
        }

        /// <summary>
        /// 复制条件内容，不含 paramKey（重放时由 addFrag 重新分配）。
        /// </summary>
        public static WhereFrag CloneFragForReplay(WhereFrag src)
        {
            if (src == null) return null;
            var f = new WhereFrag
            {
                key = src.key,
                value = src.value,
                op = src.op,
                paramed = src.paramed,
                leftParamed = src.leftParamed,
                leftValue = src.leftValue,
                pined = src.pined,
                nextPined = src.nextPined,
                connector = src.connector,
                updatable = src.updatable,
                insetable = src.insetable
            };
            return f;
        }

        /// <summary>
        /// 按录制顺序在目标 SQLBuilder 上重放公开 API。
        /// </summary>
        public static void Replay(List<WhereStep> steps, SQLBuilder kit)
        {
            if (steps == null || steps.Count == 0 || kit == null) return;
            foreach (var step in steps)
            {
                switch (step.Kind)
                {
                    case WhereStepKind.Sink:
                        if (string.Equals(step.Connector, "OR", StringComparison.OrdinalIgnoreCase))
                            kit.sinkOR();
                        else
                            kit.sink(step.Connector);
                        break;
                    case WhereStepKind.SinkNot:
                        if (string.Equals(step.Connector, "OR", StringComparison.OrdinalIgnoreCase))
                            kit.sinkNotOR();
                        else
                            kit.sinkNot(step.Connector);
                        break;
                    case WhereStepKind.Rise:
                        kit.rise();
                        break;
                    case WhereStepKind.And:
                        kit.and();
                        break;
                    case WhereStepKind.Or:
                        kit.or();
                        break;
                    case WhereStepKind.Not:
                        kit.not();
                        break;
                    case WhereStepKind.AddFrag:
                        ReplayFrag(kit, step.Frag);
                        break;
                }
            }
        }

        private static void ReplayFrag(SQLBuilder kit, WhereFrag frag)
        {
            if (frag == null) return;
            if (frag.pined)
            {
                kit.pin(frag.key);
                return;
            }
            if (frag.value == null && string.IsNullOrEmpty(frag.op))
            {
                kit.where(frag.key);
                return;
            }
            if (!frag.paramed && !frag.leftParamed)
            {
                if (string.IsNullOrEmpty(frag.op))
                    kit.where(frag.key);
                else
                    kit.where(frag.key, frag.value, frag.op, false);
                return;
            }
            kit.where(CloneFragForReplay(frag));
        }
    }
}
