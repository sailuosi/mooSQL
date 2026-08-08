using System;
using mooSQL.data.model;

namespace mooSQL.data
{
    public partial class SQLBuilder
    {
        /// <summary>
        /// Mold 两级编译会话；与 Builder 同生命周期，clear 时仅 Clear 内容。
        /// 由 <see cref="init"/> / 字段初始化保证非 null；BrotherBuilder 与父级共用同一实例。
        /// </summary>
        internal MoldSession _moldSession = new MoldSession();

        /// <summary>
        /// 走 Mold 形参登记 / L2 填参（union 等由 <see cref="TryToSelectViaMold"/> 直接回退）。
        /// </summary>
        internal bool MoldActive => true;

        /// <summary>
        /// 拼接 SQL 时跳过写入 <see cref="ps"/>（由 L2 填参）。
        /// </summary>
        internal bool MoldSkipParaBind => true;

        /// <summary>
        /// toSelect 出口：PathKey 命中则 L2；miss 则拼装模版并入缓存再 L2。
        /// 返回 null 表示走原有拼装。
        /// </summary>
        internal SQLCmd TryToSelectViaMold()
        {
            var session = _moldSession;

            // union 多分支结构暂不走 Mold L2
            if (unionHolder != null && unionHolder.Count > 0)
                return null;

            var structure = MoldSession.BuildStructureFingerprint(this);
            if (CTECollection != null && !CTECollection.Empty)
                structure += "|cte:1";
            var pathKey = session.BuildPathKey(this, "Select", structure);
            var paraPrefix = expression != null ? expression.paraPrefix : "@";

            if (SqlMoldCache.TryGet(pathKey, out var cached) && cached != null)
            {
                var bound = SqlMoldCompiler.BindValues(cached, session.Vars);
                return SqlMoldCompiler.Compile(bound, bound.Vars, paraPrefix);
            }

            string sql;
            if (unionHolder.Count == 0)
                sql = current.buildSelect();
            else
                sql = unionHolder.build();

            if (CTECollection != null && !CTECollection.Empty)
            {
                var cte = Dialect.expression.buildCET(CTECollection);
                if (!string.IsNullOrWhiteSpace(cte))
                    sql = cte + " " + sql;
            }

            if (ps != null) ps.Clear();

            var mold = session.CaptureMold(sql, pathKey, QueryType.Select,
                current != null ? (current.tableName ?? "") : "");
            SqlMoldCache.Set(pathKey, mold);

            var liveBound = SqlMoldCompiler.BindValues(mold, session.Vars);
            return SqlMoldCompiler.Compile(liveBound, liveBound.Vars, paraPrefix);
        }

        /// <summary>
        /// Format 片段：登记 ParaMold+Func，返回模版占位串（供 select/from/join/where 使用）。
        /// </summary>
        internal string MoldOrFormatSql(string kind, string template, object[] args)
        {
            var prefix = expression != null ? expression.paraPrefix : "@";
            var para = _moldSession.BeginPara(kind, template, args);
            _moldSession.CommitFormat(para, kind, template, args, prefix);
            return para.Placeholder;
        }
    }
}
