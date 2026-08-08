using System;
using System.Collections.Generic;
using mooSQL.data.model;

namespace mooSQL.data
{
    /// <summary>
    /// L2：将 <see cref="SQLMold"/> 转译为 <see cref="SQLCmd"/>。
    /// </summary>
    public static class SqlMoldCompiler
    {
        /// <summary>
        /// 使用模版骨架与本次 <paramref name="liveVars"/> 值生成命令（每次新建 Paras/SQLCmd）。
        /// </summary>
        public static SQLCmd Compile(SQLMold mold, IList<ParaMold> liveVars, string paraPrefix)
        {
            if (mold == null) throw new ArgumentNullException(nameof(mold));
            if (string.IsNullOrEmpty(mold.TemplateSql))
                throw new InvalidOperationException("SQLMold.TemplateSql is empty.");

            var sql = mold.TemplateSql;
            var paras = new Paras();
            var prefix = paraPrefix ?? "";

            var vars = liveVars ?? mold.Vars;
            if (vars != null)
            {
                for (var i = 0; i < vars.Count; i++)
                {
                    var para = vars[i];
                    if (para == null) continue;
                    // MaskBits=0：未纳入 SQL，跳过
                    if (para.MaskBits == 0) continue;

                    if (para.Processor != null)
                    {
                        var expanded = para.Processor(para) ?? new MoldExpandResult();
                        if (!string.IsNullOrEmpty(para.Placeholder))
                            sql = ReplaceOnce(sql, para.Placeholder, expanded.SqlFragment ?? "");
                        if (expanded.Parameters != null)
                        {
                            for (var p = 0; p < expanded.Parameters.Count; p++)
                            {
                                var kv = expanded.Parameters[p];
                                paras.AddByPrefix(kv.Key, kv.Value, prefix);
                            }
                        }
                    }
                    else if (para.MaskBits == 1)
                    {
                        var name = para.ParamName ?? ("mold_" + para.VisitId);
                        paras.AddByPrefix(name, para.Value, prefix);
                    }
                }
            }

            return new SQLCmd(sql, paras)
            {
                type = mold.CmdKind,
                TargetTable = mold.TargetTable ?? ""
            };
        }

        /// <summary>按 VisitId 将 live 值绑到骨架副本。</summary>
        public static SQLMold BindValues(SQLMold skeleton, IList<ParaMold> liveVars)
        {
            var bound = skeleton.CloneSkeleton();
            if (liveVars == null) return bound;
            for (var i = 0; i < bound.Vars.Count; i++)
            {
                ParaMold live = null;
                for (var j = 0; j < liveVars.Count; j++)
                {
                    if (liveVars[j] != null && liveVars[j].VisitId == bound.Vars[i].VisitId)
                    {
                        live = liveVars[j];
                        break;
                    }
                }
                if (live == null && i < liveVars.Count)
                    live = liveVars[i];
                if (live != null)
                {
                    bound.Vars[i].Value = live.Value;
                    bound.Vars[i].Arity = live.Arity;
                    bound.Vars[i].MaskBits = live.MaskBits;
                }
            }
            return bound;
        }

        static string ReplaceOnce(string source, string token, string replacement)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(token))
                return source;
            var idx = source.IndexOf(token, StringComparison.Ordinal);
            if (idx < 0) return source;
            return source.Substring(0, idx) + (replacement ?? "") + source.Substring(idx + token.Length);
        }
    }
}
