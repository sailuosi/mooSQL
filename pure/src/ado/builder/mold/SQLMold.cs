using System.Collections.Generic;
using mooSQL.data.model;

namespace mooSQL.data
{
    /// <summary>
    /// L1 可缓存模版：模版 SQL + 形参骨架（不含本次运行时值）。
    /// </summary>
    public sealed class SQLMold
    {
        /// <summary>路径键。</summary>
        public SqlMoldPathKey PathKey { get; set; }

        /// <summary>带稳定占位符的模版 SQL。</summary>
        public string TemplateSql { get; set; }

        /// <summary>形参骨架（VisitId 序；缓存中 Value 应视为空）。</summary>
        public List<ParaMold> Vars { get; set; }

        /// <summary>命令类型。</summary>
        public QueryType CmdKind { get; set; }

        /// <summary>目标表。</summary>
        public string TargetTable { get; set; }

        /// <summary>
        /// 创建空模版。
        /// </summary>
        public SQLMold()
        {
            Vars = new List<ParaMold>();
            CmdKind = QueryType.Select;
            TargetTable = "";
        }

        /// <summary>
        /// 复制骨架（Processor/占位符保留，Value 置空，供绑定本次值）。
        /// </summary>
        public SQLMold CloneSkeleton()
        {
            var clone = new SQLMold
            {
                PathKey = PathKey,
                TemplateSql = TemplateSql,
                CmdKind = CmdKind,
                TargetTable = TargetTable ?? ""
            };
            for (var i = 0; i < Vars.Count; i++)
            {
                var v = Vars[i];
                clone.Vars.Add(new ParaMold
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
            return clone;
        }
    }
}
