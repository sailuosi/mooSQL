using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using mooSQL.data.model;

namespace mooSQL.data
{
    /// <summary>
    /// Builder 实例级 Mold 会话：与 Builder 同生；clear 时 Clear 内容，不销毁。
    /// </summary>
    public sealed class MoldSession
    {
        /// <summary>可选诊断名（不参与 PathKey）。</summary>
        public string Name { get; set; }

        /// <summary>掩码字典。</summary>
        public MoldMaskDictionary Mask { get; } = new MoldMaskDictionary();

        /// <summary>本次形参（含运行时值）。</summary>
        public List<ParaMold> Vars { get; } = new List<ParaMold>();

        int _visit;

        /// <summary>列表/Format 占位标记。</summary>
        public static string SlotPlaceholder(int visitId) => "{{m" + visitId + "}}";

        /// <summary>标量参数名（无方言前缀）。</summary>
        public static string ScalarParamName(int visitId) => "mold_" + visitId;

        /// <summary>清空本轮编制状态（保留会话实例）。</summary>
        public void Clear()
        {
            Mask.Clear();
            Vars.Clear();
            _visit = 0;
            Name = null;
        }

        /// <summary>判定前建档（VisitId++，暂不写入掩码）。</summary>
        public ParaMold BeginPara(string field, string op, object value)
        {
            var id = _visit++;
            var para = new ParaMold
            {
                VisitId = id,
                MaskBits = -1,
                Value = value,
                Field = field,
                Op = op ?? "=",
                Committed = false
            };
            Vars.Add(para);
            return para;
        }

        /// <summary>本接点未纳入 SQL（MaskBits=0）。</summary>
        public void CommitSkip(ParaMold para)
        {
            if (para == null || para.Committed) return;
            para.MaskBits = 0;
            para.Committed = true;
            para.Placeholder = null;
            para.ParamName = null;
            para.Processor = null;
            para.IsList = false;
            para.IsFormat = false;
            Mask.AddSkip(para.Field, para.Op);
        }

        /// <summary>标量纳入 SQL（MaskBits=1）。</summary>
        public void CommitScalar(ParaMold para, string paraPrefix)
        {
            if (para == null || para.Committed) return;
            para.MaskBits = 1;
            para.Committed = true;
            para.IsList = false;
            para.IsFormat = false;
            para.ParamName = ScalarParamName(para.VisitId);
            para.Placeholder = (paraPrefix ?? "") + para.ParamName;
            para.Processor = null;
            Mask.AddScalar(para.Field, para.Op);
        }

        /// <summary>whereIn 列表（MaskBits=2）；null 源请用 CommitSkip。</summary>
        public void CommitInList(ParaMold para, IEnumerable values, string paraPrefix, int? inLimit)
        {
            if (para == null || para.Committed) return;
            var materialized = SqlMoldInExpand.Materialize(values);
            var arity = materialized.Count;
            var chunks = SqlMoldInExpand.GetChunkCount(arity, inLimit);
            para.MaskBits = 2;
            para.Committed = true;
            para.IsList = true;
            para.IsFormat = false;
            para.Value = materialized;
            para.Arity = arity;
            para.ParamName = ScalarParamName(para.VisitId);
            para.Placeholder = SlotPlaceholder(para.VisitId);
            para.Op = string.IsNullOrWhiteSpace(para.Op) ? "in" : para.Op;
            para.Processor = SqlMoldInExpand.Create(paraPrefix, inLimit);
            Mask.AddIn(para.Field, para.Op, arity, chunks);
        }

        /// <summary>Format 槽（MaskBits=3）。</summary>
        public void CommitFormat(ParaMold para, string kind, string template, object[] args, string paraPrefix)
        {
            if (para == null || para.Committed) return;
            var bag = new MoldFormatValue(kind, template, args);
            para.MaskBits = 3;
            para.Committed = true;
            para.IsList = false;
            para.IsFormat = true;
            para.Value = bag;
            para.ParamName = ScalarParamName(para.VisitId);
            para.Placeholder = SlotPlaceholder(para.VisitId);
            para.Field = kind;
            para.Op = template;
            para.Processor = SqlMoldFormatExpand.Create(paraPrefix);
            Mask.AddFormat(kind, SqlMoldFormatExpand.TemplateFingerprint(template),
                SqlMoldFormatExpand.BuildPresentBits(args));
        }

        /// <summary>基于会话与 Builder 结构生成 PathKey。</summary>
        public SqlMoldPathKey BuildPathKey(SQLBuilder kit, string cmdKind, string structureFingerprint = null)
        {
            var dbType = DataBaseType.None;
            var inLimit = -1;
            if (kit != null && kit.DBLive != null)
            {
                if (kit.DBLive.config != null)
                    dbType = kit.DBLive.config.dbType;
                var lim = kit.DBLive.expression != null ? kit.DBLive.expression.getWhereInLimit() : null;
                inLimit = lim ?? -1;
            }
            var structure = structureFingerprint ?? BuildStructureFingerprint(kit);
            return new SqlMoldPathKey(Mask.Fingerprint(), structure, dbType, inLimit, cmdKind ?? "Select");
        }

        /// <summary>捕获 L1 模版（骨架 Value 清空）。</summary>
        public SQLMold CaptureMold(string templateSql, SqlMoldPathKey pathKey, QueryType cmdKind, string targetTable)
        {
            var mold = new SQLMold
            {
                PathKey = pathKey,
                TemplateSql = templateSql ?? "",
                CmdKind = cmdKind,
                TargetTable = targetTable ?? ""
            };
            for (var i = 0; i < Vars.Count; i++)
            {
                var v = Vars[i];
                mold.Vars.Add(new ParaMold
                {
                    VisitId = v.VisitId,
                    MaskBits = v.MaskBits,
                    Placeholder = v.Placeholder,
                    ParamName = v.ParamName,
                    IsList = v.IsList,
                    IsFormat = v.IsFormat,
                    Processor = v.Processor,
                    Field = v.Field,
                    Op = v.Op,
                    Arity = v.Arity,
                    Committed = v.Committed,
                    Value = null
                });
            }
            return mold;
        }

        /// <summary>select/from/order/page 等结构指纹（CTE 由 Builder 侧追加）。</summary>
        public static string BuildStructureFingerprint(SQLBuilder kit)
        {
            if (kit == null || kit.current == null) return "";
            var g = kit.current;
            var sb = new StringBuilder();
            AppendJoined(sb, "sel", g.selectPart);
            AppendJoined(sb, "from", g.fromPart);
            AppendJoined(sb, "gb", g.groupbyPart);
            AppendJoined(sb, "ob", g.orderPart);
            if (!string.IsNullOrEmpty(g.havingPart))
                sb.Append("|hv:").Append(g.havingPart);
            sb.Append("|sk:").Append(g.skipNum).Append("|tk:").Append(g.pageSize);
            if (kit.unionHolder != null && kit.unionHolder.Count > 0)
                sb.Append("|un:").Append(kit.unionHolder.Count);
            return sb.ToString();
        }

        static void AppendJoined(StringBuilder sb, string tag, List<string> parts)
        {
            sb.Append('|').Append(tag).Append(':');
            if (parts == null || parts.Count == 0) return;
            for (var i = 0; i < parts.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(parts[i]);
            }
        }
    }
}
