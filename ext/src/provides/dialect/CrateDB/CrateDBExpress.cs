using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using mooSQL.data.builder;

namespace mooSQL.data
{
    /// <summary>
    /// CrateDB SQL 生成：继承 NpgsqlExpress，覆盖 MERGE→ON CONFLICT、无 SERIAL、无 COMMENT ON 等。
    /// </summary>
    public class CrateDBExpress : NpgsqlExpress
    {
        static readonly Regex ConflictEq = new Regex(
            @"^\s*(?:(?<la>[""\w]+)\.)?(?<lc>[""\w]+)\s*=\s*(?:(?<ra>[""\w]+)\.)?(?<rc>[""\w]+)\s*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public CrateDBExpress(Dialect dia) : base(dia)
        {
            _provideType = "Npgsql.NpgsqlFactory,Npgsql";
        }

        /// <summary>
        /// Crate 无 SERIAL；建表自增请用应用侧 GUID 或外部发号。
        /// </summary>
        public override string getTableAutoIdSQL() => "";

        /// <summary>
        /// Crate 无 ANSI MERGE；转为 INSERT … ON CONFLICT … DO UPDATE / DO NOTHING。
        /// </summary>
        public override string buildMergeInto(FragMergeInto frag)
        {
            if (frag == null)
                throw new ArgumentNullException(nameof(frag));
            if (string.IsNullOrWhiteSpace(frag.intoTable))
                throw new Exception("CrateDB upsert 需要目标表 intoTable");

            var insertWhen = frag.mergeWhens?.FirstOrDefault(w =>
                !w.matched && w.action == MergeAction.insert);
            if (insertWhen == null || string.IsNullOrWhiteSpace(insertWhen.fieldInner))
                throw new Exception("CrateDB upsert 需要 WHEN NOT MATCHED THEN INSERT（含字段列表）");

            var conflictCols = ParseConflictColumns(frag.onPart, frag.intoAlias, frag.usingAlias);
            if (conflictCols.Count == 0)
                throw new Exception("CrateDB upsert 无法从 ON 条件解析冲突列，请使用 t.col=s.col 形式");

            var updateWhen = frag.mergeWhens?.FirstOrDefault(w =>
                w.matched && w.action == MergeAction.update);

            var sb = new StringBuilder();
            sb.Append("INSERT INTO ").Append(frag.intoTable)
                .Append(" (").Append(insertWhen.fieldInner).Append(") ");

            if (!string.IsNullOrWhiteSpace(frag.usingTable))
            {
                var selectList = string.IsNullOrWhiteSpace(insertWhen.valueInner)
                    ? insertWhen.fieldInner
                    : insertWhen.valueInner;
                if (!string.IsNullOrWhiteSpace(frag.usingAlias))
                    sb.Append("SELECT ").Append(selectList)
                        .Append(" FROM (").Append(frag.usingTable).Append(") AS ")
                        .Append(frag.usingAlias).Append(' ');
                else
                    sb.Append("SELECT ").Append(selectList)
                        .Append(" FROM ").Append(frag.usingTable).Append(' ');
            }
            else
            {
                sb.Append("VALUES (").Append(insertWhen.valueInner).Append(") ");
            }

            sb.Append("ON CONFLICT (").Append(string.Join(", ", conflictCols)).Append(") ");

            if (updateWhen?.setInner != null && updateWhen.setInner.Count > 0)
            {
                sb.Append("DO UPDATE SET ");
                sb.Append(BuildOnConflictSet(updateWhen.setInner, frag.usingAlias));
            }
            else
            {
                sb.Append("DO NOTHING");
            }

            sb.Append(';');
            return sb.ToString();
        }

        /// <summary>Crate 通常不支持 COMMENT ON。</summary>
        public override string buildSoloFieldCaption(DDLFragSQL frag, DDLField fie) => "";

        /// <summary>Crate 通常不支持 COMMENT ON。</summary>
        public override string buildSoloTableCaption(DDLFragSQL frag) => "";

        public override string DeleteColumnCaptionBy(string tableName, string columnName) => "";

        public override string DeleteTableCaptionBy(string tableName) => "";

        /// <summary>退化为 CREATE TABLE … AS SELECT * FROM … LIMIT 0。</summary>
        public override string buildCopyTableSchema(DDLFragSQL frag)
        {
            return string.Format("CREATE TABLE {0} AS SELECT * FROM {1} LIMIT 0", frag.Table, frag.SrcTable);
        }

        public override string buildCopyTable(DDLFragSQL frag)
        {
            return string.Format("CREATE TABLE {0} AS SELECT * FROM {1}", frag.Table, frag.SrcTable);
        }

        /// <summary>Crate 无 to_regclass；用 information_schema.statistics / 索引视图不可靠时返回恒假探测。</summary>
        public override string IsAnyIndexBy(string indexName)
        {
            return string.Format(
                "SELECT COUNT(1) FROM information_schema.table_constraints WHERE constraint_name = '{0}'",
                indexName?.Replace("'", "''") ?? "");
        }

        static List<string> ParseConflictColumns(string onPart, string intoAlias, string usingAlias)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(onPart))
                return list;

            var parts = Regex.Split(onPart, @"\s+AND\s+", RegexOptions.IgnoreCase);
            foreach (var raw in parts)
            {
                var m = ConflictEq.Match(raw);
                if (!m.Success)
                    continue;

                var la = m.Groups["la"].Value;
                var lc = StripQuotes(m.Groups["lc"].Value);
                var ra = m.Groups["ra"].Value;
                var rc = StripQuotes(m.Groups["rc"].Value);

                string col = null;
                if (!string.IsNullOrEmpty(intoAlias) &&
                    string.Equals(la, intoAlias, StringComparison.OrdinalIgnoreCase))
                    col = lc;
                else if (!string.IsNullOrEmpty(intoAlias) &&
                         string.Equals(ra, intoAlias, StringComparison.OrdinalIgnoreCase))
                    col = rc;
                else if (string.IsNullOrEmpty(la) && !string.IsNullOrEmpty(ra))
                    col = lc;
                else if (!string.IsNullOrEmpty(la) &&
                         (string.IsNullOrEmpty(usingAlias) ||
                          !string.Equals(la, usingAlias, StringComparison.OrdinalIgnoreCase)))
                    col = lc;
                else
                    col = lc;

                if (!string.IsNullOrEmpty(col) &&
                    !list.Exists(x => string.Equals(x, col, StringComparison.OrdinalIgnoreCase)))
                    list.Add(col);
            }

            return list;
        }

        static string StripQuotes(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            if (name.Length >= 2 && name[0] == '"' && name[name.Length - 1] == '"')
                return name.Substring(1, name.Length - 2);
            return name;
        }

        /// <summary>
        /// DO UPDATE SET：源别名列改为 excluded.col（PG/Crate INSERT…ON CONFLICT 约定）。
        /// </summary>
        string BuildOnConflictSet(List<FragSetPart> setInner, string usingAlias)
        {
            bool first = true;
            var sb = new StringBuilder();
            foreach (var item in setInner)
            {
                if (!first) sb.Append(',');
                else first = false;

                sb.Append(item.field).Append('=');
                var val = item.value ?? "";
                if (!string.IsNullOrEmpty(usingAlias) &&
                    val.StartsWith(usingAlias + ".", StringComparison.OrdinalIgnoreCase))
                {
                    var col = val.Substring(usingAlias.Length + 1);
                    sb.Append("excluded.").Append(col);
                }
                else
                {
                    sb.Append(val);
                }
                sb.Append(' ');
            }
            return sb.ToString();
        }
    }
}
